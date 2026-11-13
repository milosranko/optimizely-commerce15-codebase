# DDS store templates

Namespaces below use the Wisbar layout. Substitute the host project's namespaces - the repo you are working in is the authority, these are the shape.

## A. Minimal store - a single-row cursor

The simplest useful DDS store: one row holding a sync cursor.

### Entity - `Wisbar.Business/Infrastructure/DDS/WidgetsSyncState.cs`

```csharp
using EPiServer.Data;
using EPiServer.Data.Dynamic;
using static Wisbar.Optimizely.Core.Constants.WisbarConstants;

namespace Wisbar.Business.Infrastructure.DDS;

[EPiServerDataStore(
    AutomaticallyRemapStore = true,
    StoreName = DynamicDataStoreName.WidgetSyncStateStore)]
internal class WidgetsSyncState : IDynamicData
{
    public Identity Id { get; set; } = Identity.NewIdentity();
    public DateTime LastSyncDate { get; set; }
}
```

### Store name constant - `WisbarConstants.cs`

```csharp
public static class DynamicDataStoreName
{
    public const string ProfileSyncStateStore = "iMISProfileSyncState";
    public const string ProductSyncStateStore = "iMISProductSyncState";
    public const string ImportRunStore = "WisbarImportRun";
    public const string WidgetSyncStateStore = "WisbarWidgetSyncState";   // new
}
```

The string is the persistent key. Renaming it orphans every existing row.

### Read and write

```csharp
// Read-only: GetStore returns null when the store has never been created. Do not create it
// just to read - a missing store simply means "no cursor yet".
private static DateTime? GetLastSyncDate()
{
    var store = DynamicDataStoreFactory.Instance.GetStore(DynamicDataStoreName.WidgetSyncStateStore);
    return store?.Items<WidgetsSyncState>().FirstOrDefault()?.LastSyncDate;
}

// Write: create on first use.
private static void SaveLastSyncDate(DateTime syncDate)
{
    var store = DynamicDataStoreFactory.Instance.GetStore(DynamicDataStoreName.WidgetSyncStateStore)
                ?? DynamicDataStoreFactory.Instance.CreateStore(
                    DynamicDataStoreName.WidgetSyncStateStore, typeof(WidgetsSyncState));

    var state = store.Items<WidgetsSyncState>().FirstOrDefault();

    if (state != null)
    {
        state.LastSyncDate = syncDate;
        store.Save(state);
    }
    else
    {
        store.Save(new WidgetsSyncState { LastSyncDate = syncDate });
    }
}

// Reset: delete the rows rather than the store.
public void ResetSyncDate()
{
    var store = DynamicDataStoreFactory.Instance.GetStore(DynamicDataStoreName.WidgetSyncStateStore);
    if (store == null)
        return;

    foreach (var item in store.Items<WidgetsSyncState>().ToList())
        store.Delete(item.Id);
}
```

## B. History store - the full pattern

### Entity - `Wisbar.Business/Infrastructure/DDS/WidgetRunEntity.cs`

```csharp
using EPiServer.Data;
using EPiServer.Data.Dynamic;
using static Wisbar.Optimizely.Core.Constants.WisbarConstants;

namespace Wisbar.Business.Infrastructure.DDS;

// Execution history for every widget run. Scalars only - results are far too large for a row in the
// shared big table, so they live in a blob this record points at.
[EPiServerDataStore(
    AutomaticallyRemapStore = true,
    StoreName = DynamicDataStoreName.WidgetRunStore)]
internal class WidgetRunEntity : IDynamicData
{
    public Identity Id { get; set; } = Identity.NewIdentity();

    // Indexed: every lookup filters on kind or state, or orders by creation time.
    // Budget is 3 indexed columns per type - spend them on what the queries actually filter.
    [EPiServerDataIndex]
    public string RunType { get; set; } = string.Empty;

    [EPiServerDataIndex]
    public string State { get; set; } = string.Empty;

    [EPiServerDataIndex]
    public DateTime CreatedUtc { get; set; }

    public string? JobId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? ResultBlobUri { get; set; }     // pointer, not payload

    public int Processed { get; set; }
    public string? StatusMessage { get; set; }

    // No existing store here maps a nullable value type, so "unset" is default(DateTime) and the
    // store maps it back to null rather than making this the first one to rely on that.
    public DateTime StartedUtc { get; set; }
    public DateTime FinishedUtc { get; set; }

    public string? ErrorMessage { get; set; }
}
```

### Store service - `DdsWidgetRunStore.cs`

