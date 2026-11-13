---
name: commerce-metadata
description: Create and modify Commerce metadata in Optimizely Commerce Connect (CMS 12) - MetaDataPlus meta-classes and metafields on LineItem, PurchaseOrder, OrderForm, Shipment, Payment and OrderAddress, and BusinessFoundation entities and fields on Contact, Organization and Address. Covers adding fields, widening or retyping existing ones, renaming, removing fields and meta enum/dictionary items, creating a new BF meta-class, why a Properties-bag key vanishes at order placement, key prefixing, and doing all of it idempotently from a migration step. Use whenever asked to add or change a custom field on an order, line item, payment, shipment, address, contact or organization, to alter a metafield's size or type, to remove metadata, or when a value written to Properties disappears after checkout.
---

# Creating and modifying Commerce metadata

Commerce stores custom fields as **metafields on meta-classes**. Three metamodels answer to that description, they are not interchangeable, and picking the wrong one is the first mistake to avoid.

Registration and modification both happen in a migration step - see `optimizely-commerce-migration-step` for the step's own shape. This skill is what goes inside it.

Background reading, in order of usefulness here: `.claude/knowledge-base/pro-episerver-commerce.md` (the MetaData and Business Foundation chapters), `.claude/knowledge-base/epi-commerce-recipes.md` (§3.2 order metafields, §7.1 contact fields, §1.7 metafield storage in SQL).

## Step 1: Pick the metamodel

| Field on | Metamodel | Namespace | Reached via |
|---|---|---|---|
| LineItem, PurchaseOrder, OrderForm, Shipment, Payment, OrderAddress | **MetaDataPlus** | `Mediachase.MetaDataPlus.Configurator` | `OrderContext.MetaDataContext` |
| Contact, Organization, Address (customer side) | **BusinessFoundation** | `Mediachase.BusinessFoundation.Data.Meta.Management` | `DataContext.Current.MetaModel` |
| Products, variants, categories | **Catalog - code-first, do not touch by hand** | - | `[CatalogContentType]` classes |

The first two use incompatible `MetaClass`/`MetaField` types: an order metafield cannot go through `CommerceHelpers`, and a contact field cannot go through `OrderMetaFieldHelper`. A type mismatch that will not compile usually means you are on the wrong side.

**Catalog metadata is generated from your C# content types** and auto-synced on startup. Never hand-edit a catalog meta-class - change the content type instead, and see `optimizely-catalog-content-type`. The one exception is forcing a `[BackingType]` change onto a database that already has rows, which is a migration concern.

## Step 2: Adding a field - why they exist at all

The trap this skill exists for:

> A cart is **serializable**, so any key written to `lineItem.Properties[...]` round-trips in the cart's blob and works in every test that stops at the cart. But `SaveAsPurchaseOrder` writes a **legacy, metadata-backed** purchase order, where a property persists **only if a matching metafield exists on that meta-class**. Without one the value is silently dropped at placement - no error, no log.

So the question is never "do I need a metafield to use `Properties`" - it is **"must this value survive order placement?"**

| Key | Metafield | Why |
|---|---|---|
| `ParentEventCode`, `RegistrationAnswers` | Yes (Migration13) | The confirmation page renders them from the placed order |
| `PricingSaleCode` | Yes (Migration39) | The order export reads it after placement |
| `ClassifiedAd.*` | Yes (Migration25) | The post-placement sync sends them |
| `AutoSuppEnabled`, `CountsForMiniCart`, `UpassPriced` | **No** | Cart-only; nothing reads them after placement |

Do not register a metafield for a cart-only flag - each is a real column on a shared table. Prefix keys on shared meta-classes (`ClassifiedAdTitle`, not `Title`) and declare them as constants in `Globals.LineItemProperties` / `OrderProperties`, never inline.

Add through the idempotent helpers, never raw API calls: `OrderMetaFieldHelper.EnsureField` (returns early if present, reuses an orphaned definition, creates nullable so the add is additive) and `CommerceHelpers.CreateMetaFields` (skips existing, saves only if something changed).

## Step 3: Modifying an existing field

**There is no API for altering a metafield in place.** `EnsureField` returns early when the field exists, so changing its arguments in an existing migration does nothing at all - and editing a shipped migration step is wrong anyway, since environments that already ran it will never run it again. Every change below is a **new** step.

### Widening or retyping a column

This is a physical schema change: an `ALTER TABLE` in an embedded SQL script, guarded by a validating query. `Migration8UpdateFieldSize` widened `cls_Contact.ImisTitle` from 50 to 100; `Migration10SeoFieldSize` took `CatalogItemSeo.Keywords`/`Description` to `nvarchar(MAX)`.

Find the physical table and column first - **do not guess the table name**:

```sql
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE COLUMN_NAME = 'YourMetaFieldKey';
```

