# Commerce metadata templates

Namespaces below use the Wisbar layout. Substitute the host project's namespaces - the repo you are working in is the authority, these are the shape.

Sections A-C add metadata; D-G modify or remove it.

---

# Adding

## A. Order-side metafields (MetaDataPlus)

For LineItem, PurchaseOrder, OrderForm, Shipment, Payment, OrderAddress.

### 1. Declare the keys

`Globals.LineItemProperties` - prefixed, because the LineItem meta-class is shared and generic names collide with native fields.

```csharp
public static class LineItemProperties
{
    public const string ParentEventCode = "ParentEventCode";
    public const string RegistrationAnswers = "RegistrationAnswers";

    // Values are prefixed because these persist as metafields on the shared LineItem meta-class;
    // generic names like "Title" risk colliding with native line-item fields.
    public static class ClassifiedAd
    {
        public const string Title = "ClassifiedAdTitle";
        public const string Description = "ClassifiedAdDescription";
    }
}
```

### 2. Register in a migration step

```csharp
using EPiServer.Commerce.Internal.Migration.Steps;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Shared;
using Mediachase.MetaDataPlus.Configurator;
using Microsoft.Extensions.Logging;
using Wisbar.Optimizely.Core.Helpers;
using static Wisbar.Optimizely.Core.Constants.Globals;

namespace Wisbar.Business.Migrations;

// WHY: the cart is serializable, so these keys round-trip in its blob - but SaveAsPurchaseOrder
// writes a LEGACY (metadata-backed) purchase order, where a line-item property only persists if a
// matching metafield exists on the LineItem meta-class. Without these the keys are silently dropped.
[ServiceConfiguration(typeof(IMigrationStep))]
internal class Migration47WidgetMetaFields : IMigrationStep
{
    public int Order => 1450;
    public string Name => "Widget Meta Fields";
    public string Description =>
        "Register the widget metafields on the LineItem meta-class so widget selections survive " +
        "SaveAsPurchaseOrder, plus the WidgetSent flag on the PurchaseOrder meta-class.";

    private readonly ILogger<Migration47WidgetMetaFields> _log;

    public Migration47WidgetMetaFields(ILogger<Migration47WidgetMetaFields> log) => _log = log;

    public bool Execute(IProgressMessenger progressMessenger)
    {
        try
        {
            progressMessenger.AddProgressMessageText("Adding widget meta fields", false, 10);

            var context = OrderContext.MetaDataContext;

            var lineItemMetaClass = OrderContext.Current.LineItemMetaClass
                ?? throw new InvalidOperationException("LineItem meta-class is unavailable; order system not configured.");

            OrderMetaFieldHelper.EnsureField(context, lineItemMetaClass,
                LineItemProperties.WidgetCode, "Widget Code", MetaDataType.ShortString, 100);

            // JSON map - nvarchar(max) via LongString, so length is 0.
            OrderMetaFieldHelper.EnsureField(context, lineItemMetaClass,
                LineItemProperties.WidgetOptions, "Widget Options", MetaDataType.LongString, 0);

            var purchaseOrderMetaClass = OrderContext.Current.PurchaseOrderMetaClass
                ?? throw new InvalidOperationException("PurchaseOrder meta-class is unavailable; order system not configured.");

            // Order-level idempotency flag for the post-placement sync job.
            OrderMetaFieldHelper.EnsureField(context, purchaseOrderMetaClass,
                OrderProperties.WidgetSent, "Widget Sent", MetaDataType.Boolean, 0);

            progressMessenger.AddProgressMessageText("Finished adding widget meta fields", false, 100);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "An error occurred while adding the widget meta fields.");
            progressMessenger.AddProgressMessageText("Error adding widget meta fields", true, 100);
            return false;
        }
    }
}
```

### 3. Read and write behind extension properties

```csharp
public static class LineItemExtensions
{
    public static string? GetWidgetCode(this ILineItem lineItem) =>
        lineItem.Properties[LineItemProperties.WidgetCode] as string;

    public static void SetWidgetCode(this ILineItem lineItem, string? code) =>
        lineItem.Properties[LineItemProperties.WidgetCode] = Truncate(code, 100);

    public static bool IsWidgetEnabled(this ILineItem lineItem) =>
        lineItem.Properties[LineItemProperties.WidgetEnabled] is bool b && b;
}
```

### `MetaDataType` reference

