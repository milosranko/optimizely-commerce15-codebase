# Hangfire implementation reference

Companion to `SKILL.md`. Namespaces use the Wisbar layout; the repo is the authority.

## A. Integration from scratch

### Packages

`Hangfire.AspNetCore` and `Hangfire.SqlServer` (the project already references both, plus `Hangfire.Core` transitively).

### Options

```csharp
namespace Wisbar.Business.Features.BackgroundJobs.Options;

public class HangfireOptions
{
    public const string SectionName = "Hangfire";

    public int WorkerCount { get; set; } = 2;
    public string[] Queues { get; set; } = ["default", "orders"];
    public TimeSpan ServerTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public int RetryAttempts { get; set; } = 5;
    public string DashboardPath { get; set; } = "/ui/hangfire";
    public FailureNotificationOptions FailureNotification { get; set; } = new();
}

public class FailureNotificationOptions
{
    public bool Enabled { get; set; } = true;
    public string[] Recipients { get; set; } = [];
    public string FromAddress { get; set; } = "noreply@domain.com";
    public string FromDisplayName { get; set; } = "Background Jobs";
}
```

### Storage and server

```csharp
var hangfireOptions = _configuration
    .GetSection(HangfireOptions.SectionName)
    .Get<HangfireOptions>() ?? new HangfireOptions();

services.Configure<HangfireOptions>(_configuration.GetSection(HangfireOptions.SectionName));

services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(
        _configuration.GetConnectionString("EcfSqlConnection"),
        new SqlServerStorageOptions
        {
            SchemaName = "HangFire",
            QueuePollInterval = TimeSpan.FromSeconds(15),
            DisableGlobalLocks = true,
            PrepareSchemaIfNecessary = true
        }));

services.AddHangfireServer(options =>
{
    options.WorkerCount = hangfireOptions.WorkerCount;
    options.Queues = hangfireOptions.Queues;
    options.ServerTimeout = hangfireOptions.ServerTimeout;
});
```

Notes:

- Shares `EcfSqlConnection` under its own `HangFire` schema - no separate database.
- `PrepareSchemaIfNecessary` creates the tables; no migration step for Hangfire itself.
- `DisableGlobalLocks = true` is the recommended SQL Server setting for throughput.
- `WorkerCount` is deliberately small (2). Raising it raises concurrent load on Commerce and the database.

### Dashboard

```csharp
// Startup.Configure
var hangfireOpts = app.ApplicationServices.GetRequiredService<IOptions<HangfireOptions>>().Value;

GlobalJobFilters.Filters.Add(new JobFailureNotificationFilter(
    app.ApplicationServices,
    app.ApplicationServices.GetRequiredService<ILogger<JobFailureNotificationFilter>>()));

app.UseHangfireDashboard(hangfireOpts.DashboardPath, new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorizationFilter(
        app.ApplicationServices.GetRequiredService<IWebHostEnvironment>())]
});

// In UseEndpoints
endpoints.MapHangfireDashboard(new DashboardOptions { PrefixPath = "/ui" });
```

### Dashboard authorization

```csharp
public bool Authorize(DashboardContext context)
{
    if (_environment.IsDevelopment())
        return true;

    var httpContext = context.GetHttpContext();
    var user = httpContext.User;

    // CMS identity specifically - a signed-in storefront member is authenticated but holds no
    // CMS roles, and the dashboard is an admin surface. IsInRole spans every identity on the
    // principal, so this still resolves for an editor who is also a member.
    var identity = httpContext.RequestServices.GetRequiredService<ICurrentIdentity>();

    return identity.IsCmsUser
        && (user.IsInRole(Roles.WebAdmins) || user.IsInRole(Roles.CmsAdmins) || user.IsInRole(Roles.Administrators));
}
```

Never weaken this to "is authenticated" - that would expose the dashboard to storefront members.

### CMS menu entry

```csharp
[MenuProvider]
public class HangfireMenuProvider : IMenuProvider
{
    public IEnumerable<MenuItem> GetMenuItems()
    {
        var menuItem = new UrlMenuItem("Hangfire", MenuPaths.Global + "/cms" + "/hangfire", "/ui/hangfirecms/index")
        {
            IsAvailable = request => PrincipalInfo.CurrentPrincipal.IsInRole(Roles.CmsAdmins),
            AuthorizationPolicy = CmsPolicyNames.CmsAdmin,
            SortIndex = SortIndex.Normal
        };

        return [menuItem];
    }
}
```

## B. Marker interfaces

```csharp
namespace Wisbar.Business.Features.BackgroundJobs.Interfaces;

/// <summary>Marker interface for all background jobs.</summary>
public interface IBackgroundJob;

/// <summary>Background job that processes a typed payload.</summary>
public interface IBackgroundJob<in TPayload> : IBackgroundJob
{
    Task ExecuteAsync(TPayload payload, CancellationToken ct);
}
```

Recurring jobs with no payload implement neither - they are plain classes with an `ExecuteAsync(CancellationToken)`.

## C. Job with a payload

### Payload

```csharp
namespace Wisbar.Business.Features.BackgroundJobs.Models;

public record SendWidgetPayload(string WidgetCode, int OrderGroupId);
```

