# Optimizely scheduled job templates

Namespaces below use the Wisbar layout. Substitute the host project's namespaces - the repo you are working in is the authority, these are the shape.

## A. The job

`Wisbar.Business/ScheduledJobs/WidgetsImportJob.cs`. No DI registration - Optimizely discovers the attribute.

```csharp
using EPiServer.DataAbstraction;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using Microsoft.Extensions.Logging;
using Wisbar.Business.Synchronization.Interfaces;

namespace Wisbar.Business.ScheduledJobs;

[ScheduledPlugIn(
    GUID = JobGuid,
    DisplayName = "[Wisbar] Import Widgets",
    Description = "Imports widgets from the iMIS system.",
    SortIndex = 4,
    InitialTime = "01:00:00",           // stagger against sibling jobs hitting the same system
    IntervalType = ScheduledIntervalType.Hours,
    IntervalLength = 6,
    DefaultEnabled = true,
    Restartable = true)]
internal class WidgetsImportJob : ScheduledJobBase
{
    // Exposed so other jobs can read this job's history through IScheduledJobLogRepository.
    internal const string JobGuid = "{PASTE-A-FRESH-GUID-HERE}";

    private readonly IWidgetImportService _importService;
    private readonly ILogger<WidgetsImportJob> _log;
    private bool _stopSignaled;

    public WidgetsImportJob(IWidgetImportService importService, ILogger<WidgetsImportJob> log)
    {
        _importService = importService;
        _log = log;
        IsStoppable = true;
    }

    public override string Execute()
    {
        try
        {
            // Execute() is synchronous; the existing jobs bridge async work this way.
            var result = _importService.ImportAsync(
                status => OnStatusChanged(status),
                () => _stopSignaled
            ).GetAwaiter().GetResult();

            // This string is what the admin UI shows and stores in history. Make it count.
            var message = $"Import complete. Processed: {result.Processed}, " +
                          $"Created: {result.Created}, Updated: {result.Updated}, " +
                          $"Skipped: {result.Skipped}, Failed: {result.Failed}";

            _log.LogInformation(message);
            return message;
        }
        catch (Exception ex)
        {
            // Log AND rethrow: the throw is what marks the run failed.
            _log.LogError(ex, "An error occurred while importing widgets.");
            throw;
        }
    }

    public override void Stop()
    {
        _stopSignaled = true;
    }
}
```

## B. `[ScheduledPlugIn]` fields

| Field | Notes |
|---|---|
| `GUID` | Stable forever. Keys the schedule and history rows; changing it re-registers the job as a new one |
| `DisplayName` | Prefixed `[Wisbar]` so site jobs group away from Optimizely's built-ins |
| `Description` | Shown in the admin list. Editor/operator-facing copy: hyphens, not em dashes |
| `SortIndex` | Order in the admin list. Existing: Profiles 1, Products 2, ImportLogsNotification 3 |
| `InitialTime` | `"HH:mm:ss"`. Stagger jobs that share an external system |
| `IntervalType` | `ScheduledIntervalType`. This repo uses `Hours` and `Days`; check the enum for the rest |
| `IntervalLength` | Count of the above |
| `DefaultEnabled` | Whether it is on when first registered. `false` for expensive or destructive jobs |
| `Restartable` | Whether the scheduler may restart it after an unclean shutdown |

`InitialTime`, `IntervalType`, `IntervalLength`, and `DefaultEnabled` apply **only at first registration**. On a database where the job already exists, the admin UI owns them.

## C. `ScheduledJobBase` members

| Member | Use |
|---|---|
| `string Execute()` | The work. Return the operator-facing summary; throw to fail the run |
| `void Stop()` | Override to set a stop flag. Called when an operator presses Stop |
| `bool IsStoppable` | Set `true` in the constructor to enable the Stop button |
| `OnStatusChanged(string)` | Push a progress line to the admin screen |

## D. Stop and progress, wired through to the service

The job owns the signal; the service decides where it is safe to honour it.

```csharp
// Job
var result = _importService.ImportAsync(
    status => OnStatusChanged(status),
    () => _stopSignaled
).GetAwaiter().GetResult();

public override void Stop() => _stopSignaled = true;
```

```csharp
// Service - checks between items, never mid-write
public async Task<ImportResult> ImportAsync(Action<string> onStatus, Func<bool> stopRequested)
{
    foreach (var batch in batches)
    {
        if (stopRequested())
        {
            onStatus($"Stopped after {processed} of {total}.");
            break;
        }

        onStatus($"Processing {processed} of {total}...");
        // ...
    }
}
```

A stopped run should still return a truthful summary of what it did before stopping.

## E. Running without a request context