| Use | Type | Length |
|---|---|---|
| Codes, short text | `ShortString` | Real max (50-450) |
| JSON, long free text | `LongString` | `0` (nvarchar(max)) |
| Numbers / dates / flags | `Integer` / `DateTime` / `Boolean` | `0` (ignored) |

Raw equivalent, if a helper is unavailable (from `epi-commerce-recipes.md` §3.2.2) - `MetaField.Create` and `MetaClass.AddField` persist immediately, no `AcceptChanges`:

```csharp
var metafield = MetaField.Load(MetaDataContext.Instance, name)
    ?? MetaField.Create(MetaDataContext.Instance, "Mediachase.Commerce.Orders",
         name, friendlyName, "", MetaDataType.Boolean, 0, true, false, false, false);

var mc = MetaClass.Load(OrderContext.MetaDataContext, "ShoppingCart");
if (!mc.MetaFields.Contains(metafield)) mc.AddField(metafield);
```

Prefer `OrderMetaFieldHelper.EnsureField`, which wraps exactly this with the existence checks.

## B. Customer-side metafields (BusinessFoundation)

```csharp
using Mediachase.BusinessFoundation.Data.Meta.Management;
using Mediachase.Commerce.Customers;
using Wisbar.Optimizely.Core.Helpers;
using static Wisbar.Optimizely.Core.Constants.WisbarConstants;

public bool Execute(IProgressMessenger progressMessenger)
{
    try
    {
        // Meta enum items are a separate API from metafields.
        CommerceHelpers.AddMetaEnumItems("ContactGroup", [ContactGroup.Member, ContactGroup.NonMember]);
        progressMessenger.AddProgressMessageText("Created additional customer groups", false, 10);

        var contactMetaClass = CommerceHelpers.GetOrCreateMetaClass(ContactEntity.ClassName);

        var metaFields = new List<MetaField>
        {
            // ReadOnly: synced from iMIS, so a Commerce Manager edit would be overwritten next sync.
            new(ContactDetail.MemberId.Key, ContactDetail.MemberId.Label)
                { IsNullable = true, TypeName = MetaFieldType.Text, ReadOnly = true },

            new(ContactDetail.UpassEndDate.Key, ContactDetail.UpassEndDate.Label)
                { IsNullable = true, TypeName = MetaFieldType.DateTime, ReadOnly = true },

            // Unbounded text needs the LongText attribute, not just the type.
            new(ContactDetail.PurchaseClaims.Key, ContactDetail.PurchaseClaims.Label)
                {
                    IsNullable = true, TypeName = MetaFieldType.LongText, ReadOnly = true,
                    Attributes = new AttributeCollection { { "LongText", true } }
                },

            // Bounded text carries its cap as an attribute.
            new(ContactDetail.Title.Key, ContactDetail.Title.Label)
                {
                    IsNullable = true, TypeName = MetaFieldType.Text, ReadOnly = true,
                    Attributes = new AttributeCollection { { "MaxLength", 100 } }
                }
        };

        CommerceHelpers.CreateMetaFields(contactMetaClass, metaFields);
        progressMessenger.AddProgressMessageText("Created additional contact fields", false, 50);

        var organizationMetaClass = CommerceHelpers.GetOrCreateMetaClass(OrganizationEntity.ClassName);
        CommerceHelpers.CreateMetaFields(organizationMetaClass, [
            new(OrganizationDetail.CorporateId.Key, OrganizationDetail.CorporateId.Label)
                { IsNullable = true, TypeName = MetaFieldType.Text, ReadOnly = true }
        ]);

        progressMessenger.AddProgressMessageText("Finished", false, 100);
        return true;
    }
    catch (Exception ex)
    {
        _log.LogError(ex, "Commerce meta data initialization failed.");
        return false;
    }
}
```

`MetaFieldBuilder` is the alternative API, with per-type helpers (`epi-commerce-recipes.md` §7.1):

```csharp
using var builder = new MetaFieldBuilder(mc);
builder.CreateCheckBoxBoolean("NewsletterSubscribed", "Newsletter Subscribed", true, false, "NewsletterSubscribed");
builder.CreateDateTime("NewsletterSubscribedTime", "Newsletter Subscribed Time", true, false);
builder.CreateText("PersonNumber", "Person Number", true, 50, false);
builder.SaveChanges();
```

`CommerceHelpers.CreateMetaFields` already wraps a `MetaFieldBuilder` and skips existing fields, so use it unless you need a builder-only field type.

### Keys and labels

