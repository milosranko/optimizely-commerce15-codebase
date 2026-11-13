---
name: optimizely-dds-store
description: Add a Dynamic Data Store (DDS) to an Optimizely CMS 12 (.NET) site - an IDynamicData entity with [EPiServerDataStore], its store-name constant, a typed store service over DynamicDataStoreFactory, and DI registration. Covers what the shared big table can actually hold, which properties must be indexed, the indexed-only LINQ rule, why nullable value types and large payloads are avoided, UTC round-tripping, and retention. Use whenever asked to persist small structured state outside the CMS content tree - sync cursors, run history, job or import state, feature flags, counters - or when asked about DDS, tblBigTable, or DynamicDataStore.
---

# Adding a DDS store

The Dynamic Data Store persists small structured records outside the content tree, in **one table shared by every store in the CMS** (`tblBigTable`). That sharing is the source of every rule below: a row has a fixed column budget, queries are weak, and a careless store degrades the table for everything else.

## Step 0: Should this be DDS at all?

DDS is right for **small, scalar, operational state**: a sync cursor, a run history row, a flag. It is wrong for:

| Instead of | Use |
|---|---|
| Editorial content | A content type - page, block, or a settings page property |
| Large payloads (documents, results, files) | An Optimizely blob, with only the URI in DDS |
| Anything needing real querying, joins, or reporting | A dedicated SQL table via a migration step |
| Per-request or short-lived data | `ICacheService` |

The import feature is the worked example: run *metadata* lives in DDS, while each run's full result is written to a blob and the row keeps only `ResultBlobUri`. Do the same rather than putting a large string in a `String` column, even though those columns are `nvarchar(MAX)` and would technically fit.

Say so plainly if the request would be better served by one of the alternatives above.

## Step 1: Learn this repo's conventions first

```bash
ls src/Wisbar.Business/Infrastructure/DDS/
grep -rn "DynamicDataStoreName" src/Wisbar.Optimizely.Core/Constants/WisbarConstants.cs
grep -rln "DynamicDataStoreFactory" --include=*.cs src/
```

Read `ImportRunEntity` and `DdsImportRunStore` end to end - together they are the house pattern. `references/templates.md` has the full code.

| Part | Location |
|---|---|
| Entities | `Wisbar.Business/Infrastructure/DDS/` |
| Store names | `WisbarConstants.DynamicDataStoreName` |
| Store services | `Wisbar.Business/Synchronization/Services/` (e.g. `DdsImportRunStore`) |
| DI | `Startup.ConfigureServices` |

Existing stores: `iMISProductSyncState`, `iMISProfileSyncState`, `WisbarImportRun`.

## Step 2: The entity

```csharp
[EPiServerDataStore(AutomaticallyRemapStore = true, StoreName = DynamicDataStoreName.MyStore)]
internal class MyEntity : IDynamicData
{
    public Identity Id { get; set; } = Identity.NewIdentity();
    ...
}
```

- `IDynamicData` with an `Identity Id`. `AutomaticallyRemapStore = true` lets the store absorb added properties without a manual remap.
- **Store name goes in `WisbarConstants.DynamicDataStoreName`**, never inline. It is the persistent key; renaming it orphans every existing row.
- Keep the entity `internal` and scalar-only. Map to a public model at the store boundary, the way `DdsImportRunStore.Map` does - callers should not hold DDS types.
- **When a natural id exists, make it the identity**: `Identity.NewIdentity(runId)` lets a record be loaded straight from a URL with no secondary lookup.

## Step 3: Index what you filter on

```csharp
[EPiServerDataIndex]
public string State { get; set; } = string.Empty;
```

**DDS LINQ only supports `Where` on indexed properties.** Filter on those in the query, call `.ToList()`, then do everything else in memory:

```csharp
Store.Items<ImportRunEntity>()
    .Where(x => x.CreatedUtc >= staleAfterUtc)   // indexed - runs in SQL
    .ToList()                                     // everything after this is in-memory
    .Where(x => x.State == queued)
    .OrderByDescending(x => x.CreatedUtc)
    .FirstOrDefault();
```

Getting this wrong does not always throw - it can quietly materialize the store. Pick indexed properties so the SQL-side filter is genuinely selective, and keep the set small: the indexed budget is **3 columns each** for String, Integer, DateTime, Guid, and Boolean.