Ids only. Serialized into Hangfire's tables and deserialized later, possibly after a deploy that changed your domain types.

### Job

```csharp
using Hangfire;
using Microsoft.Extensions.Logging;
using Wisbar.Business.Features.BackgroundJobs.Interfaces;
using Wisbar.Business.Features.BackgroundJobs.Models;

namespace Wisbar.Business.Features.BackgroundJobs.Jobs;

// What this does, and WHY it is safe to retry: writing one metafield is idempotent, so a Hangfire
// retry simply overwrites the same value. The confirmation email is best-effort (swallows its own
// failures) and guarded by a persisted flag, so a retry neither fails the job nor re-sends.
[Queue("orders")]
[AutomaticRetry(Attempts = 5)]
public sealed class SendWidgetJob : IBackgroundJob<SendWidgetPayload>
{
    private readonly IWidgetStore _store;
    private readonly ILogger<SendWidgetJob> _logger;

    public SendWidgetJob(IWidgetStore store, ILogger<SendWidgetJob> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task ExecuteAsync(SendWidgetPayload payload, CancellationToken ct)
    {
        var widget = _store.Get(payload.WidgetCode);

        if (widget is null)
        {
            _logger.LogWarning("Widget {Code} not found; nothing to send.", payload.WidgetCode);
            return;
        }

        // Hangfire requeues jobs orphaned by a dead worker, so finished work must not be redone.
        if (widget.IsSent)
        {
            _logger.LogInformation("Widget {Code} already sent; skipping.", payload.WidgetCode);
            return;
        }

        try
        {
            await _store.SendAsync(widget, ct);
            _logger.LogInformation("Widget {Code} sent.", payload.WidgetCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sending widget {Code} failed.", payload.WidgetCode);
            throw;   // let Hangfire retry; the permanent-failure email comes from the global filter
        }
    }
}
```

### Registration

```csharp
// Startup.ConfigureServices - Hangfire jobs are NOT auto-discovered.
services.AddScoped<SendOrderJob>();
services.AddScoped<MarkOrdersExportedJob>();
services.AddScoped<ExceptionAlertJob>();
services.AddScoped<RunImportJob>();
services.AddScoped<ImportRetentionJob>();
services.AddScoped<SendClassifiedAdJob>();
// ... and any new job
```

## D. Enqueueing

```csharp
// Inject IBackgroundJobClient
_jobClient.Enqueue<SendWidgetJob>(job => job.ExecuteAsync(payload, CancellationToken.None));
```

### Never let an enqueue failure roll back the business operation

```csharp
_orderRepository.Delete(cart.OrderLink); // the order is placed at this point

string? sendOrderJobId = null;
try
{
    sendOrderJobId = _jobClient.Enqueue<SendOrderJob>(j =>
        j.ExecuteAsync(new SendOrderPayload(orderNumber, orderReference.OrderGroupId), CancellationToken.None));
}
catch (Exception ex)
{
    _logger.LogError(ex, "SendOrderJob enqueue failed for order {OrderNumber}", orderNumber);
}
```

### Chaining

```csharp
// Chained off SendOrderJob so the two never write the same order concurrently.
_jobClient.ContinueJobWith<SendClassifiedAdJob>(
    parentJobId,
    job => job.ExecuteAsync(payload, CancellationToken.None),
    options: JobContinuationOptions.OnAnyFinishedState);
```

`OnAnyFinishedState` runs the continuation even when the parent failed. The default runs it only on success - pick deliberately.

Enqueue sites in this codebase, for reference: `OrderPlacementService`, `ClassifiedAdService`, `PayflowCallbackService`, `ImportQueue`, `ProductsImportService`, `UCommerceDataImportService`, `ClassifiedAdExportEndpoints`, `OrderExportEndpoints`, `GlobalExceptionHandler`.

## E. Recurring jobs

```csharp
public sealed class WidgetRetentionJob
{
    private const int RetentionDays = 30;
    private const int BatchSize = 200;

    public Task ExecuteAsync(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);
        var purged = _store.Purge(cutoff, BatchSize);

        if (purged > 0)
            _logger.LogInformation("Purged {Count} widgets older than {Cutoff:u}.", purged, cutoff);

        return Task.CompletedTask;
    }
}
```

```csharp
services.AddScoped<WidgetRetentionJob>();

// Startup.Configure - the string id is permanent; changing it orphans the old entry.
app.ApplicationServices.GetRequiredService<IRecurringJobManager>().AddOrUpdate<WidgetRetentionJob>(
    "widget-retention",
    job => job.ExecuteAsync(CancellationToken.None),
    "15 3 * * *");
```

| Existing recurring job | Cron | Meaning |
|---|---|---|
| `payflow-reconciliation` | `*/15 * * * *` | Every 15 minutes |
| `payflow-retention` | `30 3 * * *` | 03:30 daily |
| `import-retention` | `45 3 * * *` | 03:45 daily |
| `payment-page-integrity` | `0 4 */3 * *` | 04:00 every third day |

Environment-guard a job that cannot work locally:

```csharp
if (!env.IsDevelopment())
{
    app.ApplicationServices.GetRequiredService<IRecurringJobManager>().AddOrUpdate<PaymentPageIntegrityMonitorJob>(
        "payment-page-integrity", job => job.ExecuteAsync(CancellationToken.None), "0 4 */3 * *");
}
```

## F. Distributed lock

```csharp
/// <summary>
/// IDistributedLock backed by Hangfire's SQL Server storage (sp_getapplock). The lock is held by a
/// dedicated storage connection and released when that connection is disposed, so a crashed holder
/// frees the lock automatically once its connection drops.
/// </summary>
public sealed class HangfireDistributedLock : IDistributedLock
{
    public IDisposable? TryAcquire(string resource, TimeSpan timeout, CancellationToken ct = default)
    {
        var connection = _storage.GetConnection();
        try
        {
            var handle = connection.AcquireDistributedLock(resource, timeout);
            return new Handle(connection, handle);
        }
        catch (DistributedLockTimeoutException)
        {
            connection.Dispose();
            return null;      // timeout is a normal outcome, not an exception to propagate
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }
}
```

Registered `services.AddSingleton<IDistributedLock, HangfireDistributedLock>();`.

Usage:

```csharp
// One lock for every import, not one per kind: several of these write the same catalog entries or
// contacts, and running them one at a time is no hardship for an occasional admin operation.
private const string LockResource = "wisbar:import";
private static readonly TimeSpan LockWait = TimeSpan.FromSeconds(10);

using var lease = _distributedLock.TryAcquire(LockResource, LockWait, ct);
if (lease is null)
{
    _runStore.MarkFailed(run.RunId, "Another import is already running. Please try again once it finishes.");
    return;
}
```

Also used by `PaymentStartService` and `PayflowFinalizationService` for at-most-once order placement.

## G. Failure notification filter

```csharp
public class JobFailureNotificationFilter : IElectStateFilter
{
    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is not FailedState failedState)
            return;

        // Only notify on permanent failure (all retries exhausted)
        var retryCount = context.GetJobParameter<int>("RetryCount");
        if (retryCount < GetMaxRetryAttempts(context))
            return;

        _logger.LogError(failedState.Exception,
            "Background job {JobId} ({JobType}) has permanently failed after all retries",
            context.BackgroundJob.Id, context.BackgroundJob.Job?.Type.Name);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<HangfireOptions>>().Value;

            if (!options.FailureNotification.Enabled || options.FailureNotification.Recipients.Length == 0)
                return;

            // ... build and send the email
        }
        catch (Exception ex)
        {
            // A notification failure must not escalate.
            _logger.LogError(ex, "Failed to send failure notification for job {JobId}", context.BackgroundJob.Id);
        }
    }

    private static int GetMaxRetryAttempts(ElectStateContext context)
    {
        var autoRetryAttr = context.BackgroundJob.Job?.Type
            .GetMethod(context.BackgroundJob.Job.Method.Name)?
            .GetCustomAttributes(typeof(AutomaticRetryAttribute), true)
            .OfType<AutomaticRetryAttribute>()
            .FirstOrDefault();

        return autoRetryAttr?.Attempts ?? 10; // Hangfire default is 10
    }
}
```

Registered once, globally, in `Startup.Configure`. It reads each job's own `[AutomaticRetry]` so a job with `Attempts = 0` alerts on its first failure. **Do not add per-job failure email.**

Note it resolves scoped services through an explicit `CreateScope()` - a global filter has no ambient request scope.

## H. Tests

`Wisbar.Tests/Features/BackgroundJobs/`: `SendOrderJobTests`, `MarkOrdersExportedJobTests`, `SendClassifiedAdJobTests`, `MarkClassifiedAdsExportedJobTests`, `PayflowVerifyJobTests`.

```csharp
[TestClass]
public class SendWidgetJobTests
{
    [TestMethod]
    public async Task ExecuteAsync_WidgetAlreadySent_DoesNotSendAgain()
    {
        // The requeue-after-worker-death path: the single most important test for a Hangfire job.
    }

    [TestMethod]
    public async Task ExecuteAsync_MissingWidget_ReturnsWithoutThrowing() { }

    [TestMethod]
    public async Task ExecuteAsync_SendFails_RethrowsSoHangfireRetries() { }
}
```

## Quick reference

| Concern | Rule |
|---|---|
| Discovery | None - `AddScoped<TJob>()` required |
| Payload | `record` of ids in `Models/` |
| Queue | `[Queue(...)]`, name must be in `HangfireOptions.Queues` |
| Retries | `[AutomaticRetry(Attempts = n)]`, `0` when not idempotent |
| Idempotency | Mandatory - terminal flag, natural idempotency, or lock |
| Enqueue token | `CancellationToken.None` |
| Enqueue failure | Caught and logged, never rolls back the business operation |
| Chaining | `ContinueJobWith`, choose the continuation option deliberately |
| Recurring id | Permanent; changing it orphans the entry |
| Failure email | Global filter only |
| Dashboard | Admin-gated via `ICurrentIdentity.IsCmsUser` + role |
