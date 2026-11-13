# Migration step templates

Namespaces below use the Wisbar layout. Substitute the host project's namespaces - the repo you are working in is the authority, these are the shape.

## A. Data fix-up over the catalog

The most common shape: walk existing content, correct it, report honestly.

```csharp
using EPiServer;
using EPiServer.Commerce.Internal.Migration.Steps;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Shared;
using Microsoft.Extensions.Logging;

namespace Wisbar.Business.Migrations;

// WHY the data is wrong, and what this does about it. Written for whoever reads it in two years
// while diagnosing odd data - not a restatement of the class name.
[ServiceConfiguration(typeof(IMigrationStep))]
internal class Migration44FixWidgetTitles : IMigrationStep
{
    public int Order => 1420;                    // next free; never renumber an existing step
    public string Name => "Fix widget titles";
    public string Description =>
        "Trims the stray whitespace the pre-2026 importer left on widget titles across every " +
        "already-imported product.";

    private readonly ILogger<Migration44FixWidgetTitles> _log;
    private readonly IContentRepository _contentRepository;
    private readonly IContentLoader _contentLoader;
    private readonly ReferenceConverter _referenceConverter;
    private readonly IServiceProvider _serviceProvider;

    public Migration44FixWidgetTitles(
        ILogger<Migration44FixWidgetTitles> log,
        IContentRepository contentRepository,
        IContentLoader contentLoader,
        ReferenceConverter referenceConverter,
        IServiceProvider serviceProvider)
    {
        _log = log;
        _contentRepository = contentRepository;
        _contentLoader = contentLoader;
        _referenceConverter = referenceConverter;
        _serviceProvider = serviceProvider;
    }

    public bool Execute(IProgressMessenger progressMessenger)
    {
        // Auto-migrate runs this on a background task whose ambient ServiceLocator.Current is the
        // triggering request's scope - already disposed by the time a catalog walk finishes, so
        // loading content throws ObjectDisposedException. Pin the locator to our own provider.
        ServiceLocator.SetScopedServiceProvider(_serviceProvider);

        try
        {
            progressMessenger.AddProgressMessageText("Collecting affected products", false, 5);

            var products = GetProductsToUpdate();

            // Nothing to do is success, not failure.
            if (products.Count == 0)
            {
                const string nothing = "Widget title fix: nothing to correct.";
                _log.LogInformation(nothing);
                progressMessenger.AddProgressMessageText(nothing, false, 100);
                return true;
            }

            int updated = 0, failed = 0;

            for (var i = 0; i < products.Count; i++)
            {
                var product = products[i];

                try
                {
                    UpdateProduct(product);
                    updated++;
                }
                catch (Exception ex)
                {
                    // One bad row must not fail the step - a returned false blocks every later step.
                    failed++;
                    _log.LogWarning(ex, "Widget title fix: failed to save product {ProductId}.",
                        product.ContentLink.ID);
                }

                // Scale progress between 5 and 95 across the products.
                var percent = 5 + (int)((i + 1) / (double)products.Count * 90);
                progressMessenger.AddProgressMessageText(
                    $"Processed {i + 1}/{products.Count} products", false, percent);
            }

            var summary = $"Widget title fix complete: {updated} updated, {failed} failed.";
            _log.LogInformation(summary);
            progressMessenger.AddProgressMessageText(summary, false, 100);

            return true;
        }
        catch (Exception ex)
        {
            // Whole-step failure: the data could not be enumerated at all.
            _log.LogError(ex, "Widget title fix failed.");
            progressMessenger.AddProgressMessageText("Widget title fix failed", true, 100);
            return false;
        }
    }

    private void UpdateProduct(WidgetProduct product)
    {
        var writable = product.CreateWritableClone() as WidgetProduct;
        writable!.Title = writable.Title?.Trim();

        _contentRepository.Save(writable, SaveAction.Publish | SaveAction.ForceCurrentVersion,
            AccessLevel.NoAccess);
    }
}
```

### Retiring something only when nothing failed

When a step both migrates rows and cleans up after itself, the cleanup is conditional - otherwise a re-run cannot fix the rows that failed:

```csharp
// A product that failed still carries the label, and removing the category would leave its row
// unresolvable in the editor drop-down. The step is re-runnable, so leave it.
if (failed == 0)
    RetireAccordionLabelCategory();
else
    _log.LogWarning("{Failed} product(s) failed, so the label category was left in place.", failed);
```

## B. Embedded SQL script

### The step

```csharp
[ServiceConfiguration(typeof(IMigrationStep))]
internal class Migration45WidgetTable : IMigrationStep
{
    public int Order => 1430;
    public string Name => "Create widget table";
    public string Description => "Creates dbo.Widget and its unique index.";

    private readonly ILogger<Migration45WidgetTable> _log;

    public Migration45WidgetTable(ILogger<Migration45WidgetTable> log) => _log = log;

    public bool Execute(IProgressMessenger progressMessenger)
    {
        var connString = ServiceLocator.Current.GetInstance<IConfiguration>()
            .GetConnectionString("EcfSqlConnection");
        var scriptExecutor = ServiceLocator.Current.GetInstance<ScriptExecutor>();

        try
        {
            progressMessenger.AddProgressMessageText("Creating widget table", false, 10);

            using (var stream = GetInstallScript())
            {
                scriptExecutor.ExecuteScript(connString, stream);
            }

            progressMessenger.AddProgressMessageText("Finished creating widget table", false, 100);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "An error occurred while creating the widget table.");
            progressMessenger.AddProgressMessageText("Error creating widget table", true, 100);
            return false;
        }

        return true;
    }

    private static Stream GetInstallScript()
    {
        // Full manifest resource name: <RootNamespace>.<folder path with dots>.<file>
        var scriptName = "Wisbar.Business.Migrations.Scripts.Migration45WidgetTableScript.sql";
        var assembly = typeof(Migration45WidgetTable).Assembly;

        return assembly.GetManifestResourceStream(scriptName)!;
    }
}
```

