---
name: optimizely-catalog-content-type
description: Add or change a catalog content type in an Optimizely Commerce Connect (CMS 12) site - products, variants/SKUs, categories, bundles, packages - covering the CatalogContentType class, its base and interfaces, the variant allow-list, view model builder, controller, view, and the import/search/migration wiring a new type needs to actually appear. Use whenever asked to create a product type, variant type, SKU type, catalog node or category type, add a property to a product or variant, or when a catalog type exists but its page, search indexing, or catalog sync is missing.
---

# Adding a catalog content type

Catalog types are not CMS pages. They carry `[CatalogContentType]`, they are backed by Commerce meta-classes in a separate database, and in a catalog-sync site **editors do not create them - the importer does**. A product type that no importer maps to will never exist in the catalog, however correct the C# is.

Read `.claude/rules/commerce.md` before starting. Pricing, inventory, cart and promotion access all go through the documented abstractions.

## Step 1: Learn this repo's conventions first

```bash
# Catalog types, their bases and interfaces
find . -type d -path "*Commerce*" \( -name Products -o -name Variants -o -name Categories \) -not -path "*/obj/*"
find . -name "ProductBase.cs" -o -name "VariantBase.cs" -o -name "CategoryBase.cs"
# The wiring a new type has to reach
grep -rn "DefinitionMap" --include=*.cs .
find . -name "FindIndexingConventionsModule.cs" -o -name "ProductsImportService.cs"
```

Read one existing product **and** its variant end to end before writing. `references/templates.md` has the full code for this family of projects; the repo is the authority.

Reference layout (Wisbar):

| Part | Location |
|---|---|
| Product types | `Wisbar.Optimizely.ContentTypes/Commerce/Products/` |
| Variant types | `.../Commerce/Variants/` |
| Categories, bundles, packages | `.../Commerce/Categories/`, `/Bundles/`, `/Packages/` |
| Custom property types | `.../Commerce/Properties/` + `/EditorDescriptors/` |
| View model + builder | `Wisbar.Web/Models/Commerce/` and `/Builders/` |
| Controller | `Wisbar.Web/Controllers/<Type>Controller.cs` |
| View | `Wisbar.Web/Views/<Type>/Index.cshtml` |
| Catalog sync | `Wisbar.Business/Synchronization/Services/ProductsImportService.cs` |
| Search conventions | `Wisbar.Business/Features/Search/FindIndexingConventionsModule.cs` |
| Migrations | `Wisbar.Business/Migrations/` |

## Step 2: Pick the right shape

