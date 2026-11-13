---
name: hangfire-background-jobs
description: Integrate Hangfire into an ASP.NET Core / Optimizely CMS 12 site and add background jobs - SQL Server storage and server setup, the dashboard and its authorization filter, typed options, job classes with payload records, DI registration, enqueueing from request code, recurring cron jobs, job chaining with continuations, the permanent-failure notification filter, cluster-wide distributed locks, idempotency, and retention. Use whenever asked to set up Hangfire, add a queued or recurring background job, move work off the request thread, chain jobs, debug a job that ran twice or never ran, or secure the Hangfire dashboard.
---

# Hangfire background jobs

Hangfire runs work off the request thread with durable, at-least-once delivery backed by SQL Server. Two consequences drive everything below:

> **At-least-once means every job must be idempotent.** Hangfire requeues jobs orphaned by a dead worker, so any job can run twice.

> **Payloads are serialized to SQL and deserialized later**, possibly after a deploy. They carry ids, never objects.

**Scope check.** This site also has Optimizely scheduled jobs (`Wisbar.Business/ScheduledJobs/`) for work an administrator drives from the CMS admin screen. If an operator needs to see it, run it on demand, stop it, and read its history, that is an Optimizely scheduled job - see the `optimizely-scheduled-job` skill. Hangfire is for request-triggered fire-and-forget work and unattended cron maintenance.

## Step 1: Orient

```bash
ls src/Wisbar.Business/Features/BackgroundJobs/
grep -n "Hangfire\|RecurringJobManager\|AddScoped<.*Job>" src/Wisbar.Web/Startup.cs
```

| Part | Location |
|---|---|
| Jobs | `Business/Features/BackgroundJobs/Jobs/` |
| Payloads | `Business/Features/BackgroundJobs/Models/` |
| Marker interfaces | `Business/Features/BackgroundJobs/Interfaces/IBackgroundJob.cs` |
| Options | `Business/Features/BackgroundJobs/Options/HangfireOptions.cs` |
| Failure filter | `Business/Features/BackgroundJobs/Filters/JobFailureNotificationFilter.cs` |
| CMS menu entry | `Business/Features/BackgroundJobs/Optimizely/HangfireMenuProvider.cs` |
| Distributed lock | `Business/Infrastructure/Concurrency/HangfireDistributedLock.cs` |
| Dashboard auth | `Web/Filters/HangfireDashboardAuthorizationFilter.cs` |
| Tests | `Wisbar.Tests/Features/BackgroundJobs/` |

## Step 2: Integration (already done here - the shape to copy)

Three registrations plus a dashboard. Storage shares the Commerce connection string under its own schema:

```csharp
services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(_configuration.GetConnectionString("EcfSqlConnection"),
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

`PrepareSchemaIfNecessary` means Hangfire creates its own tables - no migration step needed for Hangfire itself.

Everything tunable is bound from the `Hangfire` configuration section into `HangfireOptions`: `WorkerCount`, `Queues`, `ServerTimeout`, `RetryAttempts`, `DashboardPath`, and failure-notification recipients. Add new knobs there, never as constants in a job.

**The dashboard is an admin surface and must stay gated.** `HangfireDashboardAuthorizationFilter` allows everything in Development, and elsewhere requires a CMS identity in an admin role - deliberately checking `ICurrentIdentity.IsCmsUser` rather than plain authentication, because a signed-in storefront member is authenticated but holds no CMS roles. It is also surfaced in the CMS menu for `CmsAdmins`.

## Step 3: Write the job

```csharp
[Queue("orders")]
[AutomaticRetry(Attempts = 5)]
public sealed class SendWidgetJob : IBackgroundJob<SendWidgetPayload>
{
    public async Task ExecuteAsync(SendWidgetPayload payload, CancellationToken ct) { ... }
}
```

- Implement `IBackgroundJob<TPayload>`. A recurring job with no payload is a plain class with `ExecuteAsync(CancellationToken ct = default)`.
- **Payload is a `record` holding ids**, in `Models/`. It round-trips through SQL and must still make sense later.
- `[Queue(...)]` - queue names come from `HangfireOptions.Queues` (`default`, `orders` here). A queue not in that list is never processed.
- `[AutomaticRetry(Attempts = n)]`. Use `Attempts = 0` where a retry would double-apply work that is not idempotent.
- Lead with a comment saying what the job does **and why it is safe to retry**. Every job here does.

**Register it**: `services.AddScoped<SendWidgetJob>()`. Hangfire jobs are not auto-discovered - a missing registration fails at execution time, not startup.

## Step 4: Idempotency is the design, not a nicety

Check a persisted terminal state first and return:

```csharp
// Hangfire requeues jobs orphaned by a dead worker, so a finished run must not be redone.
if (run.IsFinished)
{
    _logger.LogInformation("Run {RunId} already finished as {State}; skipping.", run.RunId, run.State);
    return;
}
```

Options, roughly in order of preference:

1. **Naturally idempotent work** - writing one metafield, setting a flag. `SendOrderJob` builds order XML into a metafield precisely because overwriting the same value is harmless.
2. **A persisted terminal flag** checked on entry, as above.
3. **A distributed lock** when concurrent runs would collide (Step 6).

Best-effort side effects (a confirmation email) should swallow their own failures and be guarded by their own persisted flag, so a retry neither fails the job nor sends a duplicate.

## Step 5: Enqueue

```csharp
_jobClient.Enqueue<SendWidgetJob>(job => job.ExecuteAsync(payload, CancellationToken.None));
```

`CancellationToken.None` is correct - the token is supplied by the worker at execution time, not captured from the enqueuing request.

**Enqueue after the critical work, and never let a queue failure roll it back:**

```csharp
string? sendOrderJobId = null;
try
{
    sendOrderJobId = _jobClient.Enqueue<SendOrderJob>(j => j.ExecuteAsync(payload, CancellationToken.None));
}
catch (Exception ex)
{
    _logger.LogError(ex, "SendOrderJob enqueue failed for order {OrderNumber}", orderNumber);
}
```

The order is placed; a failed enqueue is logged, not thrown. Capture the returned job id when something needs to chain off it.

### Chaining

```csharp
_jobClient.ContinueJobWith<SendClassifiedAdJob>(
    parentJobId,
    job => job.ExecuteAsync(payload, CancellationToken.None),
    options: JobContinuationOptions.OnAnyFinishedState);