BusinessFoundation entities live in `cls_<Entity>` tables (`cls_Contact`, `cls_Organization`), one column per metafield keyed by the field name. Order-side and catalog storage differ - the query above is the reliable way to locate them on the database you are actually targeting.

Two things that bite:

- **Widening is safe; narrowing and retyping are not.** Narrowing fails or truncates against existing data. Changing type generally cannot be done in place - add a new field, backfill, retire the old one over two steps.
- **Indexes block `ALTER COLUMN`.** If the column is an INCLUDE column on an index, drop the index, alter, and recreate it in the same script - `Migration10` does exactly this for `IX_CatalogItemSeo_CatalogNodeId`.

### Renaming

Treat a rename as add + backfill + retire, never an in-place rename. The key string is referenced by the migration, the writer, and the reader, and it is the physical column name. `ContactDetail.MemberIdKey` is additionally the sign-in claim name - renaming it breaks authentication.

### Removing a field or enum item

**Migrate the dependent data first, in an earlier step.** `Migration15CustomerGroupFix` (Order 1140) reassigns every contact to `ContactGroup.Customer`, and only then does `Migration16RemoveContactGroups` (Order 1150) remove the Member/Non-Member enum items. Removing metadata that rows still reference leaves them unresolvable.

`CommerceHelpers.RemoveMetaEnumItems` is idempotent - it removes only items still present, keyed on `Handle`, not name.

Dropping a metafield entirely destroys its data with no undo. Prefer leaving an unused field in place unless it is genuinely in the way, and say so rather than dropping it on your own judgement.

## Step 4: Creating a new BusinessFoundation meta-class

A whole new BF entity (not just a field on an existing one) needs an edit scope:

```csharp
using var scope = manager.BeginEdit(MetaClassManagerEditScope.SystemOwner, AccessLevel.System);
var mc = manager.CreateMetaClass("CreditCard", "Credit Card", "CreditCards", "cls_CreditCard", PrimaryKeyIdValueType.Guid);
using (var builder = new MetaFieldBuilder(mc)) { builder.CreateText(...); builder.SaveChanges(); }
scope.SaveChanges();
```

In this repo `CommerceHelpers.GetOrCreateMetaClass` wraps the get-or-create for existing entities; reach for the raw scope only when adding a genuinely new entity. `BusinessManager.List` returns at most 1,000 objects by default, and custom columns queried often want a non-clustered index.

## Step 5: Data types

Order side (`MetaDataType`): `ShortString` with a real max (50-450), `LongString` with length `0` for nvarchar(max), `Integer` / `DateTime` / `Boolean` with length `0` (ignored).

Customer side (`MetaFieldType`): `Text` with `AttributeCollection { { "MaxLength", n } }`, `LongText` with `{ "LongText", true }`, plus `DateTime`, `Integer`, `Boolean`. Set `IsNullable = true` so the add is non-breaking, and `ReadOnly = true` for anything an external system owns - every iMIS-sourced contact field here does, so a Commerce Manager edit cannot be silently overwritten by the next sync.

`Properties` values are `object`: read defensively (`is bool b`, `is true`, `as string`) behind extension properties, as `LineItemExtensions` does. Field length is not a validation boundary - truncate at the write site.

## Step 6: Verify

```bash
dotnet build src/Wisbar.sln
```

Report which of these you actually checked:

- The step runs clean on a fresh database, and a second run is a no-op.
- **Place a real order end to end and re-read the value from the placed purchase order.** A passing cart test is exactly the false positive this area produces.
- For an alter: the validating query returns 0 on a second run, and existing rows survived with their data intact.
- For a removal: nothing still references the removed field or enum item.
- Customer-side fields appear in Commerce Manager, read-only where externally sourced.

State plainly that you have not run it against Integration, Preproduction, or Production - those are gated deployments.

## Checklist

- [ ] Correct metamodel (order = MetaDataPlus, customer = BusinessFoundation, catalog = code-first)
- [ ] Adding: the value genuinely must survive order placement
- [ ] Key prefixed, declared as a constant, not inlined
- [ ] Added via `EnsureField` / `CreateMetaFields` so it is idempotent
- [ ] Modifying: a **new** migration step, never an edit to a shipped one
- [ ] Alters use an embedded SQL script with a validating query; table name looked up, not guessed
- [ ] Index dropped and recreated if it blocks `ALTER COLUMN`
- [ ] Retype/rename done as add + backfill + retire, not in place
- [ ] Removals migrate dependent data in an earlier step first
- [ ] Externally synced fields `ReadOnly = true`; new fields `IsNullable = true`
- [ ] Reads defensive, behind extension properties; long values truncated at the write site
- [ ] Round-trip verified through an actual placed order

Templates: `references/templates.md`.

**Not this skill:** the `PropertyList<T>` classes suffixed `...PropertyMetaData` with `[PropertyDefinitionTypePlugIn]`, used as `[BackingType]` on products and variants. Those are catalog content-type properties - see `optimizely-catalog-content-type`.