## Step 4: Respect the column budget

Per-row budget in `tblBigTable`, verified against this project's database (CMS 12 / EPiServer.Framework 12.24):

| Type | Plain | Indexed |
|---|---|---|
| String | 10 | 3 |
| Integer | 10 | 3 |
| DateTime | 5 | 1 |
| Boolean | 5 | 1 |
| Guid | 3 | 1 |
| Long | 5 | - |
| Float | 7 | - |
| Decimal | 2 | - |
| Binary | 5 | - |

`String01..String10` are `nvarchar(MAX)`; only `Indexed_String01..03` are capped at 450 characters. Count your properties against this before adding the eleventh string. If an entity is straining the budget, that is the signal it belongs in its own SQL table, not DDS.

## Step 5: Two conventions that look like bugs but are not

**No nullable value types.** No store in this database maps one (`tblBigTableReference` has zero `Nullable` rows). Entities use a plain scalar with `default(DateTime)` as the "unset" sentinel and map it to `null` at the boundary:

```csharp
private static DateTime? AsUtcOrNull(DateTime value) => value == default ? null : AsUtc(value);
```

Do not make a new store the first to rely on nullable support.

**DateTime kind is not preserved.** Write UTC, and re-tag it on the way out, or JSON and Razor will render the value as local time:

```csharp
private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
```

Also truncate anything unbounded before saving (`Truncate(status, 400)`), so a runaway message cannot bloat a shared row.

## Step 6: The store service

Wrap the store in a service behind an interface; nothing outside it should touch `DynamicDataStoreFactory`.

```csharp
// Write path: create on first use.
private static DynamicDataStore Store =>
    DynamicDataStoreFactory.Instance.GetStore(DynamicDataStoreName.MyStore)
    ?? DynamicDataStoreFactory.Instance.CreateStore(DynamicDataStoreName.MyStore, typeof(MyEntity));
```

`GetStore` returns **null** when the store has never been created. Use `?? CreateStore(...)` on write paths; on a read-only path a null-check that returns "no data" is fine and avoids creating an empty store just to read it.

Update is read-modify-write. There is no optimistic concurrency, so be explicit about why concurrent writers cannot collide - `DdsImportRunStore.Update` carries exactly that note. If two writers genuinely can overlap, DDS is the wrong store.

Register in `Startup.ConfigureServices`: `.AddScoped<IMyStore, DdsMyStore>()`.

## Step 7: Retention

Rows in a shared table are forever unless something removes them. Any store that grows per event needs a retention path - `GetPurgeable(cutoff, max)` plus `Purge(ids)`, driven by a recurring job, as `ImportRetentionJob` does for import runs at 30 days. State the retention policy explicitly, or say why the store is bounded (a sync cursor holds one row).

## Step 8: Verify

```bash
dotnet build src/Wisbar.sln
dotnet test src/Wisbar.Tests/Wisbar.Tests.csproj
```

Report which of these you actually checked:

- Save then load round-trips every property, with UTC timestamps still UTC.
- Queries filter on indexed properties before `.ToList()`.
- Property count fits the column budget for each type.
- A store that grows has a purge path, and it deletes what it should.
- Nothing large landed in a `String` column that belongs in a blob.

## Checklist

- [ ] DDS is genuinely the right store (Step 0)
- [ ] Entity is `internal`, `IDynamicData`, with `Identity Id`
- [ ] `[EPiServerDataStore(AutomaticallyRemapStore = true, StoreName = ...)]`
- [ ] Store name constant in `WisbarConstants.DynamicDataStoreName`
- [ ] Natural id used as the identity where one exists
- [ ] `[EPiServerDataIndex]` on every property filtered in a query; within the 3-column budget
- [ ] All query `Where` clauses before `.ToList()` use indexed properties only
- [ ] Property count within the per-type column budget
- [ ] No nullable value types; `default` sentinel mapped to null at the boundary
- [ ] UTC re-tagged on read; unbounded strings truncated on write
- [ ] Store service behind an interface, registered in DI; entity type never leaks to callers
- [ ] Retention path for anything that grows
- [ ] Solution builds; tests pass

Full code templates: `references/templates.md`.