```

Used to stop two jobs writing the same order concurrently. `OnAnyFinishedState` runs the continuation even if the parent failed - choose deliberately between that and the default (success only).

## Step 6: Distributed locks

For at-most-once side effects across workers and servers:

```csharp
using var lease = _distributedLock.TryAcquire("wisbar:import", TimeSpan.FromSeconds(10), ct);
if (lease is null)
{
    _runStore.MarkFailed(run.RunId, "Another import is already running. Please try again once it finishes.");
    return;
}
```

`HangfireDistributedLock` is backed by `sp_getapplock` on a dedicated storage connection, so a crashed holder releases automatically when its connection drops. `TryAcquire` returns **null** on timeout - handle that as a real outcome with a user-facing message, not an exception.

Prefer one coarse resource name over many fine ones for admin-triggered work: `RunImportJob` deliberately uses a single `wisbar:import` lock for every import kind.

## Step 7: Recurring jobs

```csharp
// Startup.Configure - the string id is permanent; changing it orphans the old entry.
app.ApplicationServices.GetRequiredService<IRecurringJobManager>().AddOrUpdate<WidgetRetentionJob>(
    "widget-retention",
    job => job.ExecuteAsync(CancellationToken.None),
    "15 3 * * *");
```

Existing slots, so a new job does not pile onto a busy minute: `payflow-reconciliation` `*/15 * * * *`, `payflow-retention` `30 3 * * *`, `import-retention` `45 3 * * *`, `payment-page-integrity` `0 4 */3 * *`.

Wrap in an environment check when the job cannot work locally (`if (!env.IsDevelopment())`).

## Step 8: Failures

`JobFailureNotificationFilter` is registered globally and emails **only on permanent failure** - it compares `RetryCount` against the job's own `[AutomaticRetry]` attempts (defaulting to Hangfire's 10). **Do not add per-job failure email**; it is already handled, and its own send failure is caught and logged rather than escalating.

Recipients and the on/off switch live in `HangfireOptions.FailureNotification`.

## Step 9: Test

Jobs are plain classes with injected dependencies, so they test directly - see `Wisbar.Tests/Features/BackgroundJobs/`.

```bash
dotnet test src/Wisbar.Tests/Wisbar.Tests.csproj
```

Cover at minimum: the happy path, **the already-finished short circuit** (the requeue-after-worker-death path - the single most important test for any Hangfire job), and a failure that must not lose data.

## Step 10: Verify

```bash
dotnet build src/Wisbar.sln
```

Report which of these you actually checked:

- Job appears and completes in the dashboard (`/ui/hangfire`); a recurring job shows its next execution.
- Running it twice over the same input changes nothing the second time.
- The queue name is one the server actually processes.
- The dashboard rejects an anonymous visitor and a signed-in storefront member.
- A forced failure surfaces in the dashboard rather than passing silently.

## Checklist

- [ ] Hangfire is the right system (not an Optimizely scheduled job)
- [ ] Payload is a `record` of ids, in `Models/`
- [ ] `[Queue]` names a queue in `HangfireOptions.Queues`; `[AutomaticRetry]` set deliberately
- [ ] `services.AddScoped<TheJob>()` registered
- [ ] Idempotent - terminal-state check, natural idempotency, or a distributed lock
- [ ] Enqueue is after the critical work and cannot roll it back
- [ ] `CancellationToken.None` at the enqueue site
- [ ] Recurring id permanent; cron slot does not collide
- [ ] No per-job failure email - the global filter handles it
- [ ] Tunables in `HangfireOptions`, not constants in the job
- [ ] Dashboard authorization untouched or still admin-gated
- [ ] Tests including the already-finished path; suite green

Details and code: `references/implementation.md`.
