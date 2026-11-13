# Pro Optimizely Commerce Cloud — Knowledge Base

> **Book:** *Pro Optimizely Commerce Cloud* by **Quan Mai** (Leanpub, 2023-09-01; also published as *Pro Episerver Commerce* in prior editions).
> **Target versions:** Optimizely Commerce Cloud **13 and 14**, Optimizely Content Cloud (CMS) **12**, .NET 5/6/8.
> **Why it matters here:** Our project is on **CMS 12 + Commerce 14 + .NET 8**. The book is written by a long-time member of the Commerce engineering/support team; it reveals internal architecture, performance traps, extension points, and the "why" behind APIs that official docs do not cover. It is the single highest-priority reference for this codebase.

> Namespaces are still `EPiServer.*` / `Mediachase.*` despite the Optimizely rebrand. The book uses "Commerce", "Episerver Commerce", and "Optimizely Commerce" interchangeably. So does this document.

---

## Table of Contents

- [Chapter 0: A Brief History](#chapter-0-a-brief-history)
- [Chapter 0.5: Framework Overview (DI, Initialization, NuGet)](#chapter-05-framework-overview)
- [**Part 1: Catalog System**](#part-1-catalog-system)
  - [Chapter 1: How the Catalog is Structured](#chapter-1-how-the-catalog-is-structured)
  - [Chapter 2: Catalog Content Modeling](#chapter-2-catalog-content-modeling)
  - [Chapter 3: Associations, Relations, Assets](#chapter-3-associations-relations-assets)
- [**Part 1.1: Important Concepts**](#part-11-important-concepts)
  - [Chapter 3.1: Markets](#chapter-31-markets)
- [**Part 2: Inventories and Prices**](#part-2-inventories-and-prices)
  - [Chapter 4: Prices](#chapter-4-prices)
  - [Chapter 5: Inventories](#chapter-5-inventories)
- [**Part 3: The Order System**](#part-3-the-order-system)
  - [Chapter 6: The Order System](#chapter-6-the-order-system)
  - [Chapter 6.5: The Old Order System](#chapter-65-the-old-order-system)
- [**Part 4: Events — Listen and React**](#part-4-events)
  - [Chapter 8: Content Events](#chapter-8-content-events)
  - [Chapter 9: Inventory, Price, Order Events](#chapter-9-inventory-price-order-events)
- [**Part 5: Search**](#part-5-search)
  - [Chapter 11a: Search — SearchProvider, Search API, Find Commerce](#chapter-11a-search)
- [**Part 6: Promotions**](#part-6-promotions)
  - [Chapter 11b: The New Promotion System](#chapter-11b-the-new-promotion-system)
- [**Part 7: Business Foundation**](#part-7-business-foundation)
- [**Part 8: Extending the System**](#part-8-extending-the-system)
  - [Plug Your Implementations — The Framework Way](#plug-your-implementations)
  - [The Unusual Ways — Custom Order Processing & Workflows](#the-unusual-ways)
  - [ProfileModule](#profilemodule)
  - [Migration Steps](#migration-steps)
- [**Part 9: Advanced Stuffs**](#part-9-advanced-stuffs)
  - [Caching](#caching)
  - [Routing](#routing)
  - [Settings](#settings)
- [Appendix: How to Ensure a Successful Upgrade](#appendix)

---

## Chapter 0: A Brief History

### Concepts

- 2010: Episerver Commerce R1 was a white-label repackage of **Mediachase eCommerce Framework**. Integration into CMS was via `CatalogPageProvider`.
- Episerver 7.5 (2013): `CatalogContentProvider` replaces `CatalogPageProvider`; catalog content becomes first-class `IContent`. New Catalog UI introduced. Markets concept finalized.
- Commerce 8 (2014): Cloud compatibility (Azure/AWS). Removed dependency on Common Framework. Removed Fulltext Search (SQL Azure couldn't do it).
- Commerce 9 (2015): **Rearchitected catalog storage** — versions moved out of DDS (Dynamic Data Store) into the eCF database; 2–10× perf gain; `CatalogContentProperty` table replaces per-metaclass tables; `WorkId` unique across the system.
- Commerce 10/11: `IsPrimary` on `NodeEntryRelation`; `IRelationRepository`; `IAssociationRepository`; batch APIs.
- Commerce 13.10: Batch `IContentRepository.Publish` extension for catalog content.
- **Commerce 14 / Content 12 (2021, .NET 5+)**: Commerce Manager is dead; CSR UI replaces editing orders/customers. Customized Commerce (B2C) vs Configured Commerce (B2B).

### Gotchas

- It's "EPiServer" (capital P, small i), not "Episerver". Kept in namespaces/package names post-rebrand.
- Commerce 14 removes Commerce Manager entirely — any tutorial referencing "Commerce Manager" before Commerce 14 needs translation (Marketing UI, Catalog UI, CSR UI for orders).

---

## Chapter 0.5: Framework Overview

### Concepts

- Commerce is built on **Optimizely Content Cloud**. Almost everything is pluggable via **dependency injection** + **initialization modules**.
- From CMS 11, DI is `Microsoft.Extensions.DependencyInjection`-compatible. `ServiceLocator` remains as a wrapper.
- **The later registration wins.** If you want to override a default implementation, use an `IConfigurableModule` that depends on the framework's init module so your registration runs after it.

### Key Types

| Type | Purpose |
|------|---------|
| `EPiServer.Framework.Initialization.InitializationModule` | HttpModule that scans & runs `IInitializationModule` instances in order |
| `IInitializationModule` | Hook for startup/shutdown work |
| `IConfigurableModule : IInitializationModule` | Adds `ConfigureContainer(ServiceConfigurationContext)` — run before DI is sealed |
| `ServiceConfigurationAttribute` | Attribute-based registration |
| `InitializableModuleAttribute`, `ModuleDependencyAttribute` | Module discovery and ordering |
| `Mediachase.Commerce.Initialization.CommerceInitialization` | Registers core services (`ICatalogSystem`, `IInventoryService`, `IMarketService`, …) |
| `EPiServer.Commerce.Initialization.InitializationModule` | Registers content + routing for Commerce — depends on `CommerceInitialization` |

### Code Patterns

Attribute-based registration:

```csharp
[ServiceConfiguration(typeof(IPriceService))]
public class MyCustomPriceService : IPriceService { /* ... */ }
```

Module-based registration (to control order):

```csharp
[InitializableModule]
[ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
public class MyConfig : IConfigurableModule
{
    public void ConfigureContainer(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<ICurrentMarket, MarketStorage>();
    }
    public void Initialize(InitializationEngine context) { }
    public void Uninitialize(InitializationEngine context) { }
}
```

### DI Lifecycle Cheatsheet

| Method | `ServiceInstanceScope` | Meaning |
|---|---|---|
| `AddTransient<T,I>` | `Transient` | New instance per resolve |
| `AddSingleton<T,I>` | `Singleton` | One per container |
| `AddScoped<T,I>` | `Hybrid` | Per-HttpContext if available, else per-thread |
| `AddHttpContextScoped<T,I>` | `HttpContext` | Per HTTP request |
| `AddAddHttpContextOrThreadScoped` | `Hybrid` | Alias for `AddScoped` |

### Key NuGet Packages

- `EPiServer.Commerce` — main front-end bundle
- `EPiServer.Commerce.Core` — catalog, promotion cores
- `EPiServer.Commerce.UI` — Catalog UI, Marketing UI, Migration view
- `EPiServer.Find.Commerce` — optional Find integration
- `EPiServer.ServiceApi.Commerce` — REST catalog API
- **Commerce 14+:** `EPiServer.CommerceManager` is gone

### Gotchas

- `CommerceInitialization` vs `EPiServer.Commerce.Initialization.InitializationModule`: depend on the latter when you need content + routing wiring complete; on the former when you only need core services.
- If two modules both depend on the same parent, their relative order is undefined.

---

## Part 1: Catalog System

## Chapter 1: How the Catalog is Structured

### Concepts

- Catalog = tree: `Catalog` → `CatalogNode` (a.k.a. Category) → `CatalogEntry` (Products / Variations / Packages / Bundles).
- A node can have a parent node (`CatalogNode.ParentNodeId`) and/or be linked into other nodes (`CatalogNodeRelation`).
- `NodeEntryRelation` links entries to nodes. An entry may belong to many nodes; from Commerce 11 one is flagged `IsPrimary = true`.

### 4 Entry Types

| Type | Prices? | Inventories? | Purpose |
|---|---|---|---|
| **Product** | No | No | Container for variations |
| **Variation / SKU** | Yes | Yes | The actual sellable unit |
| **Package** | Yes | Yes | Acts as one variation — fixed contents, customer buys the whole set |
| **Bundle** | No | No | Loose collection added to cart as individual items |

### Entry Relations

| Relation | Between |
|---|---|
| `ProductVariation` | Product ↔ Variation |
| `BundleEntry` | Bundle ↔ Entries |
| `PackageEntry` | Package ↔ Entries |

Associations (cross-sell / up-sell / accessories) are separate from relations and managed via `IAssociationRepository`. The framework stores them but doesn't use them for logic.

### Key Types

- `ICatalogSystem` / `CatalogContext.Current` — legacy DTO-based API (CatalogEntryDto etc.)
- `IContentRepository`, `IContentLoader` — preferred content-based API
- `ReferenceConverter` (`Mediachase.Commerce`) — bridges ID ↔ `ContentReference`
- `IRelationRepository`, `IAssociationRepository`

### ReferenceConverter — The Bridge

Content provider system needs every content identified by a unique `ContentReference`. But catalogs, nodes, and entries are auto-incrementing IDs in three tables. Solution: bitwise encode the type into the two most significant bits of the ID.

| MSB bits | Type |
|---|---|
| `00` | `CatalogEntry` |
| `01` | `CatalogNode` |
| `10` | `Catalog` |
| `11` | Catalog Root (virtual, always same reference — `GetRootLink()`) |

```csharp
public class ReferenceConverter
{
    public virtual ContentReference GetContentLink(int objectId, CatalogContentType contentType, int versionId);
    public virtual int GetObjectId(ContentReference contentLink);
    public virtual CatalogContentType GetContentType(ContentReference contentLink);
    public virtual ContentReference GetContentLink(string code, CatalogContentType type);
    public virtual string GetCode(ContentReference contentLink); // fast; added later
    public virtual ContentReference GetRootLink();
}
```

A `ContentReference` is rendered as `<id>_<workId>_CatalogContent`. Commerce 9 made `WorkId` unique **across the whole system** (was previously unique per content). `WorkId` triumphs everything else — load resolves to the content WorkId points to.

### Gotchas

- **Commerce has no Waste Basket.** Deleting a catalog content is permanent.
- `CatalogContentProvider` exists but **never use it directly**. Always go through `IContentRepository` / `IContentLoader`.
- `GetContentLink(string code, CatalogContentType type)` used to be slow (no bitwise shortcut) — improved in recent versions but still slower than ID-based conversion.

---

## Chapter 2: Catalog Content Modeling

### Concepts

- All catalog content types inherit from `CatalogContentBase`.
- `RootContent` is virtual and read-only (returned by `ReferenceConverter.GetRootLink()`).
- Sub-base classes: `CatalogContent`, `NodeContent`, `EntryContentBase` → `ProductContent`, `VariationContent`, `PackageContent`, `BundleContent`.

### Key Types

```csharp
// Namespace: EPiServer.Commerce.Catalog.ContentTypes
CatalogContentBase
├── CatalogContent           // root; *cannot* be subclassed / extended
├── NodeContent              // Category
├── EntryContentBase
│   ├── ProductContent
│   ├── VariationContent
│   ├── PackageContent
│   └── BundleContent
└── RootContent              // virtual
```

### Attributes for Modeling

| Attribute | Purpose |
|---|---|
| `[CatalogContentType]` | Inherits from CMS `ContentType`. Marks the class as a creatable catalog content. `GUID`, `MetaClassName`, `GroupName`, `DisplayName` |
| `[ImageUrl("~/Templates/thumbnail.png")]` | Sets thumbnail in Catalog UI |
| `[Display(Name="...", Order=-15)]` | Display config |
| `[Searchable]` | Indexed by search provider |
| `[Tokenize]`, `[IncludeValuesInSearchResults]`, `[IncludeInDefaultSearch]` | Search tuning |
| `[Encrypted]` | Stores column encrypted (no-op on Azure-compatible DB) |
| `[DecimalSettings(Precision, Scale)]` | Max is SQL Server `(38,9)` |
| `[BackingType(typeof(PropertyDictionarySingle))]` | Required for dict types |
| `[BackingType(typeof(PropertyDictionaryMultiple))]` | Multi-value dict |

### Example Content Type

```csharp
[CatalogContentType(GUID = "18EA436F-3B3B-464E-A526-564E9AC454C7",
                    MetaClassName = "Fashion_Product",
                    GroupName = "Fashion",
                    DisplayName = "Fashion product")]
[ImageUrl("~/Templates/thumbnail-fashion.png")]
public class FashionProduct : ProductContent
{
    [Display(Name = "Description", Order = -15)]
    public virtual XhtmlString Info_Description { get; set; }

    [Display(Name = "Facet Brand", Order = -1)]
    [Searchable]
    public virtual string Brand { get; set; }
}
```

### Modeling Rules

- Property name must match metafield name (case-insensitive). `string facet_color` metafield ↔ `string Facet_Color` property.
- Properties **must** be `virtual`. Otherwise `CatalogContentScannerExtension` throws at startup.
- If a metafield is shared between metaclasses, the mapping properties across content types must be identical (same type, same attributes).
- **`CatalogContent` cannot be subclassed.** `CatalogContentScannerExtension` throws `EPiServerException` if you try.
- Asset type full name ≤ 190 characters.

### MetaDataType ↔ PropertyData Map

Common ones:

| MetaDataType | PropertyData |
|---|---|
| `Decimal`, `BigInt`, `Money`, `SmallMoney` | `PropertyDecimal` |
| `DateTime`, `SmallDateTime`, `Date` | `PropertyDate` |
| `Float`, `Real` | `PropertyFloatNumber` |
| `Bit`, `Char`, `Int`, `Integer`, `SmallInt`, `TinyInt` | `PropertyNumber` |
| `NChar`, `VarChar`, `NText`, `Text`, `NVarChar`, `LongString` | `PropertyLongString` |
| `Boolean` | `PropertyBoolean` |
| `Email` | `PropertyEmailAddress` |
| `URL` | `PropertyUrl` |
| `ShortString`, `UniqueIdentifier` | `PropertyString` |
| `LongHtmlString` | `PropertyXhtmlString` |
| `EnumMultiValue`, `DictionaryMultiValue` | `PropertyDictionaryMultiple` |
| `EnumSingleValue`, `DictionarySingleValue` | `PropertyDictionarySingle` |

### `IDimensionalStockPlacement` (shipping dimensions block)

`VariationContent` and `PackageContent` implement `IDimensionalStockPlacement`. Dimensions are exposed as a block:

```csharp
variationContent.ShippingDimensions.Height = 10m;
variationContent.ShippingDimensions.Width  = 20m;
variationContent.ShippingDimensions.Length = 30m;
// Never new up the block; it's never null for a content.
```

### How Properties are Stored (Commerce 9+)

- Single table **`CatalogContentProperty`** for published properties, 10 typed columns (`Boolean`, `Number`, `FloatNumber`, `Money`, `Decimal`, `Date`, `Binary`, `String` ≤ 256, `LongString`, `Guid`).
- Drafts stored in **`ecfVersionProperty`** (schema mirrors `CatalogContentProperty`).
- Old metaclass tables are dropped — replaced with **views** that query `CatalogContentProperty` (≈30 % slower than original tables, but for infrequent reports that's fine).

### Dictionary Types

```csharp
// Single-value dictionary
[BackingType(typeof(PropertyDictionarySingle))]
public virtual string Color { get; set; }

// Multi-value dictionary
[BackingType(typeof(PropertyDictionaryMultiple))]
public virtual IEnumerable<string> Colors { get; set; }
```

String dictionary (key-value pairs) is **not supported** by strongly-typed content nor Catalog UI — manage via `MetaDataPlus` APIs or Commerce Manager.

Storage:

- Single-value dict: `CatalogContentProperty.Number` = `MetaDictionaryId`.
- Multi-value dict: `Number` = `MetaKey` in `MetaKey` table → `MetaMultiValueDictionary` → `MetaDictionary`.
- String dict: `MetaKey` → `MetaStringDictionaryValue` (key+value pairs).

### Gotchas

- You cannot change a metafield's type after save — delete and recreate.
- Property name/type mismatches across classes throw at startup.
- `Decimal` is always stored as `(38,9)` in Commerce 9+ regardless of `[DecimalSettings]`.

---

## Chapter 3: Associations, Relations, Assets

### Concepts

- **Associations**: cross-sell/up-sell between entries.
- **Relations**: structural (node↔node, node↔entry, entry↔entry).
- **Assets**: `IContentMedia` attached to nodes/entries via `IAssetContainer`.

### Key Types

```csharp
public interface IAssociationRepository
{
    IEnumerable<Association> GetAssociations(ContentReference contentLink);
    void RemoveAssociations(IEnumerable<Association> associations);
    void UpdateAssociations(IEnumerable<Association> associations);
}
```

`IRelationRepository` manages:

- `NodeRelation` (node ↔ node)
- `NodeEntryRelation : NodeRelation` (node ↔ entry; has `IsPrimary` from Commerce 11.2)
- `EntryRelation` (abstract) → `ProductVariation`, `BundleEntry`, `PackageEntry`

### Association/Relation Groups

```csharp
var repo = context.Locate.Advanced
    .GetInstance<GroupDefinitionRepository<AssociationGroupDefinition>>();
repo.Add(new AssociationGroupDefinition { Name = "CrossSell" });
repo.Add(new AssociationGroupDefinition { Name = "Upsell" });
```

Same for `RelationGroupDefinition`. Only `default` group exists out of the box.

### Assets — `CommerceMediaCollection`

`EntryContentBase` and `NodeContent` implement `IAssetContainer` and expose `CommerceMediaCollection` (which is `ICollection<CommerceMedia>`):

```csharp
public class CommerceMedia : ICloneable
{
    public ContentReference AssetLink { get; set; }
    public string AssetType { get; set; }   // full type name of the IContentMedia class
    public string GroupName { get; set; }   // e.g. "images", "videos", "manual"
    public int SortOrder { get; set; }
}
```

Media class pattern:

```csharp
[ContentType(GUID = "872AA39E-5B79-43BF-B7D5-F34D415553BD")]
[MediaDescriptor(ExtensionString = "jpg,jpeg,jpe,ico,gif,bmp,png")]
public class ImageFile : CommerceImage
{
    public virtual string Description { get; set; }

    [Editable(false)]
    [ImageDescriptor(Width = 128, Height = 128)]
    public override Blob LargeThumbnail { get; set; }

    [Editable(false)]
    [ImageDescriptor(Width = 64, Height = 64)]
    public override Blob Thumbnail { get; set; }
}
```

`CommerceImage` has `Thumbnail` (48×48, inherited) and `LargeThumbnail` (256×256, used in Catalog UI detail header). The **first** `CommerceImage` (lowest `SortOrder`) supplies thumbnails. Generated blobs are stored side-by-side with the original; a CMS scheduled job clears them.

### Versioning

```csharp
var variation = contentRepo.GetDefault<VariationContent>(parentLink);
variation.Name = "New variation";
var link = contentRepo.Save(variation, SaveAction.Save, AccessLevel.NoAccess);   // CheckedOut

variation = contentRepo.Get<VariationContent>(link).CreateWritableClone<VariationContent>();
variation.Code = "New-variation";
link = contentRepo.Save(variation, SaveAction.CheckIn, AccessLevel.NoAccess);    // Ready to Publish

// Publish overwriting current version
link = contentRepo.Save(variation,
    SaveAction.Publish | SaveAction.ForceCurrentVersion, AccessLevel.NoAccess);
```

Loading a specific previously-published version:

```csharp
var versions = contentVersionRepository.List(contentLink);
var prev = versions.OrderByDescending(c => c.Saved)
    .FirstOrDefault(v => v.Status == VersionStatus.PreviouslyPublished
                         && v.LanguageBranch == "en");
```

### Language Versions

- Languages controlled at catalog level (`CatalogContent.CatalogLanguages`).
- Missing language versions are **lazy-created** on demand.
- Non-`[CultureSpecific]` properties come from the master language and are read-only in other languages.
- Default non-culture-specific properties: `Code`, `Name`, `Markets`, `Assets`, prices, inventories.
- **Do not** mutate `ExistingLanguages`; it's required to be writable (ILocalizable), but changing it breaks internals.

### Version Settings

- `DisableVersionSync` (appSettings) — skip version creation on `ICatalogSystem` updates (faster PIM imports, but no version history).
- `uiMaxVersions` / `EPiServer.Configuration.Settings.Instance.UIMaxVersions` — global cap (applies to both CMS and Commerce). Default 20.
- Per-save option:

```csharp
var extendedAction = SaveAction.Publish.SetExtendedActionFlag(ExtendedSaveAction.ClearVersions);
```

### Access Rights

Only `Catalog` and `Category` (`NodeContent`) support access rights. Entries inherit from their primary parent node; nodeless entries inherit from the catalog.

```csharp
var descriptor = _contentSecurityRepository.Get(catalogLink);
_contentSecurityRepository.Save(catalogLink, descriptor, SecuritySaveType.Replace);
```

### Import/Export — `CatalogImportExport` and `ImportJob`

```csharp
// Import
var importJob = new ImportJob(sourceZipFile, sourceXmlInZip, overwriteDuplicates, isModelsAvailable);
importJob.Execute(addMessageAction, cancellationToken);

// Export (always full)
var importExport = new CatalogImportExport();
using var fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite);
importExport.Export(CatalogName, fs, folderPath);
```

- Export is always full; import can be partial.
- Asset mappings are skipped if exporting inside Commerce Manager without strongly-typed models available.
- CSV import path: `CatalogContext.MetaDataContext` → `EntryMappingMetaClass` → `Rule.XmlDeserialize` → `CsvIncomingDataParser` → `mappingMetaClass.FillData(...)`. No CSV export built in.

### Batch API (Commerce 13.10+)

```csharp
public static void Publish(
    this IContentRepository contentRepository,
    IEnumerable<IContent> contents,
    PublishAction publishAction);
```

5–10× faster than looping `Save`. Bypasses the publishing pipeline — **skips validation**. Falls back to normal path if list contains a new content or a Catalog.

### Performance Considerations

- Don't exceed ~2000 entries per node. `SimplifiedCatalogListingThreshold` (appSettings) controls when Catalog UI switches to flat view.
- Put what you can on `ProductContent` (shared across variations) rather than per-variation metafields.
- Non-culture-specific fields are shared across languages — reduces row count in `CatalogContentProperty`.
- **Don't call `IContentRepository.Get` in a loop.** Use `GetItems` or search providers.
- `UrlResolver.GetUrl` recursively loads ancestors — cache it (CMS caches internally in newer versions) or include the URL in your search index.
- `ReferenceConverter.GetCode(ContentReference)` is much cheaper than loading the content.
- Don't use `IContentLoader.GetChildren` for a product listing page — use search.

### Gotchas

- "Catalog" vs "a content of catalog" (both called *CatalogContent*) is ambiguous. Be precise.
- Changing a catalog's default language loses non-culture-specific values.

---

## Part 1.1: Important Concepts

## Chapter 3.1: Markets

### Concepts

A Market is an abstract segmentation of operations (country, region, etc.). Affects prices, inventories (indirectly), orders, shipping methods, payment gateways.

### Key Types

```csharp
public interface IMarket
{
    MarketId MarketId { get; }
    bool IsEnabled { get; }
    string MarketName { get; }
    string MarketDescription { get; }
    CultureInfo DefaultLanguage { get; }
    IEnumerable<CultureInfo> Languages { get; }
    Currency DefaultCurrency { get; }
    IEnumerable<Currency> Currencies { get; }
    IEnumerable<string> Countries { get; }
}

public interface IMarketService
{
    IEnumerable<IMarket> GetAllMarkets();
    IMarket GetMarket(MarketId marketId);
    void CreateMarket(IMarket market);
    void UpdateMarket(IMarket market);
    void DeleteMarket(MarketId marketId);
}

public interface ICurrentMarket
{
    IMarket GetCurrentMarket();
    void SetCurrentMarket(MarketId marketId);
}
```

- `Market` is the default implementation of `IMarket`.
- `MarketId.Default` = `"DEFAULT"` — predefined; disable if unused.
- Entries excluded from a market via the special metafield `_ExcludedCatalogEntryMarkets`.
- Prices belong to a market; warehouses/inventories do not (warehouse can serve many markets).
- Orders always belong to a market.

### Extension Points

- Override `ICurrentMarket` to derive the market from geo-IP, cookie, subdomain, etc. (Quicksilver's `CurrentMarket`, Sample's `MarketStorage` use cookies.)

### Gotchas

- Shipping methods & payment gateways are still grouped by **language** not market — legacy. You must filter yourself by market.
- `ICurrentMarket` is named like data but is actually a service (historical).

---

## Part 2: Inventories and Prices

## Chapter 4: Prices

### Concepts

Prices were decoupled from entries in Commerce R3. There are **two price APIs**:

- `IPriceService` — read-optimized, batch-oriented, caches, **optimizes** prices (best-for-customer) when saving. Use for **loading** prices.
- `IPriceDetailService` — per-row editing. Saves as-is. Used by Catalog UI price editor. Use for **saving/editing** prices.

Both sync to each other via `ReplicatePriceServiceChanges` / `ReplicatePriceDetailChanges`.

### Key Types

```csharp
public interface IPriceValue
{
    CatalogKey CatalogKey { get; }
    MarketId MarketId { get; }
    CustomerPricing CustomerPricing { get; }
    DateTime ValidFrom { get; }
    DateTime? ValidUntil { get; }
    decimal MinQuantity { get; }
    Money UnitPrice { get; }
}

public interface IPriceDetailValue : IPriceValue
{
    long PriceValueId { get; set; }
}

public class CatalogKey
{
    public CatalogKey(string code);
    public CatalogKey(Guid applicationId, string code);  // legacy; ApplicationId is always the same now
    public string CatalogEntryCode { get; }
}

public class CustomerPricing
{
    public static readonly CustomerPricing AllCustomers;
    public enum PriceType { AllCustomers = 0, UserName = 1, PriceGroup = 2 }
    public PriceType PriceTypeId { get; }
    public string PriceCode { get; }  // username or pricegroup name
}
```

`ReadOnlyPriceValue` (returned) vs `PriceValue` (editable). Copy-constructor:

```csharp
var editable = new PriceValue(readonlyPrice);
```

### IPriceService surface

```csharp
public interface IPriceService
{
    bool IsReadOnly { get; }
    IPriceValue GetDefaultPrice(MarketId market, DateTime validOn, CatalogKey catalogKey, Currency currency);
    IEnumerable<IPriceValue> GetPrices(MarketId market, DateTime validOn, CatalogKey catalogKey, PriceFilter filter);
    IEnumerable<IPriceValue> GetPrices(MarketId market, DateTime validOn, IEnumerable<CatalogKey> catalogKeys, PriceFilter filter);
    IEnumerable<IPriceValue> GetPrices(MarketId market, DateTime validOn, IEnumerable<CatalogKeyAndQuantity> catalogKeysAndQuantities, PriceFilter filter);
    IEnumerable<IPriceValue> GetCatalogEntryPrices(CatalogKey catalogKey);
    IEnumerable<IPriceValue> GetCatalogEntryPrices(IEnumerable<CatalogKey> catalogKeys);
    void SetCatalogEntryPrices(CatalogKey catalogKey, IEnumerable<IPriceValue> priceValues);
    void SetCatalogEntryPrices(IEnumerable<CatalogKey> catalogKeys, IEnumerable<IPriceValue> priceValues);
    void ReplicatePriceDetailChanges(IEnumerable<CatalogKey> catalogKeys, IEnumerable<IPriceValue> priceValues);
}
```

### IPriceDetailService surface

```csharp
public interface IPriceDetailService
{
    bool IsReadOnly { get; }
    IPriceDetailValue Get(long priceValueId);
    IList<IPriceDetailValue> List(ContentReference catalogContentReference);
    IList<IPriceDetailValue> List(ContentReference catalogContentReference, int offset, int count, out int totalCount);
    IList<IPriceDetailValue> List(ContentReference catalogContentReference, MarketId marketId, PriceFilter filter, int offset, int count, out int totalCount);
    IList<IPriceDetailValue> Save(IEnumerable<IPriceDetailValue> priceValues);
    void Delete(IEnumerable<long> priceValueIds);
    void ReplicatePriceServiceChanges(IEnumerable<CatalogKey> catalogKeySet, IEnumerable<IPriceValue> priceValuesList);
}
```

`List(contentRef, …)` returns prices of children when passed a product/bundle/category (direct children only for category).

### Price Optimization Behavior

`IPriceService` merges overlapping periods keeping the **lowest** price; higher prices with higher `MinQuantity` may be kept. Imports may not round-trip if you had dominated prices.

### Extension Points

- Custom `PriceGroup` pattern (abuse `CustomerPricing.PriceType.PriceGroup` + `PriceCode = "MSRP"` as a tag) — example from the book for MSRP / list price:

```csharp
public static void SaveMsrp(this IPriceDetailService svc,
    ContentReference contentLink, MarketId marketId, Currency currency, decimal amount)
{
    var entry = ServiceLocator.Current.GetInstance<IContentRepository>()
        .Get<EntryContentBase>(contentLink);
    var filter = new PriceFilter { Currencies = new[] { currency }.ToList() };
    int total;
    var msrp = (PriceDetailValue)svc.List(contentLink, marketId, filter, 0, int.MaxValue, out total)
        .FirstOrDefault(p => p.UnitPrice.Currency == currency
            && p.CustomerPricing.PriceTypeId == CustomerPricing.PriceType.PriceGroup
            && p.CustomerPricing.PriceCode == "MSRP");

    if (msrp == null)
    {
        msrp = new PriceDetailValue
        {
            CatalogKey = new CatalogKey(entry.Code),
            CustomerPricing = new CustomerPricing(CustomerPricing.PriceType.PriceGroup, "MSRP"),
            MarketId = marketId,
            MinQuantity = 0,
            ValidFrom = entry.StartPublish ?? DateTime.UtcNow
        };
    }
    msrp.UnitPrice = new Money(amount, currency);
    svc.Save(new[] { msrp });
}
```

- Register a new PriceGroup value in `ContactGroup` enum metafield so it shows in Catalog UI:

```csharp
var metaFieldType = DataContext.Current.MetaModel.RegisteredTypes["ContactGroup"];
var items = MetaEnum.GetItems(metaFieldType);
if (!items.Any(i => string.Equals(i.Name, "MSRP", StringComparison.OrdinalIgnoreCase)))
{
    MetaEnum.AddItem(metaFieldType, "MSRP", items.Count() + 1);
}
```

- To truly extend the price schema you must implement `IPriceService` (and probably `IPriceDetailService`) from scratch with new tables.

### Gotchas

- `CatalogKey.ApplicationId` is legacy; multi-tenancy was never fully delivered. Always one `AppContext.Current.ApplicationId` per DB.
- Commerce 13.30 had a bug: price change fired one event per price; fixed in later releases — can flood Azure Service Bus.
- Cache added to `IPriceDetailService` in 13.30+.

---

## Chapter 5: Inventories

### Concepts

- **Warehouse**: physical/logical location. Managed via `IWarehouseRepository`; default impl = `Mediachase.Commerce.Inventory.Warehouse`.
- **Inventory**: per-SKU + per-warehouse. Old API: `IWarehouseInventoryService`. **New API (Commerce 7.19 / fully integrated 8.7)**: `IInventoryService` + `InventoryRecord` + request/response model.

### Key Types

```csharp
public interface IWarehouseRepository
{
    IWarehouse Get(string code);
    void Save(IWarehouse warehouse);
    // ... (Delete, List)
}

public class InventoryRecord : InventoryKey, IReadOnly<InventoryRecord>
{
    public virtual bool IsTracked { get; set; }
    public virtual decimal PurchaseAvailableQuantity { get; set; }
    public virtual decimal PreorderAvailableQuantity { get; set; }
    public virtual decimal BackorderAvailableQuantity { get; set; }
    public virtual decimal PurchaseRequestedQuantity { get; set; }
    public virtual decimal PreorderRequestedQuantity { get; set; }
    public virtual decimal BackorderRequestedQuantity { get; set; }
    public virtual DateTime PurchaseAvailableUtc { get; set; }
    public virtual DateTime PreorderAvailableUtc { get; set; }
    public virtual DateTime BackorderAvailableUtc { get; set; }
    public virtual decimal AdditionalQuantity { get; set; }
    public virtual decimal ReorderMinQuantity { get; set; }
}

public interface IInventoryService
{
    InventoryRecord Get(string catalogEntryCode, string warehouseCode);
    IList<InventoryRecord> List();                                  // DO NOT USE in prod
    IList<InventoryRecord> QueryByEntry(IEnumerable<string> codes); // preferred
    IList<InventoryRecord> QueryByEntry(IEnumerable<string> codes, int offset, int count, out int totalCount);
    InventoryResponse Request(InventoryRequest request);
    // ... many more
}
```

### Request/Response Model

Instead of "set value to 8", you send an `InventoryRequest` saying "request a Purchase of 2". The DB applies a relative adjustment atomically (no race condition).

```csharp
public class InventoryRequest
{
    public DateTime RequestDateUtc { get; set; }
    public IList<InventoryRequestItem> Items { get; set; }
    public object Context { get; set; }
}

public class InventoryRequestItem
{
    public int ItemIndex { get; set; }
    public InventoryRequestType RequestType { get; set; }
    public string CatalogEntryCode { get; set; }
    public string WarehouseCode { get; set; }
    public decimal Quantity { get; set; }
    public string OperationKey { get; set; }
    public object Context { get; set; }
}

public enum InventoryRequestType
{
    Purchase, Preorder, Backorder, PurchaseOrPreorder,
    Complete, Cancel, Split, Custom
}
```

`OperationKey` is an opaque serialized token encoding the change set (handled by `OperationKeySerializer` → `InventoryChange`). Stored on `Shipment.OperationKeysMap` (`ConcurrentDictionary<int, IEnumerable<string>>`) keyed by line-item index. Required for subsequent `Complete` / `Cancel` / `Split` requests. Call `Shipment.ClearOperationKeys()` after sending.

```csharp
private void SendRequests(Shipment shipment, InventoryRequestType type)
{
    int idx = 0;
    var items = shipment.OperationKeysMap.SelectMany(c => c.Value).Select(key =>
        new InventoryRequestItem { ItemIndex = idx++, OperationKey = key, RequestType = type })
        .ToList();
    if (items.Count == 0) return;
    _inventoryService.Request(new InventoryRequest(DateTime.UtcNow, items, null));
    shipment.ClearOperationKeys();
}
```

### Warehouse Mutation (Commerce 12+)

`IWarehouse` implements `IReadOnly<T>`:

```csharp
var warehouse = warehouseRepository.Get("MAIN");
var writable = warehouse.CreateWriteableClone();
writable.Name = "Main Warehouse";
warehouseRepository.Save(writable);
```

### Cart Validation Shortcuts (Commerce 10+ replacement for workflows)

```csharp
cart.ValidateOrRemoveLineItems((item, issue) => issues.AddValidationIssues(item, issue),
    _lineItemValidator);
cart.UpdatePlacedPriceOrRemoveLineItems(contact,
    (item, issue) => issues.AddValidationIssues(item, issue), _placedPriceProcessor);
cart.UpdateInventoryOrRemoveLineItems((item, issue) => issues.AddValidationIssues(item, issue),
    _inventoryProcessor);
```

### Extension Points

- Subclass `InventoryRecord` + custom table + custom SPs + implement `IInventoryService` — heavy investment.

### Gotchas

- `IInventoryService.List()` loads everything — never call it in production.
- `QueryByPartialKeys` not effectively cached; `QueryByEntry` is.
- Commerce 10.7 fixed global cache invalidation on every inventory change — only related keys invalidate now.
- `InventoryTrackingStatus` (warehouse level) vs `VariationContent.TrackInventory` (SKU level) — the latter overrides the former if `false`.

---

## Part 3: The Order System

## Chapter 6: The Order System

### Concepts

Old system: `OrderGroup` + its derivatives (`Cart`, `PurchaseOrder`, `PaymentPlan`) all share the `OrderGroup` table → placing an order = delete cart row, insert PO row. Not mockable, not testable, performance issues on contention.

New system (from ~Commerce 10.x): abstraction layer — **interface-first** — `IOrderGroup`, `IOrderForm`, `IShipment`, `ILineItem`, `IPayment`, `IOrderAddress`, managed by `IOrderRepository`. Default impl still hits the same tables; serializable cart mode (Commerce 10.2+) replaces cart storage with a single JSON blob in `SerializableCart.Data`.

### Key Types

```csharp
public interface IOrderGroup : IExtendedProperties
{
    OrderReference OrderLink { get; }
    IEnumerable<IOrderForm> Forms { get; }
    IOrderAddress Address { get; set; }
    IMarket Market { get; }
    Currency Currency { get; }
    // ...
}

public interface IExtendedProperties
{
    Hashtable Properties { get; }   // metafield property bag
}

public interface IOrderRepository
{
    TOrderGroup Create<TOrderGroup>(Guid customerId, string name) where TOrderGroup : class, IOrderGroup;
    IOrderGroup Load(OrderReference orderLink);
    TOrderGroup Load<TOrderGroup>(int orderGroupId) where TOrderGroup : class, IOrderGroup;
    IEnumerable<TOrderGroup> Load<TOrderGroup>(Guid customerId, string name) where TOrderGroup : class, IOrderGroup;
    OrderReference Save(IOrderGroup order);
    OrderReference SaveAsPaymentPlan(IOrderGroup cart);
    OrderReference SaveAsPurchaseOrder(IOrderGroup cart);
    void Delete(OrderReference orderLink);
}
```

Sub-interfaces: `ICart : IOrderGroup`, `IPurchaseOrder : IOrderGroup`, `IPaymentPlan : IOrderGroup`.

`OrderReference`:

```csharp
public class OrderReference
{
    public int OrderGroupId { get; }
    public string Name { get; }
    public Guid CustomerId { get; }
    public Type OrderType { get; }
}
```

Interface members are **explicitly implemented** on the concrete types:

```csharp
// Doesn't compile:
orderGroup.Forms
// Correct:
((IOrderGroup)orderGroup).Forms
```

Enforces use of the interfaces (good for testability and swappability).

### Serializable Cart Mode

- Cart saved as one JSON blob — reduces contention with PO table.
- Enabled via:

```csharp
_featureSwitch.EnableFeature(SerializedCarts.FeatureSerializedCarts);
```

- A scheduled job migrates legacy carts.
- New sites have it enabled by default.

### OrderValidationService

Centralizes cart validation so you don't re-validate at every step:

```csharp
public virtual IDictionary<ILineItem, IList<ValidationIssue>> ValidateOrder(IOrderGroup orderGroup);
```

Default behavior:

```csharp
orderGroup.ValidateOrRemoveLineItems();
orderGroup.UpdatePlacedPriceOrRemoveLineItems();
orderGroup.UpdateInventoryOrRemoveLineItems();
orderGroup.ApplyDiscounts();
orderGroup.UpdateInventoryOrRemoveLineItems();
```

### Gotchas

- Serializable cart limitations:
  - Cannot query contents (e.g. "carts containing SKU X") other than `LIKE %...%` on `Data` column.
  - Every tiny change rewrites the whole blob → log transaction cost for huge B2B carts.
  - Watch cart size: `SELECT TOP 1000 CartId, LEN(Data) AS SIZE FROM SerializableCart ORDER BY SIZE DESC`. Keep below ~1 MB.
- `Hashtable` (not `IDictionary<string,object>`) on `IExtendedProperties` is legacy — too late to change.
- Commerce 13 had two `DefaultOrderRepository`s (`EPiServer.Commerce.Order` and `...Internal`). The public one was obsoleted in Commerce 14. Stick with the interface.
- `IOrderRepository` has **no batch save/delete**. Use `IOrderSearchService` / `OrderContext.Search` for bulk access.

---

## Chapter 6.5: The Old Order System

### Structure

```
OrderGroup
├── OrderForm[]
│   ├── Payment[]
│   ├── LineItem[]                 (owned here, not by shipment)
│   ├── Shipment[]
│   │   ├── ShippingAddress
│   │   └── (LineItem index↔quantity map)
│   └── OrderFormDiscount[]
├── OrderAddress[]
└── OrderNote[]
```

### Concrete Types

- `OrderGroup` (abstract) ← `Cart` (metaclass `ShoppingCart`), `PurchaseOrder`, `PaymentPlan`.
- All are `MetaObject`s — call `.AcceptChanges()` to persist, `.Delete()` + `.AcceptChanges()` to remove.
- `Cart` uniqueness: one per (customer, market, name). Attempting to create a duplicate throws.
- `Cart.WishListName` = `"WishList"` — convention: no inventory checks, no quantity display, only for logged-in users.
- `CartHelper` (obsolete; `Mediachase.Commerce.Website` package) — God-class, untestable; avoid in new code.

### PaymentPlan

```csharp
public class PaymentPlan : OrderGroup
{
    public virtual PaymentPlanCycle CycleMode { get; set; }
    public virtual int CycleLength { get; set; }
    public virtual int MaxCyclesCount { get; set; }
    public virtual int CompletedCyclesCount { get; set; }
    public virtual DateTime StartDate { get; set; }
    public virtual DateTime EndDate { get; set; }
    public virtual bool IsActive { get; set; }
}

public enum PaymentPlanCycle { None, Days, Weeks, Months, Years, Custom1, Custom2 }
```

Built-in scheduled job "Subscription Payment Plans" converts due plans into POs.

### PurchaseOrder Extras

```csharp
public class PurchaseOrder : OrderGroup
{
    public int ParentOrderGroupId { get; set; }      // link to PaymentPlan/other
    public string TrackingNumber { get; set; }
    public DateTime ExpirationDate { get; set; }
    public OrderFormCollection ReturnOrderForms { get; }
}
```

Typical placement flow (old):

```csharp
OrderGroupWorkflowManager.RunWorkflow(cart,
    OrderGroupWorkflowManager.CartCheckOutWorkflowName, true, dic);
cart.CustomerId = PrincipalInfo.CurrentPrincipal.GetContactId();
var po = cart.SaveAsPurchaseOrder();
OrderNotesManager.AddNoteToPurchaseOrder(po, noteDetail, OrderNoteTypes.System, contactId);
po.AcceptChanges();
cart.Delete();
cart.AcceptChanges();
```

`SaveAsPurchaseOrder` / `SaveAsPaymentPlan` *copies* — the original cart must be deleted.

### Line-Item / Shipment Relationship — The Quirk

LineItems belong to the **OrderForm**, not the shipment. Shipment stores an **index→quantity** map over the OrderForm's LineItems collection. Removing a LineItem, reordering, or splitting shipments without using `PurchaseOrderManager` methods causes stale indices or `IndexOutOfRangeException`. Use the manager helpers; don't hand-edit the map.

---

## Part 4: Events

## Chapter 8: Content Events

### Concepts

Because catalog content is CMS content, you can use `IContentEvents` for most catalog operations:

```csharp
_contentEvents.MovingContent += (s, e) =>
{
    if (e.Content is CatalogContentBase) { /* ... */ }
};
```

**But** `IContentEvents` fires only for operations via `IContentRepository`. ServiceAPI uses `ICatalogSystem` internally → skip content events. For that, use `CatalogEventListenerBase`.

### Key Types

```csharp
public abstract class CatalogEventListenerBase
{
    public virtual void EntryUpdating(object source, EntryEventArgs args) { }
    public virtual void EntryUpdated(object source, EntryEventArgs args) { }
    public virtual void EntryDeleted(object source, DeletedEntryEventArgs args) { }
    public virtual void NodeUpdating(object source, NodeEventArgs args) { }
    public virtual void NodeUpdated(object source, NodeEventArgs args) { }
    public virtual void NodeDeleted(object source, DeletedNodeEventArgs args) { }
    public virtual void CatalogUpdating(object source, CatalogEventArgs args) { }
    public virtual void CatalogUpdated(object source, CatalogEventArgs args) { }
    public virtual void CatalogDeleted(object source, DeletedCatalogEventArgs args) { }
    public virtual void CatalogLanguageUpdated(object source, CatalogLanguageEventArgs args) { }
    public virtual void AssociationUpdating(object source, AssociationEventArgs args) { }
    public virtual void AssociationUpdated(object source, AssociationEventArgs args) { }
    public virtual void AssociationDeleted(object source, DeletedAssociationEventArgs args) { }
    public virtual void RelationUpdating(object source, RelationEventArgs args) { }
    public virtual void RelationUpdated(object source, RelationEventArgs args) { }
    public virtual void RelationDeleted(object source, DeletedRelationEventArgs args) { }
}
```

### CatalogEventBroadcaster — Remote Events

`CatalogEventBroadcaster` (an implementation of `CatalogEventListenerBase`) re-broadcasts events via **EPiServer Framework Events** (Azure Service Bus on DXP/Azure, UDP on-premise). Subscribe with `Event.Get(Guid)`:

```csharp
public void AddEvent()
{
    var evt = Event.Get(CatalogEventBroadcaster.CommerceProductUpdated);
    evt.Raised += CatalogEventUpdated;
}

private void CatalogEventUpdated(object sender, EventNotificationEventArgs e)
{
    var args = DeSerialize(e.Param) as CatalogContentUpdateEventArgs;
    // args.EventType (string), args.CatalogIds, args.CatalogNodeIds,
    // args.CatalogEntryIds, args.CatalogAssociationIds
}

private static EventArgs DeSerialize(byte[] buffer)
{
    var formatter = new BinaryFormatter();
    using var s = new MemoryStream(buffer);
    return formatter.Deserialize(s) as EventArgs;
}
```

### Event Type Strings (for broadcast EventType dispatch)

```
CatalogUpdating, CatalogUpdated, CatalogDeleted, CatalogLanguageUpdated,
CatalogNodeUpdating, CatalogNodeUpdated, CatalogNodeDeleted,
CatalogEntryUpdating, CatalogEntryUpdated, CatalogEntryDeleted,
AssociationUpdating, AssociationUpdated, AssociationDeleted,
RelationUpdating, RelationUpdated, RelationDeleted,
LanguageConfigurationChanged
```

### Gotchas

- At `Deleted` time, content is gone — do your logic in `Deleting`.
- Use `Deleting` / `Updating` pre-events for validations; post-events for cache invalidation.

---

## Chapter 9: Inventory, Price, Order Events

### Inventory & Price Events

- Both built on `CatalogKeyEventBroadcaster`. Find.Commerce listens to these and reindexes automatically.
- Inventory events fire frequently — reconsider whether to reindex on every inventory change.

### Order Events — Old (OrderContext)

```csharp
OrderContext.OrderGroupUpdated += (s, e) =>
{
    if (s is PurchaseOrder po) { /* ... */ }
};
OrderContext.OrderGroupDeleted += ...
```

Local-only (not broadcast across servers).

### Order Events — New (`IOrderEvents`)

```csharp
public interface IOrderEvents
{
    event EventHandler<OrderEventArgs> SavingOrder;
    event EventHandler<OrderEventArgs> SavedOrder;
    event EventHandler<OrderEventArgs> DeletingOrder;
    event EventHandler<OrderEventArgs> DeletedOrder;
}
```

Only fires when you save via `IOrderRepository`. BETA-marked (more events may be added). Register in an `IInitializationModule`:

```csharp
public class OrderEventListener
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderEvents _orderEvents;
    private readonly ILogger _logger = LogManager.GetLogger(typeof(OrderEventListener));

    public OrderEventListener(IOrderRepository r, IOrderEvents e) { _orderRepository = r; _orderEvents = e; }

    public void AddEvents()
    {
        _orderEvents.SavedOrder += OnSaved;
        _orderEvents.DeletingOrder += OnDeleting;
    }
    public void RemoveEvents()
    {
        _orderEvents.DeletingOrder -= OnDeleting;
        _orderEvents.SavedOrder    -= OnSaved;
    }

    private void OnSaved(object s, OrderEventArgs e)
    {
        if (e.OrderGroup is IPurchaseOrder po)
            _logger.Information($"Order {po.OrderNumber} was saved");
    }

    private void OnDeleting(object s, OrderEventArgs e)
    {
        var cart = _orderRepository.Load<ICart>(e.OrderLink.OrderGroupId);
        if (cart != null)
            _logger.Information($"Cart '{cart.Name}' for '{cart.CustomerId}' being deleted.");
    }
}
```

---

## Part 5: Search

## Chapter 11a: Search

### Where SearchProvider is Used

- Primarily Commerce Manager-side (catalog management, new line-item dialog, gift picker in promotions).
- Editing UI Catalog UI quick search / global search.
- **SQL fallback** used for "Search by Code/Id" in Catalog Management.
- Not to be confused with EPiServer Search (CMS pages only).

### Provider Types

| Provider | Storage | Notes |
|---|---|---|
| `LuceneSearchProvider` | Local Lucene.net files | Default; needs shared filesystem for load balanced setups |
| `Solr35SearchProvider` | Solr 3.5 server | Better scale; needs HTTP Solr setup |
| `FindSearchProvider` | Find service | Wraps Find for search-provider usage — *not* the same as Find.Commerce |

### Simple Search API

```csharp
var criteria = new CatalogEntrySearchCriteria { SearchPhrase = "OLED" };
var manager = new SearchManager(AppContext.Current.ApplicationName);
var results = manager.Search(criteria);
var ids = results.GetKeyFieldValues<int>();
```

### `CatalogEntrySearchCriteria` — key props

- `CatalogNames`, `CatalogNodes`, `Outlines`, `ClassTypes` — `StringCollection` filters (leaving empty disables the filter).
- `Add(field, value)` / `Add(field, IList<ISearchFilterValue>)` — field-specific filters.

```csharp
criteria.CatalogNames.Add("Consumer");
criteria.CatalogNodes.Add("TV");
criteria.ClassTypes.Add("Product");
criteria.Add("displayname", "OLED");
```

### Faceting — `SearchFilter`

Defined in `Mediachase.Search.Filters.Config`:

```xml
<SearchConfig version="1.0">
  <SearchFilters>
    <Filter field="size">
      <Descriptions defaultLocale="en">
        <Description locale="en">Size</Description>
      </Descriptions>
      <Values>
        <SimpleValue key="s" value="s">
          <Descriptions defaultLocale="en"><Description locale="en">S</Description></Descriptions>
        </SimpleValue>
        <SimpleValue key="m" value="m"><!-- ... --></SimpleValue>
      </Values>
    </Filter>
  </SearchFilters>
</SearchConfig>
```

Three value types:

- `SimpleValue` — literal match
- `RangeValue` — `lowerbound` / `upperbound` (and inclusion flags) for any numeric
- `PriceRangeValue` — same but currency-aware

Access at runtime:

```csharp
foreach (SearchFilter f in SearchFilterHelper.Current.SearchConfig.SearchFilters)
    criteria.Add(f);
```

### `SearchProvider` Configuration (`Mediachase.Search.config`)

```xml
<Mediachase.Search>
  <SearchProviders defaultProvider="LuceneSearchProvider">
    <providers>
      <add name="LuceneSearchProvider"
           type="Mediachase.Search.Providers.Lucene.LuceneSearchProvider, Mediachase.Search.LuceneSearchProvider"
           queryBuilderType="Mediachase.Search.Providers.Lucene.LuceneSearchQueryBuilder, Mediachase.Search.LuceneSearchProvider"
           storage="<path>" simulateFaceting="true" />
      <add name="FindSearchProvider"
           type="EPiServer.Commerce.FindSearchProvider.FindSearchProvider, EPiServer.Commerce.FindSearchProvider"
           serviceUrl="https://..." defaultIndex="myindex" />
    </providers>
  </SearchProviders>
  <Indexers basePath="<path>">
    <add name="catalog"
         type="Mediachase.Search.Extensions.Indexers.CatalogIndexBuilder, Mediachase.Search.Extensions" />
  </Indexers>
</Mediachase.Search>
```

### Implementing a SearchProvider

```csharp
public abstract class SearchProvider : ProviderBase
{
    public abstract string QueryBuilderType { get; }
    public abstract ISearchResults Search(string applicationName, ISearchCriteria criteria);
    public abstract void Index(string applicationName, string scope, ISearchDocument document);
    public abstract int Remove(string applicationName, string scope, string key, string value);
    public abstract void RemoveAll(string applicationName, string scope);
    public abstract void Close(string applicationName, string scope);
    public abstract void Commit(string applicationName);
}

public interface ISearchIndexBuilder
{
    event SearchIndexHandler SearchIndexMessage;
    SearchManager Manager { get; set; }
    IndexBuilder Indexer { get; set; }
    void BuildIndex(bool rebuild);
    bool UpdateIndex(IEnumerable<int> itemIds);
}
```

Usually subclass `BaseCatalogIndexBuilder` and override `IndexCatalogEntryDto`.

### Index Processor Statuses

`Valid`, `Invalid` (user-invalidated), `Recovering` (rebuilding), `Closed` (broken — needs site restart), `Opening` (brief startup state). Change queue via `ChangeNotificationManager` heartbeat (~5s). Commerce 11.4 introduced a new queuing system that fixes most of the old "stuck" behavior.

### SQL Search API

```csharp
var options = new CatalogSearchOptions();
var searchParams = new CatalogSearchParameters();
options.Classes.Add("Variant");
options.RecordsToRetrieve = 200;
options.StartingRecord = 0;
int totalCount = 0;
var entryDto = catalogSystem.FindItemsDto(searchParams, options, ref totalCount,
    new CatalogEntryResponseGroup(CatalogEntryResponseGroup.ResponseGroup.Info));
```

`CatalogSearchParameters.SqlWhereClause` / `SqlMetaWhereClause` are the power tools:

```csharp
// On CatalogEntry columns
searchParams.SqlWhereClause =
    " [CatalogEntry].[EndDate] < '2025-01-01 00:00:00' ";

// On metafields (views)
searchParams.SqlMetaWhereClause =
    " META.MetaFieldName = 'Color' AND META.LongString = 'Blue' ";
```

**Important:** since Commerce 9, `META.MetaFieldName` pattern only allows **one** condition per query — can't do `META.Color='RED' AND META.Size='Large'` in one hit. Do two queries and intersect. Search APIs ignore versions — published data only.

### Find.Commerce

`EPiServer.Commerce.Find` (separate NuGet, independent release schedule). Listens to price & inventory events for auto-reindex. Built on `SearchClient.Instance.Conventions` + LINQ-like API.

Example typical search:

```csharp
var results = SearchClient.Instance
    .Search<VariationContent>()
    .For(searchText)
    .FilterPriceAvailableForCurrentUser()
    .FilterForVisitor()
    .Select(x => new
    {
        x.ContentLink,
        Inventory   = ((IStockPlacement)x).Inventories(),
        Prices      = ((IPricing)x).Prices(),
        ImageUrl    = x.DefaultImageUrl(),
        x.DisplayName,
        Description = x.Description(),
        x.Code
    })
    .ApplyBestBets()
    .Track()
    .Skip(startIndex)
    .Take(pageSize)
    .GetResult();
```

Override conventions in an `IInitializableModule` that depends on `FindCommerceInitializationModule`:

```csharp
[InitializableModule]
[ModuleDependency(typeof(EPiServer.Find.Commerce.FindCommerceInitializationModule))]
public class MyFindModule : IInitializableModule
{
    public void Initialize(InitializationEngine context)
    {
        // Stop indexing inventories
        SearchClient.Instance.Conventions
            .ForInstancesOf<IStockPlacement>()
            .ExcludeField(c => c.Inventories());
    }
    public void Uninitialize(InitializationEngine context) { }
}
```

Unhook default inventory-triggered indexing:

```csharp
public void Initialize(InitializationEngine context)
{
    _broadcaster = context.Locate.Advanced.GetInstance<CatalogKeyEventBroadcaster>();
    _listener    = context.Locate.Advanced.GetInstance<CatalogContentEventListener>();
    _broadcaster.InventoryUpdated -= _listener.InventoryUpdated;
}
```

Custom indexer:

```csharp
private void InventoryUpdated(object sender, InventoryUpdateEventArgs args)
{
    var records = sender as IEnumerable<InventoryRecord>;
    if (records == null) return;

    foreach (var g in records.GroupBy(c => c.CatalogEntryCode))
    {
        if (g.Sum(c => c.PurchaseAvailableQuantity) < 10m)
        {
            _contentEventIndexer.SavingContent(
                _referenceConverter.GetContentLink(g.Key, CatalogContentType.CatalogEntry));
        }
    }
}
```

**Advanced filters:** `PriceLessThan`, `PriceGreaterThan`, `PriceAvailableForCurrentUser`, `FilterForVisitor`, etc.

### Searching for Orders

```csharp
var parameters = new OrderSearchParameters
{
    SqlMetaWhereClause = $"Meta.Modified < GETDATE() - {daysOld}"
};
var options = new OrderSearchOptions { StartingRecord = 0 };
Cart[] carts = OrderContext.Current.Search<Cart>(parameters, options, out int totalRecords);
```

- Old `FindCarts` / `FindPurchaseOrders` methods are obsolete; use the generic `Search<T>` overloads.
- `SqlMetaWhereClause` targets metaclass tables (`META.<FieldName>`), `SqlWhereClause` targets base tables (`[OrderGroup].<Col>`).
- To filter by LineItem metafields (from PO search):

```csharp
parameters.SqlMetaWhereClause = "META.Color = 'Green'";
parameters.SqlWhereClause =
    "OrderGroupId IN (SELECT ObjectId FROM OrderGroup_PurchaseOrder)";
options.Classes.Add("LineItemEx");
var pos = OrderContext.Current.Search<PurchaseOrder>(parameters, options);
```

- To find PO by LineItem code (system metaclass — not in `OrderSearchOptions.Classes`):

```csharp
parameters.SqlWhereClause =
    "OrderGroupId IN (SELECT li.OrderGroupId FROM LineItem li WHERE li.Code = 'SKU-1')";
```

### `IOrderSearchService`

```csharp
public interface IOrderSearchService
{
    OrderSearchResults<TOrderGroup> Find<TOrderGroup>(OrderSearchFilter filter)
        where TOrderGroup : class, IOrderGroup;
    OrderSearchResults<TOrderGroup> Search<TOrderGroup>(
        OrderSearchField searchField, string searchValue, int startIndex, int recordsToRetrieve)
        where TOrderGroup : class, IOrderGroup;
}
```

Serializable carts are hard to search by contents. Perf tip: run the count query once, then paginate with `ReturnTotalCount = false`:

```csharp
_serializableCartDB.FindCarts(cartFilter, out var totalRecords);
cartFilter.ReturnTotalCount = false;
cartFilter.RecordsToRetrieve = 100;

int loaded;
do
{
    var expired = _serializableCartDB.FindCarts(cartFilter).ToList();
    foreach (var cart in expired) _orderRepository.Delete(cart.OrderLink);
    loaded = expired.Count;
} while (loaded > 0);
```

### Gotchas

- `OrderContext.FindPurchaseOrdersByStatus` hits non-indexed `Status` — can cause Clustered Index Scans. Avoid hot calls.
- Search APIs don't know about versions — only published data visible.
- Don't add indexes on system metafields; Created/Modified on `OrderGroup_ShoppingCart` / `OrderGroup_PurchaseOrder` are *candidates* per-site.

---

## Part 6: Promotions

## Chapter 11b: The New Promotion System

### Concepts

Everything is content. Campaigns and promotions are CMS content with `IContentRepository` CRUD and export/import support.

### Key Types

```csharp
// Namespace: EPiServer.Commerce.Marketing
public static class SalesCampaignFolder
{
    public static ContentReference CampaignRoot { get; }
}

public class SalesCampaign : CampaignData
{
    public virtual string Description { get; set; }
    public virtual DateTime ValidFrom { get; set; }
    public virtual DateTime ValidUntil { get; set; }
    public virtual bool IsActive { get; set; }
    public virtual string Code { get; set; }                   // e.g. GA tracking
    public virtual IList<Guid> VisitorGroups { get; set; }
    public virtual string TargetMarket { get; set; }           // or Constants.AllMarkets
    public virtual decimal RevenueGoal { get; set; }
}

public abstract class PromotionData : BasicContent
{
    public virtual string Description { get; set; }
    public virtual PromotionSchedule Schedule { get; set; }
    public virtual bool IsActive { get; set; }
    public virtual CouponData Coupon { get; set; }
    public virtual RedemptionLimitsData RedemptionLimits { get; set; }
    public virtual int Priority { get; set; }                  // lower number = higher priority
    public virtual IList<ContentReference> ExcludedItems { get; set; }  // other promotions/campaigns, not catalog items
    public abstract DiscountType DiscountType { get; }
}

// Abstract subclasses (with DiscountType set)
public abstract class EntryPromotion    : PromotionData { /* DiscountType.LineItem */ }
public abstract class OrderPromotion    : PromotionData { /* DiscountType.Order */ }
public abstract class ShippingPromotion : PromotionData { /* DiscountType.Shipping */ }
```

### Engine

```csharp
public interface IPromotionEngine
{
    IEnumerable<RewardDescription> Run(IOrderGroup orderGroup);
    IEnumerable<RewardDescription> Run(IOrderGroup orderGroup, PromotionEngineSettings settings);
}
```

Default `PromotionEngine` pipeline:

1. Load all promotions → filter inactive → sort by priority.
2. Filter by required coupons (missing coupon → skip).
3. Per promotion:
   - Check redemption limits.
   - Resolve `PromotionProcessorBase<T>` and `Evaluate`.
   - Apply redemptions up to limit.
4. Return list of `RewardDescription`.

```csharp
public class RewardDescription
{
    public RewardType RewardType { get; }
    public decimal UnitDiscount { get; }
    public decimal Percentage { get; }
    public decimal SavedAmount { get; internal set; }
    public FulfillmentStatus Status { get; }
    public IEnumerable<RedemptionDescription> Redemptions { get; }
    public PromotionData Promotion { get; }
    public string Description { get; set; }
}
```

### Custom Promotion Pattern

```csharp
[ContentType(GUID = "7660BDC2-6F65-4D22-8FDB-812EE7F4074D")]
public class SpendAmountGetDiscount : OrderPromotion
{
    public virtual IList<Money> Amounts { get; set; }          // UI: nice multi-currency widget
    public virtual decimal Discount { get; set; }
    public virtual decimal AlmostFulfilledThreshold { get; set; }
}

public class SpendAmountGetDiscountProcessor
    : PromotionProcessorBase<SpendAmountGetDiscount>
{
    public override RewardDescription Evaluate(
        SpendAmountGetDiscount promo, PromotionProcessorContext context)
    {
        var form = context.OrderForm;
        var threshold = promo.Amounts.FirstOrDefault(m => m.Currency == context.OrderGroup.Currency);
        var subTotal = form.Shipments.SelectMany(s => s.LineItems)
            .Sum(i => i.PlacedPrice * i.Quantity - i.LineItemDiscountAmount - i.OrderLevelDiscountAmount);

        if (threshold.Amount <= subTotal)
        {
            var redemptions = new List<RedemptionDescription>
            {
                new RedemptionDescription(new AffectedOrder(form))
            };
            return RewardDescription.CreatePercentageReward(
                FulfillmentStatus.Fulfilled, redemptions, promo, promo.Discount,
                "The promotion has been fulfilled");
        }
        return RewardDescription.CreateNotFulfilledDescription(promo, FulfillmentStatus.NotFulfilled);
    }
}
```

`IList<Money>` and `IList<ContentReference>` are specially rendered by Marketing UI.

### `PromotionProcessorContext`

```csharp
public class PromotionProcessorContext
{
    public IOrderGroup OrderGroup { get; }
    public IOrderForm OrderForm { get; }
    public decimal CurrentOrderFormSubTotal { get; }   // before this promo, after earlier ones
}
```

### `FulfillmentStatus`

```csharp
[Flags]
public enum FulfillmentStatus
{
    NotFulfilled              = 1 << 0,
    PartiallyFulfilled        = 1 << 1,
    Fulfilled                 = 1 << 2,
    CouponCodeRequired        = 1 << 8,
    Excluded                  = 1 << 9,
    VisitorGroupRequired      = 1 << 10,
    RedemptionLimitReached    = 1 << 11,
    InvalidCoupon             = NotFulfilled | CouponCodeRequired,
    InvalidCombination        = NotFulfilled | Excluded,
    MissingVisitorGroup       = NotFulfilled | VisitorGroupRequired,
    NoRedemptionRemaining     = NotFulfilled | RedemptionLimitReached,
}
```

Processors should only return `NotFulfilled` / `PartiallyFulfilled` / `Fulfilled`.

### "Spend more" UX pattern

Read only partial hits without applying the reward:

```csharp
var rewards = _promotionEngine.Run(cart, new PromotionEngineSettings
{
    RequestedStatuses = RequestFulfillmentStatus.PartiallyFulfilled,
    ApplyReward = false
});
var almost = rewards.Where(r => r.Status == FulfillmentStatus.PartiallyFulfilled);
```

### Exclusion

- `ExcludedItems`: **other promotions/campaigns** (not catalog items!).
  - `CampaignRoot` reference → excludes ALL other promotions.
  - `SalesCampaign` reference → excludes all in that campaign.
  - Specific `PromotionData` → excludes that one.
  - Higher priority wins.
- `ExcludedCatalogItems`: catalog content (per promotion). Catalog, node, or entry references.
- **`EntryFilterSettings`** (global, code-only, runtime, no DB save) to exclude types/entries across all promotions:

```csharp
entryFilterSettings.AddFilter<GiftCardContent>(x => false);
entryFilterSettings.AddFilter<EntryContentBase>(x => !(bool)(x["IsGiftCard"] ?? false));
// Clear via ClearFilters() — no RemoveFilter method
```

### Exclusion Levels

- Default **per order**: excluded promotions can't both apply to same order.
- Switch to **per unit** (same line item can't receive both) via `appSettings`:

```
<!-- Commerce ≤ 13 -->
<add key="PromotionExclusionLevel" value="Unit" />
<!-- Commerce 14 configuration key -->
Episerver__Commerce__Promotion__PromotionExclusionLevel=Unit
```

### Gotchas

- Promotion contents do **not** support localizations; localize via embedded blocks.
- Caching campaigns/promotions is hard — expiry/activation fires no events. Short TTL (30 s–1 min) is acceptable.
- `Schedule.CampaignLink` decides "inherit campaign dates vs own dates" via `UseCampaignDate`.
- Localization files for promotion UI naming — embed as EmbeddedResource:

```xml
<?xml version="1.0" encoding="utf-8" standalone="yes"?>
<languages>
  <language name="English" id="en">
    <contenttypes>
      <spendamountgetdiscount>
        <name>Spend Amount and Get Discount</name>
        <description>Spend at least amount in cart, get a discount.</description>
        <properties>
          <amount><caption>Spend this amount</caption></amount>
          <discount><caption>Get a discount</caption></discount>
        </properties>
      </spendamountgetdiscount>
    </contenttypes>
  </language>
</languages>
```

---

## Part 7: Business Foundation

### Concepts

Business Foundation (BF) is a data-modeling system — essentially an ORM — to define custom entities. Used by `Contacts`, `Organizations`. Commerce 14 dropped the BF UI from Commerce Manager, then restored it in a modern form.

### Key Types

- `BusinessManager` (static) — `Create`, `Update`, `Delete`, `List`. All methods ultimately call `Execute(Request request)`.
- `EntityObject`, `ContactEntity`, `CustomerContact : ContactEntity`.
- `CustomerContext` — convenience access to contacts (no "find by email" — write your own).

```csharp
public class CustomerContact : ContactEntity { /* ... */ }
```

Find by email:

```csharp
try
{
    var filter = new FilterElement("Email", FilterElementType.Equal, email);
    return BusinessManager
        .List(ContactEntity.ClassName, new[] { filter })
        .OfType<CustomerContact>()
        .FirstOrDefault();
}
catch (ObjectNotFoundException) { return null; }
```

### Performance / Gotchas

- 40-column limit per BF table; overflow goes to `_ex0`, `_ex1`, … — custom SQL gets messy.
- `BusinessManager.List` caps at 1000 by default; change via `MetaObjectOptions.MaxListObject` — but paging support is patchy.
- Add non-clustered indexes on columns you filter on (e.g. personnummer, SSN-like IDs).
- `Contact` has a `Password` field — legacy; never use for authentication (use `AspNetIdentity`).

---

## Part 8: Extending the System

## Plug Your Implementations — The Framework Way

Two initialization modules you care about:

- `Mediachase.Commerce.Initialization.CommerceInitialization` — registers core services.
- `EPiServer.Commerce.Initialization.InitializationModule` (depends on the above) — registers catalog content and routing.

Pattern — override the default `ICurrentMarket`:

```csharp
[ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
public class InitializationModule : IConfigurableModule
{
    public void Initialize(InitializationEngine context) { /* event registration */ }
    public void Uninitialize(InitializationEngine context) { /* unregister */ }

    public void ConfigureContainer(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<ICurrentMarket, MarketStorage>();
    }
}
```

Attribute form + factory member:

```csharp
[ServiceConfiguration(typeof(ApplicationContext), Lifecycle = ServiceInstanceScope.Singleton)]
[ServiceConfiguration(typeof(IApplicationContext), FactoryMember = "Instance")]
public class ApplicationContext : IApplicationContext { }
```

### Gotchas

- Order of two modules depending on the same parent is **undefined**. If you need certainty, chain dependencies.
- "Later registration wins" — to override, make sure your module runs after the default one.

## The Unusual Ways

### Customize Order Processing via `ecf.order.config`

Swap system metaclasses (`PurchaseOrder`, `Cart`, `PaymentPlan`, `OrderForm`, `LineItem`) for your own:

```csharp
public class MyPurchaseOrder : PurchaseOrder
{
    public override void AcceptChanges()
    {
        // pre-save hook
        base.AcceptChanges();
        // post-save hook
    }
}
```

```xml
<!-- Configs/ecf.order.config -->
<MappedTypes>
  <PurchaseOrderType name="MySite.Order.Classes.MyPurchaseOrder, MySite" />
</MappedTypes>
```

Warning: static helpers like `PurchaseOrder.LoadByCustomer(guid)` only return the base class if your persistence differs.

### Workflows → ActivityFlows (Commerce 9+)

`Mediachase.Commerce.Workflow` is **open-source**. Commerce 9 removed the WF 3.5 dependency — workflows are now plain C# classes.

```csharp
[ActivityFlowConfiguration(Name = OrderGroupWorkflowManager.CartCheckOutWorkflowName)]
public class CartCheckoutActivityFlow : ActivityFlow
{
    public override ActivityFlowRunner Configure(ActivityFlowRunner activityFlow)
    {
        return activityFlow
            .If(() => ShouldProcessPayment())
                .Do<ProcessPaymentActivity>()
            .EndIf()
            .Do<CalculateTotalsActivity>()
            .Do<AdjustInventoryActivity>()
            .Do<RecordPromotionUsageActivity>();
    }
}
```

Register an override with higher `Priority`:

```csharp
[ActivityFlowConfiguration(Name = OrderGroupWorkflowManager.CartCheckOutWorkflowName, Priority = 10)]
public class MyCartCheckoutActivityFlow : ActivityFlow
{
    public override ActivityFlowRunner Configure(ActivityFlowRunner af) => af.Do<MyActivity>();
}
```

Key activities you'll touch:

- `ValidateLineItemsActivity`
- `GetFulfillmentWarehouseActivity`
- `CheckInstoreInventoryActivity` / `CheckInventoryActivity`
- `AdjustInventoryActivity`
- `ProcessPaymentActivity`
- `ProcessShipmentsActivity`
- `RemoveDiscountsActivity`
- `CalculateTotalsActivity`, `CalculateDiscountsActivity`, `CalculateTaxActivity`
- `CalculatePurchaseOrderStatusActivity`
- `RecordPromotionUsageActivity`

For downloadable goods you may want to short-circuit inventory checks.

### `AvailableInBetaMode`

Workflows decorated with this attribute only run when BETA features are enabled (`VNextFeatures`). Likely to be removed in upcoming releases.

## ProfileModule

Commerce ships an `IHttpModule` for login-time behavior: `EPiServer.Business.Commerce.HttpModules.ProfileModule` — assigns the last anonymous order to the user profile, merges carts on login.

Default `<modules>`:

```xml
<modules runAllManagedModulesForAllRequests="true">
  <add name="BusinessFoundationInitializeModule"
       type="Mediachase.Commerce.Core.Modules.BusinessFoundationInitializeModule, Mediachase.Commerce"
       preCondition="managedHandler" />
  <add name="InitializationModule"
       type="EPiServer.Framework.Initialization.InitializationModule, EPiServer.Framework"
       preCondition="managedHandler" />
  <add name="UrlRewriteModule"
       type="EPiServer.Web.RoutingUrlRewriteModule, EPiServer"
       preCondition="managedHandler" />
  <add name="ShellRoutingModule"
       type="EPiServer.Shell.Web.Routing.ShellRoutingModule, EPiServer.Shell" />
  <remove name="WebDAVModule" />
  <add name="ProfileModule"
       type="EPiServer.Business.Commerce.HttpModules.ProfileModule, EPiServer.Business.Commerce" />
  <add name="CatalogNodeModule"
       type="EPiServer.Business.Commerce.HttpModules.CatalogNodeModule, EPiServer.Business.Commerce" />
</modules>
```

Replace `ProfileModule` with your own `IHttpModule` implementation to customize login-time cart/PO merging. `CatalogNodeModule` is obsolete and removable.

## Migration Steps

Commerce's internal one-time migration mechanism. Lives in `EPiserver.Commerce.Internal.Migration`. Runs on site startup before serving.

```csharp
public interface IMigrationStep
{
    int Order { get; }
    string Name { get; }
    bool Execute(IProgressMessenger processMessenger);
    string Description { get; }
}
```

- Low `Order` reserved for built-in steps — pick higher numbers.
- Store run history in DB; step returning `true` is marked complete.
- View/run at `/EPiServer/Commerce/Migration` (requires `EPiServer.Commerce.UI`).
- Enable auto-run via `<add key="AutoMigrateEPiServer" value="true" />` or:

```csharp
context.Locate.Advanced.GetInstance<MigrationSettings>().AutoMigrate = true;
```

- Can't run from Commerce Manager context; run on front-end site first.

---

## Part 9: Advanced Stuffs

## Caching

### Best Practices

- Use **`ISynchronizedObjectInstanceCache`**, not `IObjectInstanceCache`. The synced one handles multi-instance invalidation (Azure Service Bus / UDP).
- Invalidation is only guaranteed to be synchronous across instances — not the cache insertion itself. Each instance will reload on demand.
- Keep cache keys short but unique. Long keys stress CPU on key matching.
- Don't over-cache. Commerce already caches:
  - Catalog content (by `ContentProvider`) — unless loaded with `ContentReference.GetPublishedOrLatest = true`.
  - Carts.
  - DTOs.
  - `IPriceValue`s.

### Master-Key Pattern

```
EP:EC:Catalog                  // prefix
EP:EC:Catalog:*                // master key
EP:EC:Catalog:<id>             // individual entries

// Invalidate all catalog cache
cache.Remove("EP:EC:Catalog:*");
```

### Overload Pitfall

- Wrong overload = dependency key that doesn't exist = cache item is **invalidated on insert** (not cached at all). Compiles, runs, just ... doesn't cache. Be very careful which `Insert` overload you use for master vs dependency keys.

## Routing

### Two Systems

1. **SEO URI** (Mediachase legacy) — single-segment URIs per content, stored in `CatalogItemSeo`. Router: `SeoUriRouter` (`ISimpleAddressRouteHandler`).
2. **HierarchicalCatalogPartialRouter** (Commerce 7.5+, partial router) — path-per-segment reflecting the catalog tree.

### SEO URI Generation — `UniqueSeoGenerator`

```csharp
public class UniqueSeoGenerator
{
    protected virtual string UriExtension { get; }   // default ".aspx"
    public virtual string GenerateSeoUri(string name, string languageCode, bool includeRandomToken);
    public virtual string GenerateUriSegment(string name, bool includeRandomToken);   // for hierarchical
    protected virtual string GetRandomToken();
}
```

Override and re-register. Typical: drop `.aspx`.

### Hierarchical Routing

`HierarchicalCatalogPartialRouter : ICommerceRouter : IPartialRouter<TContent, TRoutedData>`.

`IPartialRouter<TContent, TRoutedData>`:

```csharp
PartialRouteData GetPartialVirtualPath(
    TRoutedData content, string language,
    RouteValueDictionary routeValues, RequestContext requestContext);

object RoutePartial(TContent content, SegmentContext segmentContext);
```

Register a custom root:

```csharp
var commerceRoot = contentLoader
    .GetChildren<CatalogContent>(referenceConverter.GetRootLink()).First();

RouteTable.Routes.RegisterPartialRouter(new HierarchicalCatalogPartialRouter(
    () => ContentReference.StartPage, commerceRoot, enableOutgoingSeoUri: false));
```

Or use the helper:

```csharp
CatalogRouteHelper.MapDefaultHierarchialRouter(RouteTable.Routes, enableOutgoingSeoUri: true);
CatalogRouteHelper.SetupSeoUriPermanentRedirect();  // redirect old hierarchical → new SEO URI
```

Parameters:

- `RouteStartingPoint`: `Func<ContentReference>` — usually `() => ContentReference.StartPage` or `SiteDefinition.StartPage`; evaluated every request.
- `CommerceRoot`: the `CatalogContentBase` from which URL matching starts. Point at a specific catalog to hide catalog segment. Point at a node to hide node segment.
- `enableOutgoingSeoUri`: if true, outgoing URL generation uses SEO URIs even under hierarchical router.

### SEO URI → Hierarchical Redirect

No built-in helper. Hook `ContentRoute.RoutedContent`:

```csharp
ContentRoute.RoutedContent += Routed_SeoUri;

private static void Routed_SeoUri(object sender, RoutingEventArgs e)
{
    var ctx = e.RoutingSegmentContext;
    if (!(ctx.RoutedObject is CatalogContentBase)) return;
    if (string.IsNullOrEmpty(ctx.LastConsumedFragment)) return;   // hierarchical router consumed it

    var urlResolver = ServiceLocator.Current.GetInstance<UrlResolver>();
    ctx.PermanentRedirect(urlResolver.GetUrl(ctx.RoutedContentLink));
}
```

- `HierarchicalCatalogPartialRouter` consumes all segments (so `LastConsumedFragment` ends empty).
- `SeoUriRouter` doesn't process segments — `LastConsumedFragment` stays intact. That distinguishes them.

### Gotchas

- Changing SEO URIs loses SEO rank and breaks inbound links. Use Geta 404 handler (https://github.com/Geta/404handler) if you must change.
- Default extension `.aspx` is outdated — override via `UniqueSeoGenerator.UriExtension`.
- `RouteSegment` is stored on `CatalogItemSeo.UriSegment` (for nodes/entries) and `CatalogLanguage` (for catalogs).

## Settings

No central document. Advice:

- **Don't touch cache settings** unless you know what you're doing.
- Investigate `appSettings` keys case-by-case (Commerce 14 uses `Episerver__Commerce__...` double-underscore pattern).
- `AutoMigrateEPiServer` — auto-run migrations at startup.
- `SimplifiedCatalogListingThreshold` — threshold for Catalog UI flat view.
- `DisableVersionSync` — suppress version creation on `ICatalogSystem` updates.
- `uiMaxVersions` / `UIMaxVersions` — cap version history (affects CMS too).
- `PromotionExclusionLevel` — `Unit` vs `Order`.
- `MetaObjectOptions.MaxListObject` — raise BF `BusinessManager.List` cap.

---

## Appendix: How to Ensure a Successful Upgrade

- **Back up** DB and site before upgrading.
- Upgrade in **small steps** — one major at a time (7.5 counts as a major).
- Upgrade **front-end site first**; DB changes and migration steps run there.
- Watch migration steps; don't skip them (hard-gated since Commerce 8.16).
- Increase DB connection timeout for heavy migration data work.
- Pre-Commerce 9 catalog data: consider **export catalogs, upgrade, import** (catalog import/export is more robust than some upgrade scripts).
- Commerce Manager upgrade: often easier to spin up a fresh CM matching the front-end version and point at the upgraded DB.
- Always run the latest version your license allows — newer = fewer bugs, faster fixes, better perf, easier support.

---

## Quick Reference — Namespace & Assembly Index

| Area | Namespace | Assembly (common) |
|---|---|---|
| Catalog content types | `EPiServer.Commerce.Catalog.ContentTypes` | EPiServer.Commerce.Core |
| Catalog services | `EPiServer.Commerce.Catalog` | EPiServer.Commerce.Core |
| Legacy catalog | `Mediachase.Commerce.Catalog` | Mediachase.Commerce |
| Reference conversion | `Mediachase.Commerce` (`ReferenceConverter`) | Mediachase.Commerce |
| Market | `Mediachase.Commerce` (`IMarket`, `IMarketService`, `MarketId`, `ICurrentMarket`) | Mediachase.Commerce |
| Pricing | `Mediachase.Commerce.Pricing`, `EPiServer.Commerce.Pricing` | EPiServer.Commerce.Core |
| Inventory | `Mediachase.Commerce.Inventory`, `EPiServer.Commerce.Inventory` | EPiServer.Commerce.Core |
| Orders (new) | `EPiServer.Commerce.Order` | EPiServer.Commerce.Order |
| Orders (legacy) | `Mediachase.Commerce.Orders`, `Mediachase.Commerce.Orders.Managers` | Mediachase.Commerce |
| Workflows | `Mediachase.Commerce.Workflow`, `...Activities`, `...Activities.Cart`, `...Activities.PurchaseOrderActivities` | Mediachase.Commerce.Workflow |
| Promotions | `EPiServer.Commerce.Marketing` | EPiServer.Commerce.Marketing |
| Search providers | `Mediachase.Search`, `Mediachase.Search.Providers.Lucene`, `...Solr35`, `EPiServer.Commerce.FindSearchProvider` | Mediachase.Search, Mediachase.Search.LuceneSearchProvider, etc. |
| Find Commerce | `EPiServer.Find.Commerce` | EPiServer.Find.Commerce |
| BF / Contacts | `Mediachase.BusinessFoundation`, `Mediachase.Commerce.Customers` | Mediachase.BusinessFoundation, Mediachase.Commerce |
| Initialization | `EPiServer.Framework.Initialization`, `EPiServer.ServiceLocation`, `EPiServer.Commerce.Initialization`, `Mediachase.Commerce.Initialization` | EPiServer.Framework, EPiServer.Commerce.Core, Mediachase.Commerce |
| Routing | `EPiServer.Web.Routing`, `EPiServer.Commerce.Routing`, `Mediachase.Commerce.Catalog.Routing` | EPiServer, EPiServer.Commerce.Core |

---

## Cross-Cutting Advice for This Project (Commerce 14 / .NET 8)

- **Prefer `IContentRepository`, `IOrderRepository`, `IRelationRepository`, `IAssociationRepository`, `IInventoryService`, `IPriceService`, `IPriceDetailService`, `IMarketService`, `ICurrentMarket`, `IOrderSearchService`, `IOrderEvents`, `IContentEvents`** over their legacy counterparts (`ICatalogSystem`, `OrderContext`, `CartHelper`, `IWarehouseInventoryService`).
- **Batch over loops**: `IContentRepository.GetItems`, `IContentRepository.Publish`, `IInventoryService.QueryByEntry(codes)`, `IPriceService.GetPrices(IEnumerable<CatalogKey>)`.
- **Never call `IInventoryService.List()`** in production.
- **Serializable carts**: watch cart size; avoid per-action saves in B2B scenarios.
- **Find.Commerce conventions**: override in an `IInitializableModule` with `[ModuleDependency(typeof(FindCommerceInitializationModule))]` to reshape the index (exclude inventories from triggering reindex if churn is high).
- **Promotions**: always implement `PromotionProcessorBase<T>` for custom types. Use `AffectedOrder` / `AffectedShipment` / `AffectedLineItem` redemption targets per discount type.
- **Markets**: always start every price/inventory/order read-or-write with an explicit `MarketId`. Resolve via injected `ICurrentMarket` (never resolve from `HttpContext` ad hoc).
- **Routing**: prefer hierarchical router for SEO depth; migrate existing SEO-URI sites gradually using the `RoutedContent` redirect hook.
- **DI scopes**: use `Singleton` for stateless services, `Scoped` for per-request, `Transient` when state holds per-call context. Mis-scoping `ICurrentMarket` at `Singleton` with cookie-based switching is a classic bug.
- **Events**: for IOrder-based code, register `IOrderEvents` (not `OrderContext.OrderGroupUpdated`) in an `IInitializationModule`; deregister in `Uninitialize`.
- **Catalog content versions**: always publish master language first; non-culture-specific properties are read from master.
- **Caching**: stick to `ISynchronizedObjectInstanceCache` with master-key pattern; don't cache promotions longer than a minute.
- **Migrations**: enable `AutoMigrateEPiServer`; include `EPiServer.Commerce.UI` on at least one instance to view/debug migration state.
