---
name: optimizely-commerce-migration-step
description: Add a migration step to an Optimizely Commerce Connect (CMS 12) site - an IMigrationStep that runs once per environment to change catalog data, order-system metafields, property definitions, or database schema. Covers the Order number, progress reporting, idempotency and re-runnability, embedded SQL scripts with validating-query guards, the disposed-scope trap on long catalog walks, and why a returned false blocks every later step. Use whenever asked for a migration, a data backfill or fix-up, a metafield or schema change, a one-off correction to existing catalog content, or when a content-type attribute change needs forcing onto a database that already has rows.
---

# Adding a migration step

A migration step is an `IMigrationStep` that Commerce auto-migrate runs once per environment. It is the tool for changing **data that already exists** - catalog content, order metafields, property definitions, schema - as opposed to code-first content types, which sync on their own.

Migrations promote Integration → Preproduction → Production with the environments' approval gates, so a step will run against production data you cannot inspect first. Write it to be re-runnable and to survive bad rows.

## Step 1: Learn this repo's conventions first

```bash
ls src/Wisbar.Business/Migrations/
grep -h "public int Order =>" src/Wisbar.Business/Migrations/*.cs | sort -n | tail -5
ls src/Wisbar.Business/Migrations/Scripts/
ls src/Wisbar.Tests/Migrations/
```

Read the two most recent steps before writing. `references/templates.md` has the full code; the repo is the authority.

| Part | Location |
|---|---|
| Steps | `Wisbar.Business/Migrations/` |
| Embedded SQL | `Wisbar.Business/Migrations/Scripts/` |
| Seed data | `Wisbar.Business/Migrations/Data/` |
| Tests | `Wisbar.Tests/Migrations/` |

## Step 2: Decide whether you need one at all

You **do not** need a migration for a new content type, a new property on a content type, or a new page or block. Those are code-first and sync on startup.

You **do** need one for:

| Change | Why |
|---|---|
| Backfilling or correcting existing content | Nothing else touches rows already in the database |
| Changing a `[BackingType]` on an existing List/Json property | Optimizely's sync silently refuses the change on a database that already has rows; the attribute alone only works on a fresh database |
| Order-system metafields (line item, order, address) | Properties only survive `SaveAsPurchaseOrder` if a metafield exists on the meta-class |
| Column size or schema changes | Not expressible in content-type attributes |
| Seeding categories, settings, or reference data | One-time creation with stable identity |

If the change is code-first, say so and skip the migration rather than writing a no-op step.

## Step 3: Shape of a step

```csharp
[ServiceConfiguration(typeof(IMigrationStep))]
internal class Migration44DoTheThing : IMigrationStep
{
    public int Order => 1420;
    public string Name => "Do the thing";
    public string Description => "One or two sentences an operator can read in the migration log.";

    public bool Execute(IProgressMessenger progressMessenger) { ... }
}
```

- `[ServiceConfiguration(typeof(IMigrationStep))]` registers it; the class is `internal`.
- **Class name carries its number**: `Migration44DoTheThing`. Take the next unused number - there are 41 steps today, the highest `Order` is 1410.
- **`Order` is global and permanent.** Take the next free value (roughly `Order` 1420 next, following the ten-apart convention) and **never renumber an existing step** - environments record what has run by identity, and reordering re-runs or skips steps unpredictably.
- Lead with a comment explaining *why* the data is wrong and what the step does about it. Migration steps are read years later by someone diagnosing odd data; the existing steps all carry this.

## Step 4: Returning false blocks everything after it

`Execute` returns `bool`. A returned `false` marks the step failed **and stops the whole migration run** - every later step is blocked until it is resolved, in every environment it reaches.

So:

- Return `false` only for a genuine, must-fix failure of the step as a whole.
- A handful of bad rows is **not** that. Catch per item, count the failures, log them, and return `true` with an honest summary.
- "Nothing to do" is success: return `true` early, with a progress message saying so.

## Step 5: Idempotency

The step may run again - a re-deploy, a restored database, a partly-failed previous run. Write every step so a second run is a no-op:

- Check for the end state before writing (`EnsureField`, "does this category already exist", "is the value already migrated").
- SQL scripts use the validating-query guard (Step 7).
- Where a step both migrates rows and retires something afterwards, **only retire when nothing failed**. `Migration43` leaves its label category in place when any product failed, precisely so the step stays re-runnable and the failed rows stay resolvable in the editor.