```csharp
public static class ContactDetail
{
    // Const mirrors of the two keys that also travel as sign-in claims (see Authentication).
    // A tuple field can't seed a const, so the const is the source and the tuple reuses it.
    public const string MemberIdKey = "ImisMemberId";
    public const string PurchaseClaimsKey = "ImisPurchaseClaims";

    public static (string Key, string Label) MemberId = (MemberIdKey, "iMIS Member ID");
}
```

`Authentication.MemberIdClaim` is defined as `ContactDetail.MemberIdKey` - changing the key changes the claim.

### `MetaFieldType` reference

| Use | Type | Attributes |
|---|---|---|
| Short text | `Text` | `{ "MaxLength", n }` |
| Unbounded text | `LongText` | `{ "LongText", true }` |
| Dates / numbers / flags | `DateTime` / `Integer` / `Boolean` | - |

## C. New BusinessFoundation meta-class

Only for a genuinely new entity. Existing entities go through `GetOrCreateMetaClass`.

```csharp
MetaClassManager manager = DataContext.Current.MetaModel;

using (var scope = manager.BeginEdit(MetaClassManagerEditScope.SystemOwner, AccessLevel.System))
{
    var mc = manager.CreateMetaClass(
        "CreditCard", "Credit Card", "CreditCards", "cls_CreditCard", PrimaryKeyIdValueType.Guid);

    using (var builder = new MetaFieldBuilder(mc))
    {
        builder.CreateText("CreditCardNumber", "Credit Card Number", false, 512, false);
        builder.SaveChanges();
    }

    scope.SaveChanges();
}
```

Both the builder and the scope need `SaveChanges()`. `BusinessManager.List` caps at 1,000 objects by default; add a non-clustered index for columns queried often.

---

# Modifying

**There is no in-place metafield alter API.** `EnsureField` returns early when the field exists, so changing arguments in a shipped step does nothing - and editing a shipped step is wrong regardless, because environments that already ran it never will again. Every change below is a **new** step.

## D. Find the physical storage first

Never guess a table name. Query the database you are targeting:

```sql
-- Where does this metafield actually live?
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE COLUMN_NAME = 'ImisTitle';
```

BusinessFoundation entities are `cls_<Entity>` (`cls_Contact`, `cls_Organization`), one column per metafield named for the field key. Catalog properties live in `CatalogContentProperty`, joined through `MetaField`:

```sql
-- Catalog metafield id, for the CatalogContentProperty joins (epi-commerce-recipes.md §1.7)
SELECT MetaFieldId FROM MetaField
WHERE Name = '_ExcludedCatalogEntryMarkets' AND Namespace = 'Mediachase.Commerce.Catalog';
```

## E. Widening a column

`ALTER TABLE` in an embedded SQL script, guarded so a second run is a no-op. Register the `.sql` in the csproj **twice** (`<None Remove>` and `<EmbeddedResource Include>`).

Pattern from `Migration8UpdateFieldSize` - guard on the *current* state, so the script self-skips once applied:

```sql
--beginvalidatingquery
IF (EXISTS (SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'cls_Contact'
  AND COLUMN_NAME = 'ImisTitle'
  AND CHARACTER_MAXIMUM_LENGTH = 50))
   SELECT 1, 'Updating'
ELSE
   SELECT 0, 'Already Done'
--endvalidatingquery

BEGIN

    BEGIN TRANSACTION

    ALTER TABLE cls_Contact
    ALTER COLUMN ImisTitle NVARCHAR(100) NULL

    COMMIT

END
```

Keep `NULL` in the altered definition - metafields are created nullable, and omitting it would make the column `NOT NULL`.

### When an index blocks the alter

An INCLUDE column cannot be altered while the index exists. Drop, alter, recreate - all in one transaction, as `Migration10SeoFieldSize` does:

```sql
    -- Keywords and Description are INCLUDE columns on IX_CatalogItemSeo_CatalogNodeId,
    -- which blocks ALTER COLUMN. Drop the index, resize the columns, then recreate it.
    IF EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_CatalogItemSeo_CatalogNodeId'
                 AND object_id = OBJECT_ID('dbo.CatalogItemSeo'))
        DROP INDEX IX_CatalogItemSeo_CatalogNodeId ON dbo.CatalogItemSeo

    ALTER TABLE CatalogItemSeo ALTER COLUMN Keywords NVARCHAR(1000) NULL
    ALTER TABLE CatalogItemSeo ALTER COLUMN Description NVARCHAR(1000) NULL

    CREATE NONCLUSTERED INDEX IX_CatalogItemSeo_CatalogNodeId
    ON dbo.CatalogItemSeo (CatalogNodeId)
    INCLUDE (LanguageCode, Uri, Title, Description, Keywords, UriSegment)
```