### The script - `Migrations/Scripts/Migration45WidgetTableScript.sql`

The validating query is the idempotency guard. `1` runs the body, `0` skips it.

```sql
--beginvalidatingquery
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
               WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Widget')
   SELECT 1, 'Creating Widget'
ELSE
   SELECT 0, 'Already Done'
--endvalidatingquery

BEGIN

    BEGIN TRANSACTION

    CREATE TABLE dbo.Widget
    (
        Id               INT IDENTITY NOT NULL
            CONSTRAINT PK_Widget PRIMARY KEY,
        -- Trimmed + lowercased Name; the unique index is the DB-level guarantee.
        NormalizedName   NVARCHAR(450) NOT NULL,
        CreatedUtc       DATETIME2(3) NOT NULL
    )

    CREATE UNIQUE NONCLUSTERED INDEX UX_Widget_NormalizedName
        ON dbo.Widget (NormalizedName)

    COMMIT TRANSACTION

END
```

### csproj - both entries, or it fails at runtime

```xml
<ItemGroup>
  <None Remove="Migrations\Scripts\Migration45WidgetTableScript.sql" />
</ItemGroup>

<ItemGroup>
  <EmbeddedResource Include="Migrations\Scripts\Migration45WidgetTableScript.sql" />
</ItemGroup>
```

## C. Order-system metafield

Line item, order, and address `Properties` entries survive `SaveAsPurchaseOrder` only if a metafield exists on the meta-class.

```csharp
public bool Execute(IProgressMessenger progressMessenger)
{
    try
    {
        progressMessenger.AddProgressMessageText("Adding widget meta fields", false, 10);

        var context = OrderContext.MetaDataContext;
        var lineItemMetaClass = OrderContext.Current.LineItemMetaClass
            ?? throw new InvalidOperationException("LineItem meta-class is unavailable; order system not configured.");

        // Short catalog code identifying the widget this line came from.
        OrderMetaFieldHelper.EnsureField(context, lineItemMetaClass, LineItemProperties.WidgetCode,
            "Widget Code", MetaDataType.ShortString, 100);

        // Free-form JSON map - nvarchar(max) via LongString.
        OrderMetaFieldHelper.EnsureField(context, lineItemMetaClass, LineItemProperties.WidgetOptions,
            "Widget Options", MetaDataType.LongString, 0);

        progressMessenger.AddProgressMessageText("Finished adding widget meta fields", false, 100);
        return true;
    }
    catch (Exception ex)
    {
        _log.LogError(ex, "Failed to add widget meta fields.");
        return false;
    }
}
```

`EnsureField` is the idempotency: it is a no-op when the field already exists. Key names come from `Globals.LineItemProperties` and are **prefixed**, because the LineItem meta-class is shared and generic names collide with native fields.

## D. Forcing a content-type property change onto an existing database

A `[BackingType]` change on a List/Json property is silently refused by Optimizely's content-type sync when the property already exists. The attribute alone only takes effect on a brand-new database.

```csharp
// SecondaryAccordion's PropertyDefinition is List/Json-kind, and EPiServer's own content-type sync
// silently refuses to change the backing type of an already-existing List/Json property. A
// [BackingType] attribute change alone only takes effect on a brand-new database; this step forces
// it on databases that already have the rows.
[ServiceConfiguration(typeof(IMigrationStep))]
internal class Migration46ChangeWidgetTabsBackingType : IMigrationStep
{
    public int Order => 1440;
    public string Name => "Change Widget Tabs backing type";
    public string Description =>
        "Forces the Tabs PropertyDefinition on every widget content type onto the new metadata type.";

    private const string PropertyName = "Tabs";

    private static readonly Type[] ContentTypes =
    [
        typeof(WidgetProduct),
        typeof(GadgetProduct)
    ];

    private readonly IContentTypeRepository _contentTypeRepository;
    private readonly IPropertyDefinitionRepository _propertyDefinitionRepository;
    private readonly IPropertyDefinitionTypeRepository _propertyDefinitionTypeRepository;
    // Resolve each content type, find the PropertyDefinition by name, repoint its
    // PropertyDefinitionType, and save. See Migration36 for the full implementation.
}
```

Remember to update **every** content type that carries the property, not just the one you noticed.

## E. Test shape

`Wisbar.Tests/Migrations/Migration44FixWidgetTitlesTests.cs`. Extract the transformation so it is callable without the migration shell.

```csharp
[TestClass]
public class Migration44FixWidgetTitlesTests
{
    [TestMethod]
    public void Transform_TrimsStrayWhitespace() { }

    [TestMethod]
    public void Transform_AlreadyClean_LeavesValueUnchanged() { }   // re-run safety

    [TestMethod]
    public void Transform_MalformedRow_IsSkippedNotThrown() { }
}
```

## Quick reference

| Concern | Rule |
|---|---|
| `Order` | Global, permanent, ten apart. Highest today is 1410 |
| Class name | `Migration<N><WhatItDoes>`, `internal` |
| Registration | `[ServiceConfiguration(typeof(IMigrationStep))]` |
| Return `true` | Success, including "nothing to do" and "some rows failed" |
| Return `false` | Whole-step failure only - blocks every later step |
| Per-item errors | Catch, count, log, continue |
| Re-runnability | Required. Check end state, or use a validating query |
| Progress | `AddProgressMessageText(text, isError, percent)`, scaled |
| Catalog walks | `ServiceLocator.SetScopedServiceProvider(_serviceProvider)` first |
| SQL | Embedded resource + `--beginvalidatingquery` guard + both csproj entries |