## Step 6: Progress reporting

```csharp
progressMessenger.AddProgressMessageText("Collecting affected products", false, 15);
```

Signature is `(text, isError, percentComplete)`. Scale it across the work so the admin screen is not silent through a long walk:

```csharp
var percent = 15 + (int)((i + 1) / (double)products.Count * 80);
progressMessenger.AddProgressMessageText($"Processed {i + 1}/{products.Count} products", false, percent);
```

Log to `ILogger` as well - the progress messages are transient, the log is what survives.

## Step 7: SQL scripts

Schema changes go in an embedded `.sql` file, not inline strings.

1. Add `Migrations/Scripts/Migration44ThingScript.sql`.
2. Register it in `Wisbar.Business.csproj` **twice** - `<None Remove="..."/>` and `<EmbeddedResource Include="..."/>`. Miss the second and the step throws at runtime, not build time.
3. Guard it with a validating query so a re-run is a no-op:

```sql
--beginvalidatingquery
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
               WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'PromotedResult')
   SELECT 1, 'Creating PromotedResult'
ELSE
   SELECT 0, 'Already Done'
--endvalidatingquery
```

`1` runs the script, `0` skips it. Execute via `ScriptExecutor` with the `EcfSqlConnection` connection string read from `IConfiguration`, and load the script by its full manifest resource name.

## Step 8: The long catalog walk

Auto-migrate runs steps on a background task started from the first HTTP request. That request's scope is disposed long before a catalog walk finishes, so ambient state goes stale mid-run.

The pattern every walking step in this repo uses, first line of `Execute`:

```csharp
// Auto-migrate runs this on a background task whose ambient ServiceLocator.Current is the
// triggering request's scope - already disposed by the time a catalog walk finishes, so
// loading content throws ObjectDisposedException. Pin the locator to our own provider.
ServiceLocator.SetScopedServiceProvider(_serviceProvider);
```

Inject `IServiceProvider` to have one to pin. Combine it with a per-item try/catch, so one unloadable row logs and continues.

If you additionally hit **SQL connection** errors on a long walk - `Cannot create a command without an existing connection`, `Internal connection fatal error`, `There is already an open DataReader associated with this Connection` - that is a different cause: the database executor is cached against the request cache, not the DI scope, so pinning the locator does not address it. The deeper remedy is to detach the execution context for the walk, the way `OutOfRequestExecutor` does (`ExecutionContext.SuppressFlow()` around `Task.Run`). No migration in this repo currently does this, so treat it as a fix to apply if the symptom appears, not a default. **Do not** enable MARS - `MultipleActiveResultSets=False` is deliberate for CMS 12.

## Step 9: Test it

Migrations that transform data are testable - see `Wisbar.Tests/Migrations/`, which covers five of them. Extract the transformation into something callable and test it directly: the transform, the already-migrated no-op, and a malformed row that must be skipped rather than throw.

```bash
dotnet test src/Wisbar.Tests/Wisbar.Tests.csproj
```

## Step 10: Verify

```bash
dotnet build src/Wisbar.sln
```

Report which of these you actually checked:

- Step runs on a fresh local database and reports sensible progress and a truthful summary.
- Running it a second time changes nothing and still returns `true`.
- A deliberately broken row is skipped and counted, not fatal.
- The `Order` is unused and higher than every existing step.
- SQL script, if any, is embedded (both csproj entries) and its validating query returns 0 on a second run.
- The step's effect is what was actually asked for, on data resembling production - not just on an empty dev database.

State plainly that you have not run it against Integration, Preproduction, or Production; those runs are gated deployments and are not yours to trigger.

## Checklist

- [ ] A migration is genuinely needed (not a code-first change)
- [ ] `[ServiceConfiguration(typeof(IMigrationStep))]`, `internal` class, numbered name
- [ ] Next free `Order`; no existing step renumbered
- [ ] Leading comment explaining why the data is wrong
- [ ] Re-runnable; "nothing to do" returns `true` early
- [ ] Per-item try/catch with counts; `false` reserved for whole-step failure
- [ ] Progress scaled across the work, plus `ILogger` output
- [ ] SQL script embedded in csproj twice, with a validating-query guard
- [ ] Catalog walks pin the service provider and guard each item
- [ ] Test for the transformation where there is one
- [ ] Solution builds; tests pass

Full code templates: `references/templates.md`.