```csharp
using EPiServer.Data;
using EPiServer.Data.Dynamic;
using Wisbar.Business.Infrastructure.DDS;
using static Wisbar.Optimizely.Core.Constants.WisbarConstants;

namespace Wisbar.Business.Features.Widgets.Services;

/// <summary>
/// <see cref="IWidgetRunStore"/> over the Dynamic Data Store. The run id doubles as the DDS identity,
/// so a run is addressable straight from the URL without a secondary lookup.
/// </summary>
public sealed class DdsWidgetRunStore : IWidgetRunStore
{
    private static DynamicDataStore Store =>
        DynamicDataStoreFactory.Instance.GetStore(DynamicDataStoreName.WidgetRunStore)
        ?? DynamicDataStoreFactory.Instance.CreateStore(
            DynamicDataStoreName.WidgetRunStore, typeof(WidgetRunEntity));

    public void Create(Guid runId, string runType, string userName)
    {
        Store.Save(new WidgetRunEntity
        {
            // The caller's id becomes the identity - no secondary lookup, no duplicate key column.
            Id = Identity.NewIdentity(runId),
            RunType = runType,
            State = nameof(WidgetRunState.Queued),
            UserName = userName,
            CreatedUtc = DateTime.UtcNow
        });
    }

    public WidgetRun? Get(Guid runId)
    {
        var entity = Store.Load<WidgetRunEntity>(Identity.NewIdentity(runId));
        return entity is null ? null : Map(entity);
    }

    public WidgetRun? GetActive(DateTime staleAfterUtc)
    {
        var queued = nameof(WidgetRunState.Queued);
        var running = nameof(WidgetRunState.Running);

        var entity = Store.Items<WidgetRunEntity>()
            .Where(x => x.CreatedUtc >= staleAfterUtc)   // indexed - the only SQL-side filter
            .ToList()                                    // everything below runs in memory
            .Where(x => x.State == queued || x.State == running)
            .OrderByDescending(x => x.CreatedUtc)
            .FirstOrDefault();

        return entity is null ? null : Map(entity);
    }

    public void MarkRunning(Guid runId) => Update(runId, entity =>
    {
        entity.State = nameof(WidgetRunState.Running);
        entity.StartedUtc = DateTime.UtcNow;
    });

    public void ReportStatus(Guid runId, string status) =>
        Update(runId, entity => entity.StatusMessage = Truncate(status, 400));

    public void MarkFailed(Guid runId, string error) => Update(runId, entity =>
    {
        entity.State = nameof(WidgetRunState.Failed);
        entity.ErrorMessage = Truncate(error, 2000);
        entity.FinishedUtc = DateTime.UtcNow;
    });

    public IReadOnlyList<WidgetRun> GetPurgeable(DateTime cutoffUtc, int max) =>
        Store.Items<WidgetRunEntity>()
            .Where(x => x.CreatedUtc < cutoffUtc)
            .ToList()
            .Where(x => x.FinishedUtc != default)
            .OrderBy(x => x.CreatedUtc)
            .Take(max)
            .Select(Map)
            .ToList();

    public int Purge(IReadOnlyCollection<Guid> runIds)
    {
        var store = Store;
        var purged = 0;

        foreach (var runId in runIds)
        {
            store.Delete(Identity.NewIdentity(runId));
            purged++;
        }

        return purged;
    }

    // Read-modify-write against a single record. DDS has no optimistic concurrency, so state WHY two
    // writers cannot overlap: only the job writes a run once it is under way, and the enqueueing
    // request is finished before that starts.
    private static void Update(Guid runId, Action<WidgetRunEntity> apply)
    {
        var store = Store;
        var entity = store.Load<WidgetRunEntity>(Identity.NewIdentity(runId));

        if (entity is null)
            return;

        apply(entity);
        store.Save(entity);
    }

    // Map at the boundary: the DDS entity never leaves this class.
    private static WidgetRun Map(WidgetRunEntity entity) => new()
    {
        RunId = entity.Id.ExternalId,
        RunType = entity.RunType,
        State = Enum.Parse<WidgetRunState>(entity.State),
        UserName = entity.UserName,
        ResultBlobUri = entity.ResultBlobUri,
        Processed = entity.Processed,
        StatusMessage = entity.StatusMessage,
        ErrorMessage = entity.ErrorMessage,
        CreatedUtc = AsUtc(entity.CreatedUtc),
        StartedUtc = AsUtcOrNull(entity.StartedUtc),
        FinishedUtc = AsUtcOrNull(entity.FinishedUtc)
    };

    // Every timestamp is written in UTC; the tag has to be restored or JSON and Razor would render
    // these as local times. default(DateTime) is the entity's stand-in for "not set".
    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static DateTime? AsUtcOrNull(DateTime value) => value == default ? null : AsUtc(value);

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
```

### DI

```csharp
// Startup.ConfigureServices
.AddScoped<IWidgetRunStore, DdsWidgetRunStore>()
```

## C. Large payloads go to a blob

Never put a document, a result set, or an uploaded file in a DDS string column, even though `String01..String10` are `nvarchar(MAX)`. Write a blob and store the URI:

```csharp
var resultBlobUri = await WidgetResultBlobs.WriteAsync(_blobFactory, run.RunId, outcome.Result, ct);
_runStore.MarkSucceeded(run.RunId, outcome.Counters, resultBlobUri.ToString());
```

Clean up the blob when the row is purged - `ImportRetentionJob` deletes both the result blob and any source archive an aborted run left behind before purging the DDS rows.

## D. Retention job

Anything that grows per event needs one. See the `optimizely-scheduled-job` skill for the job itself; the store side is `GetPurgeable` + `Purge` as in section B, driven from a recurring job:

```csharp
var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);
var runs = _runStore.GetPurgeable(cutoff, BatchSize);

foreach (var run in runs)
    DeleteBlob(run, run.ResultBlobUri);

var purged = _runStore.Purge(runs.Select(r => r.RunId).ToList());
```

## Storage limits

Per-row column budget in `tblBigTable`, verified against this project's local database (CMS 12 / EPiServer.Framework 12.24):

| Type | Plain columns | Indexed columns |
|---|---|---|
| String | 10 (`nvarchar(MAX)`) | 3 (capped at 450 chars) |
| Integer | 10 | 3 |
| DateTime | 5 | 1 |
| Boolean | 5 | 1 |
| Guid | 3 | 1 |
| Long | 5 | - |
| Float | 7 | - |
| Decimal | 2 | - |
| Binary | 5 | - |

Rules that follow from the table being shared by every store in the CMS:

- Exceeding a budget is a design signal, not a limit to work around - move to a dedicated SQL table via a migration step.
- `Where` in a DDS query only works on properties mapped to indexed columns. Filter on those, `.ToList()`, then continue in memory.
- No nullable value types - nothing in this database maps one.
- Large payloads belong in blobs, with only the pointer in DDS.