Recreate the index with its **original** definition - copy it out of `sys.indexes` before dropping, do not reconstruct it from memory.

## F. Retyping or renaming - add, backfill, retire

Neither can be done in place safely. Narrowing truncates or fails; a rename breaks every reader, and the key is the physical column name.

Three steps, in this order, each its own migration:

1. **Add** the new field alongside the old one.
2. **Backfill** - copy and convert existing values, per-item try/catch, counting failures.
3. **Retire** the old field, and only once the backfill reported zero failures.

Steps 2 and 3 can share a step if the backfill is cheap, but retire only on a clean run:

```csharp
if (failed == 0)
    RetireOldField();
else
    _log.LogWarning("{Failed} row(s) failed, so the old field was left in place.", failed);
```

## G. Removing a field or enum item

**Migrate the dependent data first, in an earlier-numbered step.** The house example:

- `Migration15CustomerGroupFix` (Order 1140) reassigns every contact to `ContactGroup.Customer`.
- `Migration16RemoveContactGroups` (Order 1150) then removes the Member/Non-Member enum items.

```csharp
// Runs after Migration15CustomerGroupFix (Order 1140), which reassigns every contact to
// ContactGroup.Customer, so no contact still references Member / Non-Member when these
// legacy ContactGroup meta enum items are removed.
public int Order => 1150;

public bool Execute(IProgressMessenger progressMessenger)
{
    CommerceHelpers.RemoveMetaEnumItems("ContactGroup", [ContactGroup.Member, ContactGroup.NonMember]);
    ...
}
```

`RemoveMetaEnumItems` removes only items still present and keys on `Handle`, not name, so a re-run finds nothing to do.

Dropping a metafield destroys its data irreversibly. Leave an unused field in place unless it is genuinely in the way, and confirm before dropping one rather than deciding alone.

## H. Meta enum and dictionary items

Separate APIs from metafields.

```csharp
CommerceHelpers.AddMetaEnumItems("ContactGroup", [ContactGroup.Member, ContactGroup.NonMember]);
CommerceHelpers.RemoveMetaEnumItems("ContactGroup", ["Obsolete"]);
var items = CommerceHelpers.GetMetaEnumItems("ContactGroup");

CommerceHelpers.AddMetaDictionaryItems("SomeDictionaryField", ["A", "B"]);
```

Dictionary storage, for SQL-level work (`epi-commerce-recipes.md` §1.7):

- Single-value: `CatalogContentProperty.Number` = `MetaDictionaryId`.
- Multi-value: `Number` = `MetaKey`, joined `MetaMultiValueDictionary` -> `MetaDictionary`.
- String dictionary: via `MetaStringDictionaryValue`.

---

## The failure this area produces

Symptom: a value is written to `Properties`, works in the cart, and is gone on the confirmation page or in an export.

Cause: no metafield on that meta-class, so `SaveAsPurchaseOrder` dropped it. Silent - no exception, nothing logged.

Fix: register the metafield, then **re-place an order** to confirm. A cart-level test cannot detect this class of bug.

Related: value present but truncated, or placement throws on save - the declared length is too small. Truncate at the write site, and widen the column with a new step (section E) if the data genuinely needs the room.

## Quick reference

| Concern | Order side | Customer side |
|---|---|---|
| Namespace | `Mediachase.MetaDataPlus.Configurator` | `Mediachase.BusinessFoundation.Data.Meta.Management` |
| Context | `OrderContext.MetaDataContext` | `DataContext.Current.MetaModel` |
| Meta-class | `OrderContext.Current.LineItemMetaClass` etc. | `CommerceHelpers.GetOrCreateMetaClass(ContactEntity.ClassName)` |
| Add field | `OrderMetaFieldHelper.EnsureField(...)` | `CommerceHelpers.CreateMetaFields(mc, fields)` |
| Type enum | `MetaDataType` | `MetaFieldType` |
| Length | `EnsureField` argument | `AttributeCollection` |
| Physical table | Look up via `INFORMATION_SCHEMA` | `cls_<Entity>` |
| Alter | New step, embedded SQL, validating query | Same |
| Read at runtime | `entity.Properties[key]` (object) | Contact/Organization accessors |