| Shape | Base | CLR base |
|---|---|---|
| Product (the sellable thing's page) | `ProductBase` | `ProductContent` |
| Variant / SKU (the thing that has a price) | `VariantBase` | `VariationContent` |
| Add-on variant (tuition, extras) | `AddOnVariantBase` | `VariationContent` |
| Category / catalog node | `CategoryBase` | `NodeContent` |
| Bundle / package | `BundleBase` / `PackageBase` | `BundleContent` / `PackageContent` |

Products carry marketing and display; **variants carry price, inventory, and purchasability**. If the new field differs per format, language, or date, it belongs on the variant, not the product.

Before writing a new type, check whether subclassing an existing one is enough - `EventProduct : MeetingProduct` adds no properties at all, only a different variant allow-list and thumbnail.

Shared fields go on the base class, not copy-pasted. Behavior that several types share goes behind one of the existing marker interfaces (`IDetails`, `ITabs`, `IAccordions`, `ISponsors`, `ICredits`, `ISubscription`, `ILanguage`, `IRegistration`, `IEventDetails`, `IAutoSupplementation`); check those folders before inventing a property.

## Step 3: Write the content type

- **`[CatalogContentType]`, not `[ContentType]`.** Catalog types use the Commerce attribute, with `GroupName` from `Globals.GroupNames` (`Products`, `GenericCategory`).
- Fresh, stable GUID. Never copy or change one.
- **Every property `virtual`.**
- `[AvailableContentTypes(Include = [typeof(TheVariant), ...])]` on a product declares which variants may live under it. Omit it and editors can nest anything.
- `[SiteImageUrl("book-product.png")]` for the catalog UI thumbnail.
- `[Searchable]` on text that should reach the Find index. Nothing is indexed by intent alone.
- Custom editors use `[BackingType(...)]` + `[EditorDescriptor(...)]` + `[UIHint(Globals.SiteUIHints...)]` together - all three, or the property renders as a raw string list.
- Variants hide SEO with `[ScaffoldColumn(false)] public override SeoInformation SeoInformation { get; set; }` - variants are not routable pages.
- `DisplayName` and `Description` are editor-facing: **hyphens, not em dashes.**

## Step 4: The two migration traps

Catalog types are code-first and sync on startup, which is true right up until it isn't:

1. **Changing `[BackingType]` on an existing List/Json property does nothing on a database that already has rows.** Optimizely's content-type sync silently refuses the change; the attribute only takes effect on a brand-new database. Fixing it on Integration/Preproduction/Production needs an `IMigrationStep` that forces the `PropertyDefinition` over - see `Migration36ChangeSecondaryAccordionBackingType`.
2. **Order-system properties are not catalog properties.** Anything written to a line item, order, or address `Properties` bag survives `SaveAsPurchaseOrder` only if a metafield exists on that meta-class. Register it in a migration - see `Migration13LineItemEventMetaFields`.

Adding a plain new property to a product or variant needs no migration. Changing an existing property's backing type or column size does. When in doubt, say which case you think it is rather than assuming the clean-database behavior.

## Step 5: Wire it into catalog sync - or it will never exist

In a site whose catalog comes from an external system, `ProductsImportService.DefinitionMap` maps each external product family to a `(ProductType, VariantType)` pair. **A new product or variant type is unreachable until it is added there**, along with:

- a family constant in `WisbarConstants.ProductFamily`,
- a `case NewVariant v: MapNewVariantFields(v, dto); break;` in the variant field-mapping switch, and the mapping method itself.

Note the reverse-lookup constraint in that file: variant types must be unique per family (they resolve a definition 1:1), while one product type may back several families. If the new type breaks that uniqueness, the reverse lookups degrade - read the comments there before changing the map.

If the type is genuinely editor-created rather than synced, say so explicitly; it is the exception in this codebase.

## Step 6: Search indexing

New product types inherit the `ForInstancesOf<ProductBase>()` conventions in `FindIndexingConventionsModule`. Add a `ForInstancesOf<NewProduct>()` block only to exclude fields that do not apply. Computed index fields are extension methods in `ProductSearchIndexExtensions`; nested fields must be registered in both the flat and nested convention blocks. Product search goes through `IProductSearchService`, not the general `ISearchService`.

## Step 7: Rendering - only for products

Variants and add-ons are not routable; they need no controller or view. A product type that gets its own page needs four pieces, matching the page-type pattern:

1. **View model** deriving from `ProductViewModel<T>`, which extends `PageViewModel<T>` and so gets `Layout` and `IsMember` stamped by `PageContextActionFilter`. Derived product view models use the record copy constructor - `public MyProductViewModel(ProductViewModel<MyProduct> baseModel) : base(baseModel)`.
2. **Builder** `IMyProductViewModelBuilder : IProductViewModelBuilder` plus its implementation, registered in `Startup.ConfigureServices` as scoped. Pricing and availability are member-aware, so builders take `memberId` and a `CancellationToken`.
3. **Controller** `MyProductController : CommerceControllerBase<MyProduct>`, action decorated `[CommerceTracking(TrackingType.Product)]`. The base already returns 404 for a product with no published variant, skipping the check in edit mode - do not re-implement that.
4. **View** at `Views/MyProduct/Index.cshtml`.

A product type reusing an existing product's page needs none of these - `EventProduct` rides on the Meeting controller and view.

## Step 8: Verify

```bash
dotnet build src/Wisbar.sln
```

The catalog UI is where this is really proven. Report which of these you actually checked:

- Type appears in the catalog UI with its thumbnail; only the allowed variants can be created under a product.
- New properties appear on the right tab, in order, with the intended editor.
- The importer creates and updates content of the new type; a re-run does not duplicate.
- The product page renders with full chrome and a working purchase panel for both anonymous and member.
- Search returns the new type, with the intended fields indexed.
- If a backing type or field size changed, the migration ran and existing rows converted.

## Checklist

- [ ] `[CatalogContentType]` with a fresh GUID and `Globals.GroupNames` group
- [ ] Correct base (`ProductBase` / `VariantBase` / `AddOnVariantBase` / `CategoryBase`)
- [ ] Fields on the variant if they vary per SKU, on the product if they do not
- [ ] `[AvailableContentTypes]` allow-list on the product
- [ ] All properties `virtual`; `[Searchable]` where indexing is wanted
- [ ] `[BackingType]` + `[EditorDescriptor]` + `[UIHint]` together for custom editors
- [ ] Migration written if a backing type or field size changed, or an order metafield is needed
- [ ] `DefinitionMap`, family constant, and variant field mapping updated in the importer
- [ ] Find conventions reviewed
- [ ] Product page: view model, builder (+ DI registration), controller, view
- [ ] Thumbnail PNG added
- [ ] Solution builds

Full code templates: `references/templates.md`.