There is no `HttpContext` in a job. Resolve the site explicitly instead of reading ambient state:

```csharp
private SiteSettingsPage? LoadSiteSettings()
{
    // GetByHostname returns null when SiteHost matches no registered site (wildcard off); guard
    // so a config/host mismatch skips the optional settings read instead of aborting the import.
    var startPage = _siteDefinitionResolver
        .GetByHostname(new Uri(_contentBuilderOptions.SiteHost).Authority, false)?.StartPage;

    return ContentReference.IsNullOrEmpty(startPage)
        ? null
        : _contentRepository.GetChildren<SiteSettingsPage>(startPage).FirstOrDefault();
}
```

Do **not** rely on `SiteDefinition.Current`, `ContentLanguage.PreferredCulture`, or the current principal inside a job. Pass culture and identity explicitly.

## F. Long catalog walks

```csharp
public override string Execute()
{
    // Detach from any ambient request context: the IDatabaseExecutor is cached against a request
    // cache that can be disposed mid-walk, producing "Cannot create a command without an existing
    // connection" / "Internal connection fatal error" / open-DataReader errors.
    using (ExecutionContext.SuppressFlow())
        return Task.Run(Sweep).GetAwaiter().GetResult();
}

private string Sweep()
{
    var processed = 0;
    var skipped = 0;

    foreach (var link in links)
    {
        if (_stopSignaled) break;

        try
        {
            if (!_contentLoader.TryGet<VariationContent>(link, out var item)) { skipped++; continue; }
            // ... work
            processed++;
        }
        catch (Exception ex)
        {
            // One bad row must not abort the whole pass.
            skipped++;
            _log.LogWarning(ex, "Skipping {Link}.", link);
        }
    }

    return $"Swept {processed} items, skipped {skipped}.";
}
```

`ServiceLocator.SetScopedServiceProvider` does not fix this. Never enable MARS.

## G. Reading job history

`IScheduledJobLogRepository` is in **`EPiServer.Scheduler`** (not `EPiServer.DataAbstraction`).

```csharp
private const int ExecutionsToReport = 4;

private static readonly (string Name, Guid Id)[] MonitoredJobs =
[
    ("Import Products", new Guid(ProductsImportJob.JobGuid)),
    ("Import Profiles", new Guid(ProfilesImportJob.JobGuid)),
];

var page = await _logRepository.GetAsync(id, 0, ExecutionsToReport);
var logs = page.PagedResult
    .OrderByDescending(l => l.CompletedUtc)
    .Take(ExecutionsToReport)
    .ToList();
```

- `PagedScheduledJobLogResult.PagedResult` holds the items; `.TotalCount` the total.
- `ScheduledJobLogItem`: `Status` (`ScheduledJobExecutionStatus`: Unknown / Succeeded / Failed / Cancelled / UnableToStart / Aborted), `Message`, `CompletedUtc`, `Duration`, `Trigger`, `Server`.

HTML-encode `Message` before emailing it:

```csharp
$"<pre style=\"white-space:pre-wrap;margin:0;\">{WebUtility.HtmlEncode(log.Message)}</pre>"
```

## H. Operator settings

On `SiteSettingsPage`, under the jobs tab:

```csharp
[Display(
    Name = "Widgets Import Start Date",
    GroupName = Globals.Tabs.ScheduleJobSettings,
    Order = 230)]
public virtual DateTime? WidgetsImportStartDate { get; set; }
```

Existing entries: `ProductsImportStartDate` (200), `ProfilesImportStartDate` (210), `NotificationEmailAddresses` (220). Recipient lists are read through `INotificationRecipientProvider`, which merges the Site Settings list with `EmailOptions.DefaultToAddress`:

```csharp
var recipients = new List<string>(_emailOptions.DefaultToAddress);
recipients.AddRange(_recipientProvider.GetNotificationEmailAddresses());

if (recipients.Count == 0)
{
    _log.LogWarning("No notification email addresses configured in Site Settings; skipping.");
    return "No notification recipients configured in Site Settings. Email not sent.";
}
```

Note the shape of that early return: a job that cannot do its work reports *why* in the status string rather than silently succeeding.

## I. Testing

The job shell is thin by design - test the service it delegates to, in `Wisbar.Tests/`, with MSTest:

```csharp
[TestClass]
public class WidgetImportServiceTests
{
    [TestMethod]
    public async Task ImportAsync_StopRequested_StopsBetweenBatchesAndReportsProgress()
    {
        // stopRequested returns true after the first batch; assert the run ends cleanly
        // and the result counts reflect only what was actually processed.
    }
}
```

Worth covering: the stop path, an item-level failure that must not abort the pass, and the "nothing to do" path that produces the explanatory status string.
