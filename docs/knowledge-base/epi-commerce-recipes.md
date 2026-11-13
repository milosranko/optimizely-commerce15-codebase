# EPiServer Commerce Recipes — Knowledge Base

## Introduction

- **Book:** *Episerver Commerce: A problem – solution approach — Solving problems, one at a time*
- **Author:** Quan Mai (Episerver employee; views are his own, not Episerver's)
- **Copyright:** © 2014 – 2019 Quan Mai
- **Publisher:** Leanpub (`http://leanpub.com/epicommercerecipes`), published 2019-02-23
- **Target platform:** **Episerver Commerce 11** (pre-Optimizely rebrand era). Some recipes reference Commerce 10.x / 11.2 / 11.4 / 11.6 / 12.x features where relevant. Toolchain: Visual Studio 2015+, SQL Server 2012+, Quicksilver reference site.

### What this book covers
A cookbook of practical recipes for extending Episerver Commerce — catalog routing, pricing/inventory, order/cart abstractions, promotions, search (built-in provider and Find), customer/organization handling, framework-level DI/interception, plus limited Commerce Manager UI extensions.

### Who it is for
.NET developers already familiar with C# and Episerver basics who want targeted, real-world solutions. Three difficulty tiers are used throughout: **Beginner / Immediate / Advanced**.

### Important: version scope disclaimer
This book targets **classic Episerver Commerce 11 (.NET Framework, WebForms-based Commerce Manager, legacy DTO APIs, structuremap IoC)**. It predates the Optimizely rebrand (2021) and predates the modern .NET 5+/Optimizely Commerce 14 stack used by current projects. Many concepts still apply (ReferenceConverter, IContentLoader, IOrderRepository, IPromotionEngine, CatalogContentClientConventions, interceptors), but APIs, namespaces, IoC container, and Commerce Manager have substantially changed. Treat snippets as *conceptual guidance*, not drop-in code for Optimizely 12/14.

---

## Table of Contents

- [Chapter 0: Preparation](#chapter-0-preparation)
  - [0.0 Get an Episerver Commerce site up and running](#00-get-an-episerver-commerce-site-up-and-running)
  - [0.1 Dependency injection](#01-dependency-injection)
  - [0.2 Register dependency](#02-register-dependency)
  - [0.3 The dependency lifecycle](#03-the-dependency-lifecycle)
- [Chapter 1: Catalog System Recipes](#chapter-1-catalog-system-recipes)
  - [1.0 Identify a catalog content](#10-identify-a-catalog-content)
  - [1.1 Get all catalogs](#11-get-all-catalogs)
  - [1.2 Traverse the catalog](#12-traverse-the-catalog)
  - [1.2.2 Find unpublished variants](#122-find-unpublished-variants)
  - [1.3 Read-only Catalog UI](#13-read-only-catalog-ui)
  - [1.4 Choose a URL style](#14-choose-a-url-style)
  - [1.4.1 Switch between URL styles](#141-switch-between-url-styles)
  - [1.5 Remove the catalog name from product URL](#15-remove-the-catalog-name-from-product-url)
  - [1.6 Multiple catalogs with same URL](#16-multiple-catalogs-with-same-url)
  - [1.6.2 Building the outgoing URLs](#162-building-the-outgoing-urls)
  - [1.6.3 Beyond multiple sites](#163-beyond-multiple-sites)
  - [1.6.4 URL without catalog name in multiple catalogs scenario](#164-url-without-catalog-name-in-multiple-catalogs-scenario)
  - [1.6.5 URL without categories](#165-url-without-categories)
  - [1.7 Get products belong to a specific market](#17-get-products-belong-to-a-specific-market)
  - [1.8 Move catalog items around](#18-move-catalog-items-around)
  - [1.8.2 Set the primary node](#182-set-the-primary-node)
  - [1.8.3 Adjust sort order](#183-adjust-sort-order)
  - [1.9 Build a breadcrumb](#19-build-a-breadcrumb)
  - [1.9.2 Breadcrumb: multiple categories scenario](#192-breadcrumb-multiple-categories-scenario)
- [Chapter 2: Inventories and Prices](#chapter-2-inventories-and-prices)
  - [2.0 Update a price](#20-update-a-price)
  - [2.0.1 Update prices (bulk)](#201-update-prices-bulk)
  - [2.1 Optimize prices, your own way](#21-optimize-prices-your-own-way)
  - [2.2 Store the MSRP](#22-store-the-msrp)
  - [2.2.2 Edit the price type](#222-edit-the-price-type)
  - [2.2.3 Show previous prices](#223-show-previous-prices)
  - [2.3 Change prices globally](#23-change-prices-globally)
  - [2.4 Handle obsoleted prices](#24-handle-obsoleted-prices)
  - [2.5 Split shipments on inventory status](#25-split-shipments-on-inventory-status)
  - [2.5.2 Copying line item properties](#252-copying-line-item-properties)
  - [2.6 Update inventory, the safe way](#26-update-inventory-the-safe-way)
  - [2.7 Select fulfillment warehouse](#27-select-fulfillment-warehouse)
  - [2.8 Cancel inventory reservation](#28-cancel-inventory-reservation)
  - [2.8.2 Cancel one line item](#282-cancel-one-line-item)
- [Chapter 3: Order](#chapter-3-order)
  - [3.1 Find abandoned carts](#31-find-abandoned-carts)
  - [3.1.2 Searching for serializable abandoned carts](#312-searching-for-serializable-abandoned-carts)
  - [3.2 Add meta field to cart](#32-add-meta-field-to-cart)
  - [3.2.2 Adding meta field to cart via code](#322-adding-meta-field-to-cart-via-code)
  - [3.2.3 Performance considerations](#323-performance-considerations)
  - [3.3 Add items to cart](#33-add-items-to-cart)
  - [3.3.2 Add a bundle to cart](#332-add-a-bundle-to-cart)
  - [3.3.3 Remove an item from cart](#333-remove-an-item-from-cart)
  - [3.4 Shipment-cost-based shipping cost](#34-shipment-cost-based-shipping-cost)
  - [3.4.2 Configure a shipping method](#342-configure-a-shipping-method)
  - [3.5 Migrate carts when users log in](#35-migrate-carts-when-users-log-in)
  - [3.6 Customize tax calculation](#36-customize-tax-calculation)
- [Chapter 4: Events](#chapter-4-events)
  - [4.1 Reindex entries when there are changes](#41-reindex-entries-when-there-are-changes)
  - [4.1.2 React to prices and inventories events](#412-react-to-prices-and-inventories-events)
  - [4.1.3 Remove entries from index](#413-remove-entries-from-index)
  - [4.2 Incremental indexing without events](#42-incremental-indexing-without-events)
  - [4.3 Update when there are updates](#43-update-when-there-are-updates)
  - [4.3.2 A different approach for listening to events](#432-a-different-approach-for-listening-to-events)
  - [4.4 Reindex when stock reaches a threshold](#44-reindex-when-stock-reaches-a-threshold)
- [Chapter 5: Searching](#chapter-5-searching)
  - [5.1 Searching for entries](#51-searching-for-entries)
  - [5.2 Building a facet](#52-building-a-facet)
  - [5.3 Add additional fields to the index](#53-add-additional-fields-to-the-index)
  - [5.3.2 Add additional fields to Find index](#532-add-additional-fields-to-find-index)
  - [5.4 Fast category pages](#54-fast-category-pages)
  - [5.5 Skip indexing certain entries](#55-skip-indexing-certain-entries)
  - [5.5.2 Better partial indexing](#552-better-partial-indexing)
  - [5.5.3 Skip indexing content in Find](#553-skip-indexing-content-in-find)
  - [5.5.4 Hide entries from Find searches](#554-hide-entries-from-find-searches)
- [Chapter 6: Promotions](#chapter-6-promotions)
  - [6.0 Get all promotions](#60-get-all-promotions)
  - [6.1 How to add coupon code](#61-how-to-add-coupon-code)
  - [6.2 Change prices globally (promotion)](#62-change-prices-globally-promotion)
  - [6.3 Multiple coupons per promotion](#63-multiple-coupons-per-promotion)
  - [6.3.2 Multiple coupons for built-in promotions](#632-multiple-coupons-for-built-in-promotions)
  - [6.3.3 Comprehensive coupon applied status](#633-comprehensive-coupon-applied-status)
  - [6.3.4 Cache valid coupon codes](#634-cache-valid-coupon-codes)
  - [6.4 Get coupon code statistics](#64-get-coupon-code-statistics)
  - [6.5 Product listing with discounted prices](#65-product-listing-with-discounted-prices)
  - [6.6 Predict the rate of applicable orders](#66-predict-the-rate-of-applicable-orders)
  - [6.7 Multiple markets campaign](#67-multiple-markets-campaign)
  - [6.7.2 Process multiple markets campaign](#672-process-multiple-markets-campaign)
  - [6.7.3 Process shipping methods for multiple markets](#673-process-shipping-methods-for-multiple-markets)
  - [6.8 Promotions by visitor groups](#68-promotions-by-visitor-groups)
- [Chapter 7: Customer Recipes](#chapter-7-customer-recipes)
  - [7.1 Add new fields to the contact](#71-add-new-fields-to-the-contact)
  - [7.1.2 Ensure contact meta fields](#712-ensure-contact-meta-fields)
  - [7.2 Change the contact select screen](#72-change-the-contact-select-screen)
  - [7.3 Promotion for loyal customers](#73-promotion-for-loyal-customers)
  - [7.4 Shared cart within organization](#74-shared-cart-within-organization)
- [Chapter 8: Framework](#chapter-8-framework)
  - [8.1 Extend the default behavior (interceptors)](#81-extend-the-default-behavior-interceptors)
  - [8.1.2 The partially overridden IProfileMigrator](#812-the-partially-overridden-iprofilemigrator)
  - [8.2 Shared singleton across multiple registrations](#82-shared-singleton-across-multiple-registrations)
- [Chapter 9: Mixes](#chapter-9-mixes)
  - [9.1 Add items to indexing queue](#91-add-items-to-indexing-queue)
  - [9.2 Migrate promotions to new campaign type (IMigrationStep)](#92-migrate-promotions-to-new-campaign-type-imigrationstep)
- [Chapter 10: Extending Commerce Manager](#chapter-10-extending-commerce-manager)
  - [10.1 Add wish-list management](#101-add-wish-list-management)
  - [10.2 Add organization to order list](#102-add-organization-to-order-list)
  - [10.2.1 Organization column, the alternative way](#1021-organization-column-the-alternative-way)

---

## Chapter 0: Preparation

Background: EPiServer AB rebranded to "Episerver" in 2015 after merging with Ektron. Namespaces/packages still use `EPiServer`.

### 0.0 Get an Episerver Commerce site up and running

**Goal:** Stand up a practice site. Two options:
1. **Episerver Visual Studio extension** — create empty CMS + Commerce Manager sites (no sample data).
2. **Quicksilver** (https://github.com/episerver/quicksilver) — reference MVC site with sample data. Requires SQL Server with **SQL Authentication mode enabled**. IIS recommended over IIS Express; run Visual Studio as Administrator to debug.

### 0.1 Dependency injection

Episerver is built on DI. Four ways to obtain an instance, ranked best to worst:
- **Constructor injection** (preferred)
- **Property injection** via `Injected<T>`
- **`ServiceLocator`** (anti-pattern; use sparingly)
- `new` (only for simple value objects)

The IoC container historically used is **StructureMap**.

### 0.2 Register dependency

Two registration styles — attribute and `IConfigurableModule`.

```csharp
[ServiceConfiguration(typeof(IMyNewInterface))]
public class MyNewClass : IMyNewInterface { }
```

Key rules:
- `[ServiceConfiguration]` is only needed when mapping **interface/base class → implementation**.
- When multiple implementations exist, last-registered wins — **order is not guaranteed with attributes**. Use `IConfigurableModule` for deterministic registration.
- Override default implementations registered via attribute by using an `IConfigurableModule`.
- If the default was registered in `IConfigurableModule`, force order via `[ModuleDependency]`:

```csharp
[ModuleDependency(typeof(Mediachase.Commerce.Initialization.CommerceInitialization))]
[ModuleDependency(typeof(DataInitialization))]
[InitializableModule]
public class MyInitializationModule : IConfigurableModule { }
```

Rule of thumb: depend on `EPiServer.Commerce.Initialization.InitializationModule` to run after all Commerce defaults are wired up.

### 0.3 The dependency lifecycle

Four lifecycles (CMS 10+):
- **Per request / transient** (default) — new instance each request.
- **Per session** — one per HTTP session.
- **Per hybrid session/thread** — session if present, thread otherwise (useful in scheduled jobs).
- **Singleton** — one per application lifetime.

**Gotcha:** a singleton depending on a transient effectively promotes the transient to singleton via captured reference. Real-world horror story in book: a singleton holding *current user address* leaked private data between users. Never share user-scoped state in a singleton.

---

## Chapter 1: Catalog System Recipes

### 1.0 Identify a catalog content

Three identities for catalog content:
- **`ContentReference`** — `{Id}_{WorkId}_{ProviderName}`, e.g. `123_456_CatalogContent`. Unique within system only.
- **`Code`** — string, used by prices/inventories/line items. Entries + Nodes use Code; Catalog uses Name.
- **`Guid`** — globally unique, stable permanent-link identity.

Use `ReferenceConverter` to avoid loading whole contents just to get a code:

```csharp
var code = referenceConverter.GetCode(contentLink);
var contentLink = referenceConverter.GetContentLink(code);
```

`ReferenceConverter` has batch overloads — use them for performance.

### 1.1 Get all catalogs

All catalogs are direct children of a **virtual** `RootContent`. Access via `ReferenceConverter.GetRootLink()`:

```csharp
var catalogs = _contentLoader.GetChildren<CatalogContent>(
    _referenceConverter.GetRootLink(),
    new LoaderOptions { LanguageLoaderOption.MasterLanguage() });
```

**Gotcha:** the overload without `LoaderOptions` filters by current culture — catalogs only authored in another language will be missing. Always use `MasterLanguage()` fallback if you need them all.

### 1.2 Traverse the catalog

Two styles:

**Legacy DTO (`ICatalogSystem.FindItemsDto`)** — good for bulk edit/save workflows; supports `CatalogSearchOptions.Classes` to filter by MetaClass.

**Modern content APIs** — recursive batched load, avoids over-fetching and excludes linked products:

```csharp
public virtual IEnumerable<T> GetEntriesRecursive<T>(ContentReference parentLink, CultureInfo defaultCulture)
    where T : EntryContentBase
{
    foreach (var nodeContent in LoadChildrenBatched<NodeContent>(parentLink, defaultCulture))
    foreach (var entry in GetEntriesRecursive<T>(nodeContent.ContentLink, defaultCulture))
        yield return entry;

    foreach (var entry in LoadChildrenBatched<T>(parentLink, defaultCulture))
        yield return entry;
}

private IEnumerable<T> LoadChildrenBatched<T>(ContentReference parentLink, CultureInfo c) where T : IContent
{
    var start = 0;
    while (true)
    {
        var batch = _contentLoader.GetChildren<T>(parentLink, c, start, 50);
        if (!batch.Any()) yield break;
        foreach (var content in batch)
        {
            if (!parentLink.CompareToIgnoreWorkID(content.ParentLink)) continue; // skip links
            yield return content;
        }
        start += 50;
    }
}
```

Use batch size ~50 for content APIs (heavier than DTOs; DTOs can use 500+).

### 1.2.2 Find unpublished variants

Combined use case for `GetEntriesRecursive` and `IContentVersionRepository`:

```csharp
foreach (var variant in GetEntriesRecursive<VariationContent>(catalog.ContentLink, catalog.MasterLanguage))
{
    var versions = _contentVersionRepository.List(variant.ContentLink.ToReferenceWithoutVersion());
    var missingLanguages = versions
        .GroupBy(v => v.LanguageBranch)
        .Where(g => g.All(v => v.Status != VersionStatus.Published))
        .Select(g => g.Key);

    if (missingLanguages.Any())
        _log.Information($"Variant {variant.Code} unpublished in {string.Join(",", missingLanguages)}");
}
```

### 1.3 Read-only Catalog UI

Subclass `CatalogContentProvider` and drop write capabilities:

```csharp
public class ReadonlyCatalogContentProvider : CatalogContentProvider
{
    public override ContentProviderCapabilities ProviderCapabilities
        => ContentProviderCapabilities.MultiLanguage;
}
```

Register it to override the default. Does *not* cover prices/inventories (separate systems). From Commerce 11.6 onward, prefer the new per-content permission system.

### 1.4 Choose a URL style

Two styles:
- Hierarchical: `/Catalog/Parent-Node/Child-Node/Product`
- SEO URI: `/product-name`

Default (hierarchical):

```csharp
CatalogRouteHelper.MapDefaultHierarchialRouter(RouteTable.Routes, false);
```

Switch to SEO URIs + permanent redirects:

```csharp
CatalogRouteHelper.MapDefaultHierarchialRouter(RouteTable.Routes, true);
CatalogRouteHelper.SetupSeoUriPermanentRedirect();
```

### 1.4.1 Switch between URL styles

To redirect **SEO URI → hierarchical** (opposite of built-in direction): hook `ContentRoute.RoutedContent`. Trick to detect which router matched: `HierarchicalCatalogPartialRouter` consumes every segment, leaving `LastConsumedFragment` empty; `SeoUriRouter` consumes nothing, leaving the full path.

```csharp
ContentRoute.RoutedContent += (sender, e) => {
    var ctx = e.RoutingSegmentContext;
    if (!(ctx.RoutedObject is CatalogContentBase)) return;
    if (string.IsNullOrEmpty(ctx.LastConsumedFragment)) return; // hierarchical already
    var urlResolver = ServiceLocator.Current.GetInstance<UrlResolver>();
    ctx.PermanentRedirect(urlResolver.GetUrl(ctx.RoutedContentLink));
};
```

### 1.5 Remove the catalog name from product URL

Bypass `CatalogRouteHelper` and register `HierarchicalCatalogPartialRouter` directly, passing the first `CatalogContent` (instead of `RootContent`) as `commerceRoot`:

```csharp
var commerceRootContent = contentLoader
    .GetChildren<CatalogContent>(referenceConverter.GetRootLink())
    .FirstOrDefault();

Func<ContentReference> startingPoint = () =>
    ContentReference.IsNullOrEmpty(SiteDefinition.Current.StartPage)
        ? SiteDefinition.Current.RootPage
        : SiteDefinition.Current.StartPage;

RouteTable.Routes.RegisterPartialRouter(
    new HierarchicalCatalogPartialRouter(startingPoint, commerceRootContent, false));
```

Works only for single-catalog scenarios.

### 1.6 Multiple catalogs with same URL

For multi-site setups where each site uses a different catalog but the same URL template. Override `FindNextContentInSegmentPair`:

```csharp
public class MultipleSiteCatalogPartialRouter : HierarchicalCatalogPartialRouter
{
    private readonly IContentLoader _contentLoader;
    public MultipleSiteCatalogPartialRouter(Func<ContentReference> s, CatalogContentBase root, bool seo, IContentLoader loader)
        : base(s, root, seo) { _contentLoader = loader; }

    protected override CatalogContentBase FindNextContentInSegmentPair(
        CatalogContentBase catalogContent, SegmentPair pair,
        SegmentContext segmentContext, CultureInfo culture)
    {
        if (catalogContent.ContentType == CatalogContentType.Root)
        {
            var link = _contentLoader.Get<StartPage>(RouteStartingPoint).CatalogLink;
            if (_contentLoader.TryGet<CatalogContent>(link, culture, out var defined))
                return defined;
        }
        return base.FindNextContentInSegmentPair(catalogContent, pair, segmentContext, culture);
    }
}
```

Pick catalog via a StartPage property:

```csharp
[AllowedTypes(typeof(CatalogContent))]
[UIHint(UIHint.CatalogContent)]
[Required]
public virtual ContentReference CatalogLink { get; set; }
```

`StartingPoint` is `Func<ContentReference>` so it resolves per current SiteDefinition.

### 1.6.2 Building the outgoing URLs

With catalog routing magic, outgoing URLs still use the real catalog's `RouteSegment`. Override `TryGetRouteSegment` to return a shared constant:

```csharp
protected override bool TryGetRouteSegment(ContentReference link, string lang, out string segment)
{
    if (_contentLoader.TryGet<CatalogContent>(link, CultureInfo.GetCultureInfo(lang), out _))
    {
        segment = "products";
        return true;
    }
    return base.TryGetRouteSegment(link, lang, out segment);
}
```

### 1.6.3 Beyond multiple sites

Scenario: one site, two landing URLs (`/private`, `/business`), each mapping to a different catalog subtree. Episerver doesn't officially support multiple route roots. Pragmatic workaround: model "private" and "business" as catalog **nodes** and render a CMS page for them via a `LandingNode` type with a `LandingPageLink` property:

```csharp
[CatalogContentType(GUID="...", MetaClassName="LandingNode", DisplayName="Landing Node")]
[AvailableContentTypes(Include = new[] { typeof(NodeContent) })]
public class LandingNode : NodeContent
{
    [AllowedTypes(typeof(PageData)), Required]
    public virtual ContentReference LandingPageLink { get; set; }
}

public class LandingNodeController : ContentController<LandingNode>
{
    public ActionResult Index(LandingNode currentContent) {
        var page = _contentLoader.Get<LandingPage>(currentContent.LandingPageLink);
        return View("~/Views/LandingPage/Index.cshtml", page);
    }
}
```

### 1.6.4 URL without catalog name in multiple catalogs scenario

Combine recipes: pick first-level node under the start-page-configured catalog based on URL segment, instead of returning the catalog itself:

```csharp
protected override CatalogContentBase FindNextContentInSegmentPair(...)
{
    if (catalogContent.ContentType == CatalogContentType.Root)
    {
        var definedCatalogLink = _contentLoader.Get<StartPage>(RouteStartingPoint).CatalogLink;
        if (_contentLoader.TryGet<CatalogContent>(definedCatalogLink, cultureInfo, out var cat))
        {
            var nodes = _contentLoader.GetChildren<NodeContent>(definedCatalogLink);
            return nodes.FirstOrDefault(n =>
                n.RouteSegment.Equals(segmentPair.Next, StringComparison.OrdinalIgnoreCase));
        }
    }
    return base.FindNextContentInSegmentPair(...);
}
```

### 1.6.5 URL without categories

Product URLs become `/products/{code}` regardless of catalog depth. Override both `TryGetRouteSegment` (outgoing) and `FindNextContentInSegmentPair` (incoming):

```csharp
protected override bool TryGetRouteSegment(ContentReference link, string lang, out string segment)
{
    var culture = string.IsNullOrEmpty(lang) ? CultureInfo.CurrentCulture : CultureInfo.GetCultureInfo(lang);
    if (_contentLoader.TryGet<ProductContent>(link, culture, out var product))
    {
        segment = product.Code;
        return true;
    }
    segment = "";
    return true;
}

protected override CatalogContentBase FindNextContentInSegmentPair(...)
{
    if (catalogContent.ContentType == CatalogContentType.Root)
    {
        var link = _referenceConverter.GetContentLink(segmentPair.Next, CatalogContentType.CatalogEntry);
        return _contentLoader.Get<ProductContent>(link);
    }
    return base.FindNextContentInSegmentPair(...);
}
```

### 1.7 Get products belong to a specific market

API-level approach (recursive `MarketFilter` check) is too slow for large catalogs. Use SQL against the hidden `_ExcludedCatalogEntryMarkets` multi-value-dictionary metafield. An entry is **excluded** from a market if its `_ExcludedCatalogEntryMarkets` contains that MarketId.

```sql
DECLARE @MetaFieldId INT = (SELECT MetaFieldId FROM MetaField
    WHERE Name = '_ExcludedCatalogEntryMarkets' AND Namespace = 'Mediachase.Commerce.Catalog')
DECLARE @MetaDictionaryId INT = (SELECT MetaDictionaryId FROM MetaDictionary
    WHERE Value = 'US' AND MetaFieldId = @MetaFieldId)

SELECT ObjectId FROM CatalogContentProperty
WHERE MetaFieldId = @MetaFieldId
  AND Number NOT IN (
    SELECT mk.MetaKey FROM MetaMultiValueDictionary mmv
    INNER JOIN MetaKey mk ON mmv.MetaKey = mk.MetaKey
    WHERE mk.MetaFieldId = @MetaFieldId AND MetaDictionaryId = @MetaDictionaryId);
```

Dictionary-type lookup cheat sheet:
- **Single-value dictionary:** `Number` column in `CatalogContentProperty` = `MetaDictionaryId`.
- **Multi-value dictionary:** `Number` = `MetaKey` in `MetaKey` table, joined via `MetaMultiValueDictionary` → `MetaDictionary`.
- **String dictionary:** similar, via `MetaStringDictionaryValue`.

### 1.8 Move catalog items around

Two APIs:

```csharp
// Preferred — respects permissions, fires IContentEvents
_contentRepo.Move(contentLink, destination, sourceAccess, destAccess);

// Lower level — no permission checks, no events
var relations = _relationRepository.GetParents<NodeRelation>(contentLink);
// modify relations...
_relationRepository.UpdateRelations(relations);
```

Relation types (Commerce 11+): `NodeRelation` (node↔node), `NodeEntryRelation` (node↔entry). In relations, the **bigger** entity is `Parent`, the **smaller** is `Child`. Pre-11 used `Source`/`Target` — deprecated.

### 1.8.2 Set the primary node

Requires Commerce 11.2+ (`NodeEntryRelation.IsPrimary`):

```csharp
var relations = _relationRepository.GetParents<NodeEntryRelation>(entryLink);
var relation = relations.First(r => r.Parent == parentLink);
relation.IsPrimary = true;
_relationRepository.UpdateRelations(new[] { relation });
```

Rules:
- Setting `IsPrimary=true` on two relations for same entry throws.
- Setting primary overrides any existing primary.

### 1.8.3 Adjust sort order

From Commerce 11+ `SortOrder` is freed from its misused primary-node duty (`IsPrimary` handles that now). Drag-and-drop in Sort Mode works in Catalog UI; programmatically use the same `NodeEntryRelation` APIs.

### 1.9 Build a breadcrumb

Walk up via `ParentLink` until reaching a `CatalogContent`:

```csharp
public ActionResult GenerateBreadCrumb(FashionProduct currentContent)
{
    CatalogContentBase content = currentContent;
    var models = new List<CatalogContentModel>();
    while (!(content is CatalogContent))
    {
        var parent = _contentLoader.Get<CatalogContentBase>(content.ParentLink);
        models.Add(new CatalogContentModel {
            DisplayName = parent is NodeContent n ? n.DisplayName : parent.Name,
            Url = UrlResolver.Current.GetUrl(parent)
        });
        content = parent;
    }
    models.Reverse();
    return PartialView("BreadCrumb", new BreadCrumbViewModel { Models = models });
}
```

### 1.9.2 Breadcrumb: multiple categories scenario

Problem: walking `ParentLink` only reaches the **primary** node, so linked-category visits show wrong breadcrumb. Solution: capture actual routed contents in a custom `HierarchicalCatalogPartialRouter` and stash them in `HttpContext.Items`:

```csharp
public class BreadCrumbCatalogPartialRouter : HierarchicalCatalogPartialRouter
{
    protected override CatalogContentBase FindNextContentInSegmentPair(...)
    {
        var found = base.FindNextContentInSegmentPair(...);
        var http = HttpContext.Current.GetRequestContext().HttpContext;
        var saved = http.Items["RoutedContents"] as BreadCrumbViewModel ?? new BreadCrumbViewModel();
        saved.Models.Add(new CatalogContentModel {
            DisplayName = found is NodeContent n ? n.DisplayName : found.Name,
            Url = UrlResolver.Current.GetUrl(found)
        });
        http.Items["RoutedContents"] = saved;
        return found;
    }
}
```

Controller then simply reads `HttpContext.Items["RoutedContents"]`. No reverse needed — router visits left-to-right.

---

## Chapter 2: Inventories and Prices

Two pricing services:
- `IPriceService` — optimized for reading, caches, **optimizes** prices on save (best prices across overlapping periods win).
- `IPriceDetailService` — saves prices **as-is**, no caching, authoritative per-row.

### 2.0 Update a price

Load filtered prices and save a mutable clone (`IPriceDetailValue` is read-only; `PriceDetailValue` is the writable impl):

```csharp
int totalCount;
var priceFilter = new PriceFilter { Currencies = new List<Currency> { Currency.USD, Currency.EUR } };
var prices = _priceDetailService.List(contentLink, marketId, priceFilter, 0, int.MaxValue, out totalCount);

var priceDetail = new PriceDetailValue(existingPrice);
priceDetail.UnitPrice = new Money(10, Currency.USD);
_priceDetailService.Save(new[] { priceDetail });
```

### 2.0.1 Update prices (bulk)

For bulk updates across markets/currencies, use `IPriceService.SetCatalogEntryPrices` — ignores existing prices (saved prices always win). Takes `CatalogKey`/`IPriceValue` lists; supports batching across SKUs:

```csharp
var prices = new List<IPriceValue>();
foreach (var src in sourcePrices)
    prices.Add(new PriceValue {
        CatalogKey = new CatalogKey(src.EntryCode),
        MarketId = new MarketId(src.Market),
        ValidFrom = src.ValidFrom,
        // ...
    });
_priceService.SetCatalogEntryPrices(prices);
```

Note: from Commerce 11, `CatalogKey` is just a code wrapper — `ApplicationId` removed.

### 2.1 Optimize prices, your own way

`IPriceService` optimizes overlapping periods to keep cheapest. To change (e.g. keep highest), implement `IPriceOptimizer` (Commerce 11+):

```csharp
public interface IPriceOptimizer
{
    IEnumerable<IOptimizedPriceValue> OptimizePrices(IEnumerable<IPriceValue> prices);
}

public class HighestPriceOptimizer : IPriceOptimizer
{
    public IEnumerable<IOptimizedPriceValue> OptimizePrices(IEnumerable<IPriceValue> prices)
        => prices.GroupBy(p => new { p.CatalogKey, p.MinQuantity, p.MarketId, p.ValidFrom,
                                     p.CustomerPricing, p.UnitPrice.Currency })
                 .Select(g => g.OrderByDescending(c => c.UnitPrice.Amount).First())
                 .Select(p => new OptimizedPriceValue(p, null));
}
```

Register via `IConfigurableModule`. Prefer this over intercepting `PriceServiceDatabase` (which may be made internal).

### 2.2 Store the MSRP

Abuse `PriceType.PriceGroup` with a `"MSRP"` code. Put behavior in **extension methods** rather than re-implementing the interface:

```csharp
public static void SaveMsrp(this IPriceDetailService svc, ContentReference link, MarketId market, Currency c, decimal amount)
{
    var msrp = (PriceDetailValue)svc.LoadMsrp(link, market, c);
    if (msrp != null)
    {
        msrp.UnitPrice = new Money(amount, c);
    }
    else
    {
        var entry = _contentLoader.Get<EntryContentBase>(link);
        msrp = new PriceDetailValue {
            CatalogKey = new CatalogKey(entry.Code),
            CustomerPricing = new CustomerPricing(CustomerPricing.PriceType.PriceGroup, "MSRP"),
            UnitPrice = new Money(amount, c),
            MarketId = market,
            MinQuantity = 0,
            ValidFrom = entry.StartPublish ?? DateTime.UtcNow
        };
    }
    svc.Save(msrp);
}
```

For reading use `IPriceService` (cached) not `IPriceDetailService`:

```csharp
public static IPriceValue LoadMsrp(this IPriceService svc, ContentReference link, MarketId market, Currency c)
{
    var code = _referenceConverter.GetCode(link); // avoid loading EntryContentBase just for code
    return svc.GetPrices(market, DateTime.UtcNow, new CatalogKey(code), new PriceFilter{Currencies=new[]{c}})
              .FirstOrDefault(p => p.CustomerPricing.PriceTypeId == CustomerPricing.PriceType.PriceGroup
                                && p.CustomerPricing.PriceCode == "MSRP");
}
```

### 2.2.2 Edit the price type

The Catalog UI price editor hard-codes three `PriceGroup` values. Add `"MSRP"` via the MetaEnum system in an init module:

```csharp
var type = DataContext.Current.MetaModel.RegisteredTypes["ContactGroup"];
var items = MetaEnum.GetItems(type);
if (!items.Any(i => string.Equals(i.Name, "MSRP", StringComparison.OrdinalIgnoreCase)))
    MetaEnum.AddItem(type, "MSRP", items.Count() + 1);
```

### 2.2.3 Show previous prices

Find any expired price and compare to current active price to show a "Was $X" strikethrough. Only display if the previous price was higher:

```csharp
public static IPriceValue GetPreviousPrice(this IPriceService svc, ContentReference link, MarketId m, Currency c)
{
    var prices = svc.GetCatalogEntryPrices(new CatalogKey(_referenceConverter.GetCode(link)));
    var prev = prices.FirstOrDefault(p => p.MarketId == m && p.UnitPrice.Currency == c
                                       && p.ValidUntil.HasValue && p.ValidUntil.Value < DateTime.UtcNow);
    var current = prices.FirstOrDefault(p => p.MarketId == m && p.UnitPrice.Currency == c
                                          && p.ValidFrom < DateTime.UtcNow
                                          && (!p.ValidUntil.HasValue || p.ValidUntil.Value > DateTime.UtcNow));
    return prev?.UnitPrice.Amount > current.UnitPrice.Amount ? prev : current;
}
```

### 2.3 Change prices globally

Two approaches:

**Raw SQL** (fastest but: bypasses cache, fires no events, schema not guaranteed stable):

```sql
UPDATE PriceDetail SET UnitPrice = 0.95 * UnitPrice WHERE 1=1;
UPDATE PriceValue  SET UnitPrice = 0.95 * UnitPrice WHERE 1=1;
```

**Recommended** — recursive API-based update:

```csharp
public void UpdatePrices(ContentReference link)
{
    foreach (var child in _contentLoader.GetChildren<CatalogContentBase>(link))
    {
        if (child is ProductContent) UpdateProductPrices(child.ContentLink);
        else if (child is NodeContent || child is CatalogContent) UpdatePrices(child.ContentLink);
    }
}

private void UpdateProductPrices(ContentReference link)
{
    var prices = _priceDetailService.List(link); // returns all variant prices for a product
    var newPrices = prices.Select(p => new PriceDetailValue(p) {
        UnitPrice = new Money(0.95m * p.UnitPrice.Amount, p.UnitPrice.Currency)
    }).ToList<IPriceDetailValue>();
    _priceDetailService.Save(newPrices);
}
```

For **temporary** changes (Black Friday), prefer promotions (Ch. 6).

### 2.4 Handle obsoleted prices

Issue: prices silently expire via `ValidUntil`; no event fires. Indexed entries need reindexing so facets reflect removed price points. Direct SQL query to find SKUs whose prices expired in a rolling window:

```sql
DECLARE @until DATETIME = GETUTCDATE();
DECLARE @from  DATETIME = DATEADD(MINUTE, -5, @until);

SELECT CatalogEntryCode FROM PriceGroup pg
INNER JOIN PriceValue pv ON pg.PriceGroupId = pv.PriceGroupId
WHERE pv.ValidUntil IS NOT NULL AND pv.ValidUntil BETWEEN @from AND @until;
```

Schedule a job to resolve codes → `ContentReference` via `ReferenceConverter` and reindex.

### 2.5 Split shipments on inventory status

Episerver ships whole shipments atomically. To ship in-stock items first, split into two shipments:

```csharp
var inventoryRecords = inventoryService.QueryByEntry(shipment.LineItems.Select(l => l.Code));

foreach (var record in inventoryRecords)
{
    var lineItem = shipment.LineItems.FirstOrDefault(l => l.Code == record.CatalogEntryCode);
    var newShipment = cart.CreateShipment();
    if (record.PurchaseAvailableQuantity < lineItem.Quantity)
    {
        var remainder = lineItem.Quantity - record.PurchaseAvailableQuantity;
        lineItem.Quantity = record.PurchaseAvailableQuantity;
        var newLineItem = cart.CreateLineItem(lineItem.Code);
        newLineItem.Quantity = remainder;
        newShipment.LineItems.Add(newLineItem);
    }
}
```

### 2.5.2 Copying line item properties

When splitting shipments, copy non-quantity data:

```csharp
private static void CopyLineItemProperties(ILineItem src, ILineItem dst)
{
    dst.DisplayName = src.DisplayName;
    dst.PlacedPrice = src.PlacedPrice;
    dst.ReturnQuantity = src.ReturnQuantity;
    dst.IsInventoryAllocated = src.IsInventoryAllocated;
    dst.IsGift = src.IsGift;
    dst.CopyPropertiesWithOverwriteFrom(src); // extension for IExtendedProperties
}

private static void CopyShipmentProperties(IShipment src, IShipment dst)
{
    dst.ShippingMethodId = src.ShippingMethodId;
    dst.ShippingAddress = src.ShippingAddress;
    dst.PickListId = src.PickListId;
    dst.WarehouseCode = src.WarehouseCode;
    dst.CopyPropertiesWithOverwriteFrom(src);
}
```

`ILineItemInventory` was obsoleted in Commerce 12.1 — its cached inventory fields are no longer needed.

### 2.6 Update inventory, the safe way

**Never** use `IInventoryService.Update` or `.Save` — they create race conditions (two concurrent buyers both reading stock=10, both writing stock=8).

**Always** use `IInventoryService.Adjust` — delegates delta to SQL Server, which handles concurrency correctly.

### 2.7 Select fulfillment warehouse

Implement `IFulfillmentWarehouseProcessor`:

```csharp
public interface IFulfillmentWarehouseProcessor
{
    IWarehouse GetFulfillmentWarehouse(IShipment shipment);
}
```

Simple "first warehouse that has everything" impl:

```csharp
public IWarehouse GetFulfillmentWarehouse(IShipment shipment)
{
    var warehouses = _warehouseRepository.List().ToList();
    var qtyByCode = shipment.LineItems.ToDictionary(k => k.Code, v => v.Quantity);
    var codes = qtyByCode.Keys.ToList();
    var records = _inventoryService.QueryByEntry(codes);

    foreach (var warehouse in warehouses)
    {
        var inWh = records.Where(r => r.WarehouseCode == warehouse.Code).ToList();
        if (inWh.Count < codes.Count) continue;
        if (inWh.All(r => r.PurchaseAvailableQuantity >= qtyByCode[r.CatalogEntryCode]))
            return warehouse;
    }
    return warehouses.First();
}
```

Remember to filter inactive warehouses and compare strings case-insensitively.

### 2.8 Cancel inventory reservation

Cancellation is done via `InventoryRequest` with `RequestType = Cancel`:

```csharp
private void CancelOperationKeys(IShipmentInventory shipment)
{
    var idx = 0;
    var requestItems = shipment.OperationKeys.SelectMany(c => c.Value)
        .Select(key => new InventoryRequestItem {
            ItemIndex = idx++, OperationKey = key, RequestType = InventoryRequestType.Cancel
        }).ToList();
    if (!requestItems.Any()) return;
    _inventoryService.Request(new InventoryRequest(DateTime.UtcNow, requestItems, null));
    shipment.ClearOperationKeys();
}
```

Cast `IShipment` to `IShipmentInventory` and null-check before use.

### 2.8.2 Cancel one line item

Get the operation keys for the specific line item via `IShipmentInventory.GetOperationKeys(ILineItem)`. **Cancel reservation BEFORE removing the line item** — `GetOperationKeys` looks up by index, returns empty if the item was already removed:

```csharp
private void CancelOperationKeys(IShipmentInventory shipment, ILineItem li)
{
    var keys = shipment.GetOperationKeys(li).ToList();
    var idx = 0;
    var requestItems = keys.Select(k => new InventoryRequestItem {
        ItemIndex = idx++, OperationKey = k, RequestType = InventoryRequestType.Cancel }).ToList();
    if (!requestItems.Any()) return;
    _inventoryService.Request(new InventoryRequest(DateTime.UtcNow, requestItems, null));
    keys.ForEach(k => shipment.RemoveOperationKey(li, k));
}
```

---

## Chapter 3: Order

### 3.1 Find abandoned carts

Built-in scheduled job deletes them. To instead remind customers, create a custom `ScheduledJobBase`:

```csharp
[ScheduledPlugIn(GUID = "...", DisplayName = "Old cart reminder",
    Description = "Reminds users with carts older than 3 days",
    LanguagePath = "/Commerce/scheduledjobs/oldcartreminderjob")]
public class OldCartReminderJob : ScheduledJobBase
{
    public override string Execute()
    {
        var parameters = new OrderSearchParameters { SqlMetaWhereClause = "Meta.Modified < GETDATE() - 3" };
        var options = new OrderSearchOptions { Namespace = "Mediachase.Commerce.Orders" };
        options.Classes.Add(OrderContext.ShoppingCartClassType);
        options.RecordsToRetrieve = 50;

        int total = 0, processed = 0;
        do {
            options.StartingRecord = processed;
            var carts = OrderContext.Current.FindCarts(parameters, options, out total);
            foreach (var cart in carts) { /* send reminder */ processed++; }
        } while (processed < total);
        return "Done";
    }
}
```

Avoid double-reminders — combine `Modified` range with a `ReminderSent` flag (see 3.2.3 for selectivity caveat).

### 3.1.2 Searching for serializable abandoned carts

**Legacy** `OrderContext.FindCarts` only works with concrete `Cart`/`PurchaseOrder` — **not** with the serializable cart system. Use the unofficial `SerializableCartDB` (public class in internal namespace, may break between minor versions):

```csharp
var filter = new CartFilter {
    ModifiedFrom = DateTime.MinValue,
    ModifiedTo = DateTime.UtcNow.AddDays(-daysOld),
    StartingRecord = 0,
    RecordsToRetrieve = 100
};
do {
    var carts = _serializableCartDB.Service.FindCarts(filter);
    foreach (var cart in carts) { /* ... */ }
    filter.StartingRecord += carts.Count();
} while (carts.Any());
```

### 3.2 Add meta field to cart

Cart metaclass is `ShoppingCart` (confusingly, the typed class is `Cart`). Add `ReminderSent` via Commerce Manager, then:

```csharp
cart["ReminderSent"] = true;
cart.AcceptChanges();
```

Query by metafield: `SqlMetaWhereClause = "Meta.ReminderSent = 0"`.

### 3.2.2 Adding meta field to cart via code

```csharp
var metafield = MetaField.Load(MetaDataContext.Instance, metaFieldName)
    ?? MetaField.Create(MetaDataContext.Instance, "Mediachase.Commerce.Orders",
         metaFieldName, metaFieldName, "", MetaDataType.Boolean, 0, false, false, false, false);

var mc = MetaClass.Load(OrderContext.MetaDataContext, "ShoppingCart");
if (!mc.MetaFields.Contains(metafield)) mc.AddField(metafield);
```

`MetaField.Create` and `MetaClass.AddField` persist immediately (no `AcceptChanges` needed). Call from `IInitializableModule.Initialize` for idempotent provisioning.

### 3.2.3 Performance considerations

**Gotcha:** filtering carts by a boolean `ReminderSent` metafield is very slow — two-value selectivity defeats indexes. Combine with a `Modified` range (high selectivity):

```csharp
SqlMetaWhereClause = "(Meta.Modified BETWEEN GETDATE() - 3 AND GETDATE() - 5) AND Meta.ReminderSent = 0"
```

### 3.3 Add items to cart

Legacy `CartHelper` is obsoleted in Commerce 11.6.1 (tight coupling; incompatible with SerializableCart).

Abstraction API:

```csharp
var cart = _orderRepository.LoadCart<ICart>(_customerContext.CurrentContactId, "default", _currentMarket);
var newLineItem = cart.CreateLineItem(code, _orderGroupFactory);
newLineItem.Quantity = quantity;
cart.AddLineItem(newLineItem, _orderGroupFactory);
_orderRepository.Save(cart);
```

Use `LoadOrCreate<ICart>` to create if missing (unlike `new CartHelper()`, it doesn't persist until `Save`). When adding an item that already exists, increment quantity on the existing `ILineItem`.

### 3.3.2 Add a bundle to cart

Bundles have no price/inventory — expand to member SKUs via `IRelationRepository`:

```csharp
if (entryContent is BundleContent)
{
    foreach (var relation in _relationRepository.GetChildren<BundleEntry>(entryContent.ContentLink))
    {
        var entry = _contentLoader.Get<EntryContentBase>(relation.Child);
        AddToCart(cart, entry.Code, relation.Quantity ?? 1);
    }
}
```

Optimization: use `ReferenceConverter.GetCode` instead of loading `EntryContentBase`.

### 3.3.3 Remove an item from cart

Line items live on `IShipment.LineItems`. Watch for **gifts** (auto-added by promotions) with the same code:

```csharp
var shipment = cart.GetFirstShipment();
var li = shipment.LineItems.FirstOrDefault(l =>
    l.Code.Equals(entryCode, StringComparison.OrdinalIgnoreCase) && !l.IsGift);
if (li != null) shipment.LineItems.Remove(li);
```

Codes are **case-insensitive** — always use `StringComparison.OrdinalIgnoreCase`.

### 3.4 Shipment-cost-based shipping cost

Implement `IShippingPlugin` (and `IShippingGateway` for legacy compatibility):

```csharp
public interface IShippingPlugin
{
    ShippingRate GetRate(Guid methodId, IShipment shipment, ref string message);
}

public class ShipmentCostBasedGateway : IShippingPlugin, IShippingGateway
{
    public ShipmentCostBasedGateway(IMarket market) { } // required empty ctor in Commerce 11
    public ShipmentCostBasedGateway() { }

    public ShippingRate GetRate(Guid methodId, IShipment shipment, ref string msg)
    {
        var methodDto = ShippingManager.GetShippingMethod(methodId);
        var method = methodDto.ShippingMethod[0];
        var cost = GetShippingCost(shipment.GetShippingItemsTotal(method.Currency).Amount);
        return new ShippingRate(methodId, method.DisplayName, new Money(cost, new Currency(method.Currency)));
    }

    public ShippingRate GetRate(Guid methodId, Shipment shipment, ref string msg)
        => GetRate(methodId, (IShipment)shipment, ref msg);

    private decimal GetShippingCost(decimal cost) =>
        cost < 100 ? cost * 0.15m + 20 :
        cost > 1000 ? cost * 0.05m + 20 :
                      cost * 0.10m + 20;
}
```

For testability, inject `IShippingCalculator`.

### 3.4.2 Configure a shipping method

Two options: Commerce Manager UI, or `ShippingManager`/`ShippingMethodDto`:

```csharp
var dto = ShippingManager.GetShippingMethods(languageId, true);
var row = dto.ShippingMethod.NewShippingMethodRow();
row.ShippingMethodId = Guid.NewGuid();
row.Created = row.Modified = DateTime.UtcNow;
row.Name = row.DisplayName = row.Description = name;
row.BasePrice = basePrice;
row.Currency = currency;
row.LanguageId = languageId;
row.IsActive = true;

var optionRow = dto.ShippingOption.NewShippingOptionRow();
optionRow.ShippingOptionId = Guid.NewGuid();
optionRow.Name = "Shipment Cost Based";
optionRow.ClassName = string.Join(",", typeof(ShipmentCostBasedGateway)
    .AssemblyQualifiedName.Split(',').Take(2)); // Type + assembly, no version
dto.ShippingOption.AddShippingOptionRow(optionRow);
row.ShippingOptionId = optionRow.ShippingOptionId;
dto.ShippingMethod.Rows.Add(row);
ShippingManager.SaveShipping(dto);
```

Store `ClassName` without version for painless upgrades. Deploy the gateway assembly to Commerce Manager's `bin`.

### 3.5 Migrate carts when users log in

Default behavior: anonymous cart items are **merged** with logged-in cart. To **override** instead, implement `IProfileMigrator` (Commerce 10.4.1+):

```csharp
public class MyProfileMigrator : IProfileMigrator
{
    public void MigrateCarts(Guid anonymousId)
    {
        var customerId = PrincipalInfo.CurrentPrincipal.GetCustomerContact().PrimaryKeyId.Value;
        foreach (var sourceCart in _orderRepository.Load<ICart>(anonymousId, null))
        {
            if (!sourceCart.GetAllLineItems().Any()) {
                _orderRepository.Delete(sourceCart.OrderLink); continue;
            }
            var existing = _orderRepository.Load<ICart>(customerId, sourceCart.Name)
                .FirstOrDefault(c => c.Market.MarketId == sourceCart.Market.MarketId);
            if (existing != null) _orderRepository.Delete(existing.OrderLink);
            sourceCart.CustomerId = customerId;
            _orderRepository.Save(sourceCart);
        }
    }
    public void MigrateOrders(Guid _) { }
    public void MigrateWishlists(Guid _) { }
}
```

Register with **`AddHttpContextScoped`** — not singleton (one instance per user/session):

```csharp
services.AddHttpContextScoped<IProfileMigrator, MyProfileMigrator>();
```

For partial override (keep default migrate of orders/wishlists), see 8.1.2.

### 3.6 Customize tax calculation

Replace `ITaxCalculator`. Only two non-obsoleted methods:

```csharp
public class CustomTaxCalculator : ITaxCalculator
{
    public Money GetSalesTax(ILineItem li, IMarket market, IOrderAddress address, Money basePrice)
    {
        var entry = _contentLoader.Get<EntryContentBase>(_referenceConverter.GetContentLink(li.Code));
        var taxCategory = (entry as IPricing)?.TaxCategory;
        // lookup rate, maybe call external service
        return new Money(rate * basePrice.Amount, basePrice.Currency);
    }
    public Money GetShippingTax(ILineItem li, IMarket market, IOrderAddress addr, Money basePrice)
        => new Money(0, basePrice.Currency);
}
```

In EU markets, if `IMarket.PricesIncludeTax` is true, Commerce 12+ auto-excludes tax from order totals.

---

## Chapter 4: Events

### 4.1 Reindex entries when there are changes

Three indexing strategies:
- **Event-driven** — in an ideal world, automatic. Occasionally stops working; restart fixes it.
- **Full index** — expensive, only for new environments.
- **Incremental index** — best in Commerce 11.4+ (earlier versions had gaps around prices/inventories/moves).

Subclass `CatalogEventListenerBase` to hook catalog events. Don't handle `*ing` events for reindex — too early. For `*Deleted` events, index removal is the right action.

### 4.1.2 React to prices and inventories events

Prices/inventories fire through `CatalogKeyEventBroadcaster` (uses Framework Events). Register in `IInitializableModule`:

```csharp
public void Initialize(InitializationEngine ctx)
{
    var evt = Event.Get(CatalogKeyEventBroadcaster.CatalogKeyEventGuid);
    evt.Raised += CatalogKeyEventUpdated;
}

private void CatalogKeyEventUpdated(object sender, EventNotificationEventArgs e)
{
    var args = (CatalogKeyEventArgs)DeSerialize((byte[])e.Param);
    if (args is PriceUpdateEventArgs pu) RemotePriceUpdated(sender, pu);
    else if (args is InventoryUpdateEventArgs iu) RemoteInventoryUpdated(sender, iu);
}

private void RemotePriceUpdated(object sender, PriceUpdateEventArgs args)
{
    var sm = new SearchManager(AppContext.Current.ApplicationName);
    var links = _referenceConverter.GetContentLinks(
        args.CatalogKeys.Select(c => c.CatalogEntryCode), CatalogContentType.CatalogEntry);
    sm.UpdateIndex(links);
}
```

Always **deregister** in `Uninitialize` to prevent memory leaks.

### 4.1.3 Remove entries from index

`SearchManager.UpdateIndex` doesn't delete; `IndexBuilder.DeleteContent` does:

```csharp
public override void EntryDeleted(object src, DeletedEntryEventArgs args)
{
    var sm = new SearchManager(AppContext.Current.ApplicationName);
    foreach (var ib in sm.GetIndexBuilders())
        ib.DeleteContent("_id", (string)args.EntryId);
}
```

Listen to `-Deleted` here (you only need the id) rather than `-Deleting`.

### 4.2 Incremental indexing without events

Commerce 11.4+ ships a new indexing queue (fast, consistent, covers relations/prices/inventories) and a scheduled job. Just schedule it. Internals:

```csharp
var sm = new SearchManager(AppContext.Current.ApplicationName);
sm.SearchIndexMessage += (s, e) => { /* log */ };
sm.BuildIndex(false); // false = incremental, true = full rebuild
```

### 4.3 Update when there are updates

Prefer `IContentEvents` over `CatalogEventListenerBase` when working with content APIs. `IContentEvents` fires at content layer; `CatalogEventListenerBase` at DTO/metaobject layer — lower-level APIs like import/export bypass the former.

`PublishingContent` is pipeline-style — edit `contentEventArgs.Content` directly; no clone/save needed:

```csharp
[ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
public class ContentEventsInitialization : IInitializableModule
{
    public void Initialize(InitializationEngine ctx) {
        var ev = ctx.Locate.Advanced.GetInstance<IContentEvents>();
        ev.PublishingContent += OnPublishing;
    }
    private void OnPublishing(object s, ContentEventArgs e) => e.Content.Name += "Edited";
    public void Uninitialize(InitializationEngine ctx) { /* deregister */ }
}
```

### 4.3.2 A different approach for listening to events

Separation of concerns — keep event logic out of init modules:

```csharp
public class CatalogContentEventListener
{
    public CatalogContentEventListener(IContentEvents evts) { ... }
    public void AddEvents()    { /* register */ }
    public void RemoveEvents() { /* deregister */ }
}

// In IInitializableModule:
var listener = ctx.Locate.Advanced.GetInstance<CatalogContentEventListener>();
listener.AddEvents();
```

### 4.4 Reindex when stock reaches a threshold

Useful optimization: only reindex when stock crosses a threshold (e.g. 10 → 8 is no-op, 11 → 8 triggers). Requires delta-aware inventory events.

---

## Chapter 5: Searching

### 5.1 Searching for entries

Built-in search via `CatalogEntrySearchCriteria` + `SearchService`:

```csharp
var criteria = new CatalogEntrySearchCriteria {
    ClassTypes = new StringCollection { "product", "package", "bundle" },
    Locale = _languageResolver.GetPreferredCulture().Name,
    MarketId = market.MarketId,
    StartingRecord = 0,
    RecordsToRetrieve = 20,
    Sort = new SearchSort(new SearchSortField("name", descending: true)),
    SearchPhrase = "keyword"
};
var result = _search.Search(criteria);
foreach (var doc in result.Documents) {
    var imageUrl = doc["imageurl"]?.Value?.ToString() ?? "";
}
```

Only `code`/`displayname` are in the default document — add custom fields via a custom indexer (5.3).

### 5.2 Building a facet

Define filters in `Mediachase.Search.Filters.Config`:

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
      </Values>
    </Filter>
  </SearchFilters>
</SearchConfig>
```

Three value types: `SimpleValue`, `RangeValue` (numeric), `PriceRangeValue` (currency-aware):

```xml
<PriceRangeValue key="100-200" currency="USD" lowerbound="100" upperbound="200"
  lowerboundincluded="false" upperboundincluded="true">
  <Descriptions defaultLocale="en"><Description locale="en">$100-$200</Description></Descriptions>
</PriceRangeValue>
<RangeValue key="under-1.5" upperbound="1.5">...</RangeValue>
```

Apply all filters to criteria, then layer selected values from querystring:

```csharp
foreach (SearchFilter f in SearchFilterHelper.Current.SearchConfig.SearchFilters)
    criteria.Add(f);

foreach (var selected in GetSelectedFilters())
{
    if (selected.SimpleValue != null)     criteria.Add(selected.Filter.field, selected.SimpleValue);
    if (selected.RangeValue != null)      criteria.Add(selected.Filter.field, selected.RangeValue);
    if (selected.PriceRangeValue != null) criteria.Add(selected.Filter.field, selected.PriceRangeValue);
}
```

### 5.3 Add additional fields to the index

Subclass `BaseCatalogIndexBuilder` (not `CatalogIndexBuilder`):

```csharp
public class MyCatalogIndexer : BaseCatalogIndexBuilder
{
    protected override void OnCatalogEntryIndex(ref SearchDocument document,
        CatalogEntryDto.CatalogEntryRow entry, string language)
    {
        var link = _referenceConverter.GetContentLink(entry.CatalogEntryId, CatalogContentType.CatalogEntry, 0);
        var entryContent = _contentLoader.Get<EntryContentBase>(link, CultureInfo.GetCultureInfo(language));
        document.Add(new SearchField("displayname", entryContent.DisplayName,
            new[] { SearchField.Store.YES, SearchField.IncludeInDefaultSearch.YES }));
        document.Add(new SearchField("url", UrlResolver.Current.GetUrl(link))); // stored only
    }
}
```

Register in `Mediachase.Search.config`:

```xml
<Indexers basePath="[appDataPath]\MySite\SearchIndex">
  <add name="catalog" type="MyCompany.MySite.MyProject.MyCatalogIndexer, MyCompany.MySite.MyProject" />
</Indexers>
```

Copy assembly to Commerce Manager `bin` too.

### 5.3.2 Add additional fields to Find index

Subclass `CatalogContentClientConventions` and register in `IConfigurableModule`:

```csharp
public class SiteCatalogContentClientConventions : CatalogContentClientConventions
{
    public override void ApplyConventions(IClientConventions c)
    {
        base.ApplyConventions(c);
        SearchClient.Instance.Conventions.NestedConventions
            .ForInstancesOf<ProductContent>().Add(x => x.VariationContents());
    }

    protected override void ApplyProductContentConventions(
        ClientConventions.TypeConventionBuilder<ProductContent> b)
    {
        base.ApplyProductContentConventions(b);
        b.ExcludeField(x => x.Variations()).IncludeField(x => x.VariationContents());
    }
}
```

```csharp
[ModuleDependency(typeof(FindCommerceInitializationModule))]
public class FindConfig : IConfigurableModule {
    public void ConfigureContainer(ServiceConfigurationContext ctx) {
        ctx.Container.Configure(c =>
            c.For<CatalogContentClientConventions>().Use<SiteCatalogContentClientConventions>());
    }
}
```

### 5.4 Fast category pages

Don't use `IContentLoader.GetChildren` for listings — DB-backed. Drive category pages from the search provider via outlines:

```csharp
public class CategoryController : ContentController<NodeContent>
{
    public ViewResult Index(FashionNode currentContent)
    {
        criteria.Outlines = SearchFilterHelper.GetOutlinesForNode(currentContent.Code);
        // execute search, return view
    }
}
```

Commerce 11.6 fixed outline linking so linked-node children show up correctly.

### 5.5 Skip indexing certain entries

Subclass `CatalogIndexBuilder` (needs parameterless ctor — legacy constraint) and override `IndexCatalogEntryDto`:

```csharp
public class PartialCatalogIndexer : CatalogIndexBuilder
{
    protected override int IndexCatalogEntryDto(IndexBuilder indexer,
        CatalogEntryDto.CatalogEntryRow row, string defaultCurrency, string[] languages)
    {
        var link = _referenceConverter.GetContentLink(row.Code, CatalogContentType.CatalogEntry);
        var ancestors = _contentLoader.GetAncestors(link).OfType<FashionNode>();
        if (ancestors.Any(c => c.Name == "Mens")) return 0; // skip
        return base.IndexCatalogEntryDto(indexer, row, defaultCurrency, languages);
    }
}
```

### 5.5.2 Better partial indexing

Drawback of 5.5: Catalog UI search also misses the entries. Better: keep them indexed with a visibility flag. Override `OnCatalogEntryIndex` and add a `publicly_visible` bool, then filter end-user searches by it:

```csharp
document.Add(new SearchField("publicly_visible", publiclyVisible));
```

### 5.5.3 Skip indexing content in Find

Built into Find — no custom indexer needed:

```csharp
ContentIndexer.Instance.Conventions
    .ForInstancesOf<ProductContent>()
    .ShouldIndex(content => ShouldIndex(content));
```

### 5.5.4 Hide entries from Find searches

Index the entry with a computed property via extension method, then filter:

```csharp
public static bool ShouldBeHiddenFromSearch(this EntryContentBase entry)
{
    var ancestors = ContentLoader.Service.GetAncestors(entry.ContentLink).OfType<FashionNode>();
    return ancestors.Any(c => c.Name == "Mens");
}

// In conventions:
clientConventions.ForInstancesOf<EntryContentBase>()
    .IncludeField(x => x.ShouldBeHiddenFromSearch());

// Filter extension:
public static ITypeSearch<T> FilterHiddenEntries<T>(this ITypeSearch<T> search) where T : EntryContentBase
    => search.Filter(x => !x.ShouldBeHiddenFromSearch());
```

Static extension methods need `Injected<IContentLoader>`.

---

## Chapter 6: Promotions

The **new** promotion system (Commerce 9.19+) is built on CMS content. The old promotion system is obsoleted and will be removed (around Commerce 13).

### 6.0 Get all promotions

```csharp
public IEnumerable<PromotionData> GetAllPromotions()
{
    var campaigns = _contentLoader.GetChildren<SalesCampaign>(SalesCampaignFolder.CampaignRoot);
    return campaigns.SelectMany(c => _contentLoader.GetChildren<PromotionData>(c.ContentLink));
}
```

Filter by schedule:

```csharp
private bool IsCampaignActive(SalesCampaign c) =>
    c.IsActive && c.ValidFrom > DateTime.UtcNow && c.ValidUntil < DateTime.UtcNow;

private bool IsPromotionActive(PromotionData p) =>
    p.IsActive && (!p.Schedule.UseCampaignDate
       || (p.ValidFrom > DateTime.UtcNow && p.ValidUntil < DateTime.UtcNow));
```

Or use `CampaignInfoExtractor.GetEffectiveStatus(promotion, campaign)`.

### 6.1 How to add coupon code

Every `PromotionData` has a `CouponData` block:

```csharp
var promotion = _contentRepository.Get<PromotionData>(link).CreateWritableClone() as PromotionData;
promotion.CouponData.Code = "COMMERCERECIPES15";
_contentRepository.Save(promotion, SaveAction.Publish, AccessLevel.NoAccess);
```

Promotions/campaigns don't support versions or localization — always `SaveAction.Publish`.

Apply coupon to cart:

```csharp
public bool AddCouponCode(ICart cart, string coupon)
{
    var codes = cart.GetFirstForm().CouponCodes;
    if (codes.Any(c => c.Equals(coupon, StringComparison.OrdinalIgnoreCase))) return false;

    codes.Add(coupon);
    var rewards = cart.ApplyDiscounts(new PromotionEngineSettings());
    var appliedCoupons = rewards.Where(r => r.AppliedCoupon != null).Select(r => r.AppliedCoupon);
    var applied = appliedCoupons.Any(c => c.Equals(coupon, StringComparison.OrdinalIgnoreCase));
    if (!applied) codes.Remove(coupon);
    return applied;
}
```

### 6.2 Change prices globally (promotion)

Use `BuyQuantityGetItemDiscount` over the catalog root:

```csharp
var promotion = _contentRepository.GetDefault<BuyQuantityGetItemDiscount>(campaignLink);
promotion.IsActive = true;
promotion.Name = "20% off everything";
var catalogLinks = _contentRepository
    .GetChildren<CatalogContent>(_referenceConverter.GetRootLink())
    .Select(c => c.ContentLink).ToList();
promotion.Condition.Items = catalogLinks;
promotion.Condition.RequiredQuantity = 1;
promotion.Condition.MatchRecursive = true; // required for category-level items
promotion.DiscountTarget.Items = catalogLinks;
promotion.Discount.UseAmounts = false;
promotion.Discount.Percentage = 20m;
promotion.Schedule.UseCampaignDate = false;
promotion.Schedule.ValidFrom  = new DateTime(2017, 12, 20);
promotion.Schedule.ValidUntil = new DateTime(2017, 12, 25);
_contentRepository.Save(promotion, SaveAction.Publish, AccessLevel.NoAccess);
```

### 6.3 Multiple coupons per promotion

Create a custom promotion type with a `Coupons` list (needs CMS 11+ for `PropertyList<T>`):

```csharp
[ContentType(GUID = "7660BDC2-...", GroupName = "entrypromotion", Order = 10300)]
public class MyPromotion : EntryPromotion
{
    public virtual IList<string> Coupons { get; set; }
}
```

Implement `ICouponFilter` (inherit `DefaultCouponFilter` or implement directly):

```csharp
public class MyCouponFilter : DefaultCouponFilter
{
    public override PromotionFilterContext Filter(PromotionFilterContext ctx, IEnumerable<string> couponCodes)
    {
        foreach (var promo in ctx.IncludedPromotions.OfType<MyPromotion>())
        {
            var required = promo.Coupons;
            if (!required.Any()) continue;
            var match = couponCodes.Intersect(required, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
            if (match != null)
                ctx.AddCouponCode(promo.ContentGuid, match);
            else
                ctx.ExcludePromotion(promo, FulfillmentStatus.CouponCodeRequired,
                    ctx.RequestedStatuses.HasFlag(RequestFulfillmentStatus.NotFulfilled));
        }
        return base.Filter(ctx, couponCodes);
    }
}

services.AddSingleton<ICouponFilter, MyCouponFilter>();
```

### 6.3.2 Multiple coupons for built-in promotions

Cleaner: define an `IMultipleCouponCodes` interface; subclass each built-in promotion and implement it. **Reusable processors** — promotion engine reuses the base processor for derived types:

```csharp
public interface IMultipleCouponCodes { IList<string> CouponCodes { get; set; } }

[ContentType(GUID="E47C...", GroupName="shippingpromotion", Order=30300)]
[PromotionSettings(FixedRedemptionsPerOrder = 1)]
public class MyBuyQuantityGetFreeShipping : BuyQuantityGetFreeShipping, IMultipleCouponCodes
{
    public virtual IList<string> CouponCodes { get; set; }
}
```

Filter by interface instead of concrete type. Disable built-ins:

```csharp
var handler = context.Locate.Advanced.GetInstance<PromotionTypeHandler>();
handler.DisableBuiltinPromotions();
```

### 6.3.3 Comprehensive coupon applied status

Build a `HashSet<string>` of valid coupons from all active promotions (O(1) lookup) and report statuses without running the engine:

```csharp
public enum CouponStatus { Invalid, NotApplicable, Applied }

var validCoupons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var p in GetAllActivePromotions())
{
    if (!string.IsNullOrEmpty(p.Coupon.CouponCode)) validCoupons.Add(p.Coupon.CouponCode);
    if (p is IMultipleCoupons mc) validCoupons.UnionWith(mc.CouponCodes);
}

if (!validCoupons.Contains(coupon)) return CouponStatus.Invalid;
```

### 6.3.4 Cache valid coupon codes

Avoid re-loading all promotions each time. Listen to `DeletingContent` (not `DeletedContent`) and `PublishedContent` to invalidate the cache:

```csharp
public class CouponValidator : IDisposable
{
    public void AddEvents() {
        _contentEvents.DeletingContent += Content_Deleting;
        _contentEvents.PublishedContent += Content_Published;
    }
    private void Content_Deleting(object s, ContentEventArgs e) {
        if (e.Content is PromotionData) { /* invalidate */ }
    }
    public void Dispose() { /* deregister */ }
}
```

Ignore schedule transitions (expiry) — promotion engine catches those anyway. Safer to over-include than under-include.

### 6.4 Get coupon code statistics

Every applied promotion is recorded in `PromotionInformation`:

```sql
SELECT COUNT(OrderFormId) AS TotalCount, CouponCode
FROM PromotionInformation
WHERE PromotionGuid = @ContentGuid AND IsRedeemed = 1
GROUP BY CouponCode
ORDER BY TotalCount DESC;
```

Join `OrderForm` + `SUM(Total)` for revenue per coupon. Use `IContentLoader.Get` to resolve promotion `ContentGuid`.

### 6.5 Product listing with discounted prices

```csharp
var discounts = _promotionEngine.GetDiscountPrices(contentLinks, market, currency);
foreach (var entry in discounts)
    var lowest = entry.DiscountPrices.OrderBy(x => x.Price).First().Price;
```

**Limitations:** only works for quantity-of-one promotions, no order-total or shipping-method conditions. Storing discounted prices in the index gets complex (keeping freshness across campaign changes).

### 6.6 Predict the rate of applicable orders

Replay a candidate promotion against historical orders using `PromotionProcessorResolver` + each promotion's `Evaluate`:

```csharp
public class PromotionEvaluator
{
    public Tuple<int,int> TryEvaluate(PromotionData promo, TimeSpan window)
    {
        int orderCount = 0, redemptCount = 0;
        var processor = _promotionProcessorResolver.ResolveForPromotion(promo);
        foreach (var order in FindPurchaseOrders(window))
        {
            bool applied = false;
            foreach (var form in ((IOrderGroup)order).Forms)
            {
                var reward = processor.Evaluate(promo, CreatePromotionContext(order, form));
                if (reward.Status == FulfillmentStatus.Fulfilled) {
                    redemptCount += reward.Redemptions.Count();
                    applied = true;
                }
            }
            if (applied) orderCount++;
        }
        return Tuple.Create(orderCount, redemptCount);
    }
}
```

Reset incoming order state before evaluation (clear `IOrderForm.Promotions`, zero each `ILineItemDiscountAmount.EntryAmount`/`OrderAmount`):

```csharp
private IOrderGroup Reset(IOrderGroup order)
{
    foreach (var form in order.Forms) form.Promotions.Clear();
    foreach (var li in order.Forms.SelectMany(f => f.GetAllLineItems()))
        if (li is ILineItemDiscountAmount d) { d.EntryAmount = 0; d.OrderAmount = 0; }
    return order;
}
```

Build `PromotionProcessorContext` with the calculators — `ILineItemCalculator`, `IOrderFormCalculator`, `IReturnLineItemCalculator`, `IShippingCalculator`.

### 6.7 Multiple markets campaign

Out-of-the-box campaigns target **one or all markets**. To target multiple specific markets, subclass `SalesCampaign`:

```csharp
[ContentType(GUID = "96D78178-...")]
[AdministrationSettings(CodeOnly = true, GroupName = "marketing")]
[AvailableContentTypes(Include = new[] { typeof(PromotionData) })]
public class MultiMarketsCampaign : SalesCampaign
{
    [ScaffoldColumn(false)]
    public override string TargetMarket { get => base.TargetMarket; set => base.TargetMarket = value; }

    [Display(Order = 50, GroupName = TabNames.MarketAndRevenue)]
    [UIHint(SystemUiHint.MarketsSelector)]
    public virtual ItemCollection<string> TargetMarkets { get; set; }
}
```

Using `SystemUiHint.MarketsSelector` + `ItemCollection<string>` gets the built-in checkbox editor. Or write your own descriptor:

```csharp
[EditorDescriptorRegistration(TargetType = typeof(IList<string>), UIHint = "MultipleMarketsSelector")]
public class MarketListDescriptor : EditorDescriptor
{
    public MarketListDescriptor()
    {
        SelectionFactoryType = typeof(MarketSelectionFactory);
        ClientEditingClass = "epi-cms/contentediting/editors/DialogWithCheckBoxListEditor";
        EditorConfiguration.Add("valueIsCsv", false);
        EditorConfiguration.Add("valueIsInclusive", true);
    }
}
```

Native in Commerce 12.4+.

### 6.7.2 Process multiple markets campaign

Intercept `PromotionEngineContentLoader.GetEvaluablePromotionsInPriorityOrder`:

```csharp
public class MultipleMarketsPromotionEngineContentLoader : PromotionEngineContentLoader
{
    public override IList<PromotionData> GetEvaluablePromotionsInPriorityOrder(IMarket market)
    {
        var campaigns = _contentLoader.GetChildren<SalesCampaign>(GetCampaignFolderRoot())
            .ToDictionary(x => x.ContentLink, x => x);
        return GetPromotions()
            .Where(p => IsValid(p, market, campaigns))
            .OrderBy(p => p.Priority).ToList();
    }

    private bool IsValidMarket(SalesCampaign c, IMarket m)
    {
        if (!m.IsEnabled) return false;
        if (c is MultiMarketsCampaign mmc)
            return mmc.TargetMarkets.Contains(m.MarketId.ToString());
        return false;
    }
}
```

Note: `PromotionEngineContentLoader` lives in an internal namespace — not covered by full backwards-compatibility guarantees.

### 6.7.3 Process shipping methods for multiple markets

`ShippingPromotion.ShippingMethods` is driven by the parent campaign's markets. Override with an editor descriptor that targets `SystemUiHint.ShippingMethodList`:

```csharp
[EditorDescriptorRegistration(TargetType = typeof(IList<Guid>),
    UIHint = SystemUiHint.ShippingMethodList,
    EditorDescriptorBehavior = EditorDescriptorBehavior.OverrideDefault)]
public class ShippingMethodListEditorDescriptor : EditorDescriptor
{
    public ShippingMethodListEditorDescriptor() {
        SelectionFactoryType = typeof(MultiMarketsShippingMethodSelectionFactory);
        ClientEditingClass = "epi-ecf-ui/contentediting/editors/ShippingMethodListEditor";
        EditorConfiguration.Add("valueIsCsv", false);
    }
}

public class MultiMarketsShippingMethodSelectionFactory : ISelectionFactory
{
    public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
    {
        var owner = metadata?.FindOwnerContent();
        if (owner == null) return Enumerable.Empty<ISelectItem>();
        var campaign = _contentLoader.Get<MultiMarketsCampaign>(owner.ParentLink);
        return campaign?.TargetMarkets.SelectMany(GetShippingMethodsByMarket)
               ?? Enumerable.Empty<ISelectItem>();
    }

    protected virtual IEnumerable<ISelectItem> GetShippingMethodsByMarket(string marketId)
        => ShippingManager.GetShippingMethodsByMarket(marketId, true)
            .ShippingMethod.Select(r => new SelectItem { Text = r.Name, Value = r.ShippingMethodId });
}
```

### 6.8 Promotions by visitor groups

`SalesCampaign.VisitorGroups` is built-in. To define a custom criterion:

```csharp
public class RecentOrderModel : CriterionModelBase
{
    [Required, Range(0, 365)]
    public int WithinDays { get; set; }
    public int NumberOfOrders { get; set; }
}

[VisitorGroupCriterion(
    DisplayName = "Recently ordered",
    Description = "Buyers who placed orders within N days",
    Category = "Commerce Criteria",
    LanguagePath = "/commercerecipes/visitorgroups/recentorderscriterion")]
public class RecentOrderCriterion : CriterionBase<RecentOrderModel>
{
    public override bool IsMatch(IPrincipal principal, HttpContextBase http)
    {
        var orders = _orderRepository.Load<IPurchaseOrder>(_customerContext.CurrentContactId)
            .OrderByDescending(x => x.Created).ToList();
        var threshold = DateTime.UtcNow.AddDays(-Model.WithinDays);
        return orders.Count(p => p.Created > threshold) >= Model.NumberOfOrders;
    }
}
```

See `DojoWidgetAttribute` in `EPiServer.Personalization.VisitorGroups` for advanced UI customization.

---

## Chapter 7: Customer Recipes

Customers/Organizations are **Business Foundation** objects. Two separate MetaClass systems:
- `Mediachase.MetaDataPlus.Configurator.MetaClass` → catalog + orders
- `Mediachase.BusinessFoundation.Data.Meta.Management.MetaClass` → contacts + organizations

### 7.1 Add new fields to the contact

Metaclass name is `Contact`, typed class is `CustomerContact`.

Via code (equivalent to Commerce Manager UI):

```csharp
var mc = DataContext.Current.MetaModel.MetaClasses[ContactEntity.ClassName];
using (var builder = new MetaFieldBuilder(mc))
{
    builder.CreateCheckBoxBoolean("NewsletterSubscribed", "NewsletterSubscribed", true, false, "NewsletterSubscribed");
    builder.CreateDateTime("NewsletterSubscribedTime", "NewsletterSubscribedTime", true, false);
    builder.SaveChanges();
}
```

`MetaFieldBuilder` has helpers for many data types (see docs).

### 7.1.2 Ensure contact meta fields

Avoid "already exists" exceptions on re-run. Check `MetaClass.Fields` then use `IInitializationPlugin` (Business Foundation init hook, internal namespace but stable):

```csharp
public interface IInitializationPlugin
{
    void Execute(BusinessContext context);
    int SortOrder { get; }
}

[ServiceConfiguration(typeof(IInitializationPlugin))]
internal class AutoInstallContactModule : IInitializationPlugin
{
    public int SortOrder => 100;
    public void Execute(BusinessContext ctx)
    {
        var mc = DataContext.Current.MetaModel.MetaClasses[ContactEntity.ClassName];
        if (mc.Fields.Contains("NewsletterSubscribed")) return;
        using (var b = new MetaFieldBuilder(mc)) { /* create fields */ b.SaveChanges(); }
    }
}
```

### 7.2 Change the contact select screen

Customize the `Contact → EntitySelect` view via `ListViewProfile`:

```csharp
var lvp = ListViewProfile.Load("Contact", "", "EntitySelect")
    ?? new ListViewProfile { Id = "", IsPublic = true, IsSystem = true, Name = "EntitySelect" };

lvp.FieldSet = new List<string> { "FullName", "Email", "LastOrder" };
lvp.ColumnsUI.Clear();
lvp.ColumnsUI.Add(new ColumnProperties("LastOrder", "200px", ""));
lvp.ColumnsUI.Add(new ColumnProperties("FullName",  "200px", ""));
lvp.ColumnsUI.Add(new ColumnProperties("Email",     "200px", ""));

ListViewProfile.SaveSystemProfile("Contact", "EntitySelect",
    PrincipalInfo.CurrentPrincipal.GetContactId(), lvp);
```

`SystemName` in Business Foundation screen shows the metaclass name you need.

### 7.3 Promotion for loyal customers

Reuse `RecentOrderCriterion` style to total spend:

```csharp
public override bool IsMatch(IPrincipal principal, HttpContextBase http)
{
    var orders = _orderRepository.Load<IPurchaseOrder>(principal.GetContactId())
        .OrderByDescending(x => x.Created).ToList();
    return orders.Sum(p => p.Total).Amount >= 1000;
}
```

**Performance** caveat: loading all orders on every request is expensive. Add a cached `TotalSpent` metafield on `Contact` and update on `SaveAsPurchaseOrder` — trade flexibility for speed.

### 7.4 Shared cart within organization

Organizations are BF objects with Guid ids — just like contacts. `IOrderRepository` is agnostic to ownership; swap in organization id when the user belongs to an organization (intercept `IOrderRepository`):

```csharp
public TOrderGroup Create<TOrderGroup>(Guid customerId, string name) where TOrderGroup : class, IOrderGroup
{
    var contact = CustomerContext.Current.GetContactById(customerId);
    if (contact?.ContactOrganization != null)
        customerId = contact.ContactOrganization.PrimaryKeyId;
    return _innerImplementation.Create<TOrderGroup>(customerId, name);
}

public IEnumerable<TOrderGroup> Load<TOrderGroup>(Guid customerId, string name)
    where TOrderGroup : class, IOrderGroup
{
    var contact = CustomerContext.Current.GetContactById(customerId);
    if (contact?.ContactOrganization != null)
        customerId = contact.ContactOrganization.PrimaryKeyId;
    return _innerImplementation.Load<TOrderGroup>(customerId, name);
}
```

Save requires no changes — current user implicit when saving via `IOrderRepository`.

---

## Chapter 8: Framework

### 8.1 Extend the default behavior (interceptors)

From CMS 10+ default implementations live in `*.Internal` namespaces — not covered by backwards-compat rules. Inheriting internal classes is risky; virtual methods aren't guaranteed. **Preferred pattern: interceptor**.

```csharp
public class MyCustomImplementation : IAmDoingSomething
{
    private IAmDoingSomething _default;
    private ServiceAccessor<IContentLoader> _loaderAccessor;

    public MyCustomImplementation(IAmDoingSomething defaultImplementation,
        ServiceAccessor<IContentLoader> loaderAccessor) {
        _default = defaultImplementation;
        _loaderAccessor = loaderAccessor;
    }

    public void GetSomething(ContentReference link) {
        if (link.ProviderName == ReferenceConverter.ProviderName) { /* custom */ return; }
        _default.GetSomething(link); // delegate
    }

    public int AnotherMethod() => _default.AnotherMethod();
}
```

Register in `IConfigurableModule`:

```csharp
public void ConfigureContainer(ServiceConfigurationContext ctx)
{
    ctx.Services.Intercept<IAmDoingSomething>((locator, defaultImpl) =>
        new MyCustomImplementation(defaultImpl, locator.GetInstance<ServiceAccessor<IContentLoader>>()));
}
```

Container resolves the default, passes it into constructor — no stack overflow.

### 8.1.2 The partially overridden IProfileMigrator

Replace only `MigrateCarts`, delegate `MigrateOrders`/`MigrateWishlists`:

```csharp
public class MyProfileMigrator : IProfileMigrator
{
    private readonly IProfileMigrator _default;
    private readonly IOrderRepository _orderRepository;
    public MyProfileMigrator(IProfileMigrator @default, IOrderRepository repo) { ... }

    public void MigrateCarts(Guid anonymousId) { /* custom */ }
    public void MigrateOrders(Guid id)    => _default.MigrateOrders(id);
    public void MigrateWishlists(Guid id) => _default.MigrateWishlists(id);
}

services.Intercept<IProfileMigrator>((locator, defaultImpl) =>
    new MyProfileMigrator(defaultImpl, locator.GetInstance<IOrderRepository>()));
```

### 8.2 Shared singleton across multiple registrations

Registering the same class for two interfaces with Singleton lifecycle creates **two** instances (one per interface). Use `FactoryMember`:

```csharp
[ServiceConfiguration(typeof(IMyInterface),      Lifecycle = ServiceInstanceScope.Singleton, FactoryMember = "Instance")]
[ServiceConfiguration(typeof(IMyOtherInterface), Lifecycle = ServiceInstanceScope.Singleton, FactoryMember = "Instance")]
public class MyClass : IMyInterface, IMyOtherInterface
{
    private MyClass() { }
    private static MyClass _instance;
    private static readonly object KeyLock = new object();
    public static MyClass Instance
    {
        get {
            if (_instance == null)
                lock (KeyLock) if (_instance == null) _instance = new MyClass();
            return _instance;
        }
    }
}
```

Private constructor prevents accidental extra instances. Container calls `Instance` for both interfaces, so single instance shared.

---

## Chapter 9: Mixes

### 9.1 Add items to indexing queue

Commerce 11.4+ introduced `CatalogItemChangeManager.NotifyChanges`. Build `CatalogEntryChange` with id + code (via `IdentityResolver.GetIdsByCodes`):

```csharp
private IEnumerable<CatalogEntryChange> GetCatalogEntryChanges(IEnumerable<string> codes)
{
    var idByCode = _identityResolver.GetIdsByCodes(codes);
    foreach (var code in codes)
        if (idByCode.TryGetValue(code, out var id))
            yield return new CatalogEntryChange(id, code);
}
```

Commerce 11.6+ adds `NotifyChanges(IEnumerable<int> changes)` — skip `IdentityResolver`, get ids directly:

```sql
SELECT CatalogEntryId FROM CatalogEntry ce
INNER JOIN PriceGroup pg ON pg.CatalogEntryCode = ce.Code
INNER JOIN PriceValue pv ON pg.PriceGroupId = pv.PriceGroupId
WHERE pv.ValidUntil IS NOT NULL AND pv.ValidUntil BETWEEN @from AND @until;
```

### 9.2 Migrate promotions to new campaign type (IMigrationStep)

`IMigrationStep` runs **once per site** (unlike init modules running per startup). Ideal for data migration:

```csharp
[ServiceConfiguration(typeof(IMigrationStep))]
public class MigrateCampaignsStep : IMigrationStep
{
    public int Order => 2000; // higher = later
    public string Name => "Migrate campaigns";
    public string Description => "Migrate all campaigns to support multi markets";

    public bool Execute(IProgressMessenger progress)
    {
        var campaigns = _contentRepository.GetChildren<SalesCampaign>(SalesCampaignFolder.CampaignRoot);
        var markets = _marketService.GetAllMarkets();
        var i = 0;
        foreach (var campaign in campaigns)
        {
            if (campaign is MultiMarketsCampaign) continue;
            progress.AddProgressMessageText($"Processing {campaign.Name}", false, i++ * 100 / campaigns.Count());

            var mmc = _contentRepository.GetDefault<MultiMarketsCampaign>(SalesCampaignFolder.CampaignRoot);
            mmc.Name = campaign.Name; mmc.Code = campaign.Code;
            mmc.Description = campaign.Description; mmc.IsActive = campaign.IsActive;
            mmc.ValidFrom = campaign.ValidFrom; mmc.ValidUntil = campaign.ValidUntil;
            mmc.VisitorGroups = new List<Guid>(campaign.VisitorGroups ?? Enumerable.Empty<Guid>());
            mmc.TargetMarkets = new List<string>();

            if (campaign.TargetMarket == Constants.AllMarkets)
                mmc.TargetMarkets.AddRange(markets.Select(m => m.MarketId.Value));
            else
                mmc.TargetMarkets.Add(campaign.TargetMarket);

            var newLink = _contentRepository.Save(mmc, SaveAction.Publish, AccessLevel.NoAccess);
            foreach (var p in _contentRepository.GetChildren<PromotionData>(campaign.ContentLink))
                _contentRepository.Move(p.ContentLink, newLink);
        }
        return true;
    }
}
```

Notes:
- Returning `false` halts `MigrationManager` — admin must fix and retry.
- Run in CMS site context before starting Commerce Manager.
- Keeping old campaigns lets you verify migration before cleanup.
- Redemption limits/statistics follow promotions (linked by promotion, not campaign).

---

## Chapter 10: Extending Commerce Manager

Commerce Manager is deprecated (no new features; only critical fixes). Extension is unofficial, undocumented, and may break on upgrade.

### 10.1 Add wish-list management

Add menu to `Apps\Order\Config\View\Navigation\LeftMenu.xml`:

```xml
<Link id="Order_Wishlist" text="Wishlists" order="45" iconUrl="" iconCss="treeIcon"
      type="Carts" command="cmdWishlists" permissions="order:mng:view" />

<Command id="cmdWishlists">
  <CommandType>ClientAction</CommandType>
  <ClientScript>CSManagementClient.ChangeView('Wishlist-List', '')</ClientScript>
  <EnableHandler type="Mediachase.Commerce.Manager.CommandHandlers.PermissionEnableHandler,
                       Mediachase.ConsoleManager" />
</Command>
```

Add `Apps\Order\Config\View\Grids\Wishlist-List.xml`:

```xml
<ViewConfig>
  <setAttributes id="Wishlist-List" isNameDynamic="true"
                 controlUrl="Order/Wishlist.ascx" permissions="order:mng:view"
                 help="Order+Management" />
</ViewConfig>
```

Build a UserControl deploying to `AppsOrder\Wishlist.ascx`:

```razor
<%@ Control Language="C#" CodeBehind="Wishlist.ascx.cs"
    Inherits="EPiServer.Commerce.Sample.Wishlist" %>
<%@ Register Src="~/Apps/Core/Controls/EcfListViewControl.ascx"
    TagName="EcfListViewControl" TagPrefix="core" %>
<%@ Register TagPrefix="orders" Namespace="Mediachase.Commerce.Orders.DataSources"
    Assembly="Mediachase.Commerce" %>
<orders:OrderDataSource runat="server" ID="OrderListDataSource" />
<core:EcfListViewControl id="MyListView" runat="server"
    DataSourceID="OrderListDataSource" AppId="Order"
    ViewId="Wishlist-List" ShowTopToolbar="true" />
```

Code-behind inherits `OrderBaseUserControl`:

```csharp
public partial class Wishlist : OrderBaseUserControl
{
    private void InitDataSource(int start, int count, bool returnTotal, string orderBy)
    {
        if (string.IsNullOrEmpty(orderBy)) orderBy = "OrderGroupId DESC";
        OrderListDataSource.Options.Namespace = "Mediachase.Commerce.Orders";
        OrderListDataSource.Options.Classes.Add("ShoppingCart");
        OrderListDataSource.Parameters.SqlWhereClause = " Name = 'Wishlist'";
        MyListView.DataMember = OrderDataSource.OrderDataSourceView.CartsViewName;
        OrderListDataSource.Options.RecordsToRetrieve = count;
        OrderListDataSource.Options.StartingRecord = start;
        OrderListDataSource.Parameters.OrderByClause = orderBy;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack) MyListView.CurrentListView.PrimaryKeyId =
            EcfListView.MakePrimaryKeyIdString("OrderGroupId", "CustomerId");
        InitDataSource(0, int.MaxValue, true, "");
        DataBind();
    }
}
```

Filters shopping carts by `Name = 'Wishlist'`.

### 10.2 Add organization to order list

Edit `Apps\Config\View\Grids\Orders-List.xml`:

```xml
<Column width="150" allowSorting="true" dataField="CustomerId"
        headingText="Organization" Align="Center" columnType="CustomTemplate">
  <Template id="OrganizationTemplate" controlUrl="GridTemplates/Organization.ascx" />
</Column>
```

Organization user control (`Organization.ascx.cs`) implements `IEcfListViewTemplate`:

```csharp
public partial class Organization : BaseUserControl, IEcfListViewTemplate
{
    public object DataItem { get; set; }
    protected global::System.Web.UI.WebControls.Label label1;

    protected void Page_Load(object sender, EventArgs e)
    {
        if (DataItem is IOrderGroup order)
        {
            var contact = CustomerContext.Current.GetContactById(order.CustomerId);
            if (contact?.ContactOrganization != null)
                label1.Text = contact.ContactOrganization.Name;
        }
    }
}
```

### 10.2.1 Organization column, the alternative way

`PurchaseOrder` is a MetaClass — add an `OrganizationName` string metafield, populate on `SaveAsPurchaseOrder`, then use a plain `<Column dataField="OrganizationName">` with no custom template:

```csharp
var po = _orderRepository.SaveAsPurchaseOrder(cart);
var contact = CustomerContext.Current.GetContactById(po.CustomerId);
if (contact?.ContactOrganization != null)
    po.Properties["OrganizationName"] = contact.ContactOrganization.Name;
_orderRepository.Save(po);
```

Trade-off: simpler view config but requires metaclass provisioning, plus similar work for `Cart`/`PaymentPlan` lists.

---

## Appendix: Frequently Referenced APIs

**Identity / References**
- `ReferenceConverter.GetCode`, `GetContentLink`, `GetRootLink`, `GetContentLinks`
- `IContentLoader.Get<T>`, `TryGet<T>`, `GetChildren<T>`, `GetAncestors`
- `IContentVersionRepository.List`
- `IContentRepository.Move`, `CreateWritableClone`, `Save`, `GetDefault<T>`

**Catalog & Routing**
- `CatalogContentProvider`, `CatalogContentType`
- `HierarchicalCatalogPartialRouter` (override `FindNextContentInSegmentPair`, `TryGetRouteSegment`)
- `SeoUriRouter`, `ContentRoute.RoutedContent`
- `IRelationRepository`, `NodeRelation`, `NodeEntryRelation` (Parent/Child/IsPrimary/SortOrder)

**Pricing & Inventory**
- `IPriceService`, `IPriceDetailService`, `IPriceOptimizer`, `IOptimizedPriceValue`
- `IPriceValue`, `PriceValue`, `PriceDetailValue`, `CatalogKey`, `CustomerPricing`, `PriceFilter`, `Money`, `Currency`
- `IInventoryService` (use `Adjust`, avoid `Update`/`Save`), `InventoryRequest`, `InventoryRequestItem`, `InventoryRequestType.Cancel`
- `IFulfillmentWarehouseProcessor`, `IWarehouseRepository`
- `IShipmentInventory` (cast from `IShipment`), `GetOperationKeys`, `ClearOperationKeys`

**Order / Cart**
- `IOrderRepository.LoadCart`, `LoadOrCreate`, `Load`, `Save`, `SaveAsPurchaseOrder`, `Delete`
- `ICart`, `IPurchaseOrder`, `IOrderGroup`, `IOrderForm`, `IShipment`, `ILineItem`
- `IOrderGroupFactory.CreateShipment`, `CreateLineItem`, extension methods `AddLineItem`, `GetAllLineItems`, `GetFirstForm`, `GetFirstShipment`, `GetShippingItemsTotal`
- `IProfileMigrator`, `ITaxCalculator`
- `IShippingPlugin`, `IShippingGateway`, `ShippingManager`, `ShippingMethodDto`
- Legacy: `OrderContext.FindCarts`, `OrderSearch`, `OrderSearchOptions`, `OrderSearchParameters`, `SqlMetaWhereClause`
- Serializable carts: `SerializableCartDB`, `CartFilter`

**Events**
- `IContentEvents.PublishingContent`, `PublishedContent`, `DeletingContent`, `DeletedContent`
- `CatalogEventListenerBase` (override `EntryDeleted`, etc.)
- `CatalogKeyEventBroadcaster.CatalogKeyEventGuid`, `Event.Get(...).Raised`
- `IInitializableModule`, `IConfigurableModule`, `InitializationEngine`

**Search (built-in provider)**
- `CatalogEntrySearchCriteria`, `SearchFilter`, `SelectedFilter`, `SimpleValue`, `RangeValue`, `PriceRangeValue`
- `SearchFilterHelper`, `SearchService`, `ISearchResults`, `SearchDocument`, `SearchField`
- `BaseCatalogIndexBuilder.OnCatalogEntryIndex`, `CatalogIndexBuilder.IndexCatalogEntryDto`
- `SearchManager.GetIndexBuilders`, `UpdateIndex`, `BuildIndex`; `IndexBuilder.DeleteContent`
- Commerce 11.4+: `CatalogItemChangeManager.NotifyChanges`, `CatalogEntryChange`, `IdentityResolver`

**Search (Find)**
- `CatalogContentClientConventions` (override `ApplyConventions`, `ApplyProductContentConventions`)
- `ContentIndexer.Instance.Conventions.ForInstancesOf<T>().ShouldIndex(...)`
- `FindCommerceInitializationModule` (use `[ModuleDependency]`)

**Promotions**
- `PromotionData`, `SalesCampaign`, `SalesCampaignFolder.CampaignRoot`, `CouponData`, `CampaignInfoExtractor`
- `ICouponFilter`, `DefaultCouponFilter`, `PromotionFilterContext`, `IOrderGroup.ApplyDiscounts`, `PromotionEngineSettings`
- `IPromotionEngine.GetDiscountPrices`, `DiscountEntry`, `DiscountPrice`
- `PromotionTypeHandler.DisableBuiltinPromotions`, `PromotionProcessorResolver`, `PromotionProcessorContext`
- Built-in promotion bases: `EntryPromotion`, `BuyQuantityGetItemDiscount`, `BuyQuantityGetFreeShipping`, `ShippingPromotion`
- `PromotionInformation` table (CouponCode + OrderFormId), `PromotionInformationRepository`
- Calculators: `ILineItemCalculator`, `IOrderFormCalculator`, `IReturnLineItemCalculator`, `IShippingCalculator`
- Visitor groups: `CriterionBase<T>`, `CriterionModelBase`, `VisitorGroupCriterion` attribute, `DojoWidgetAttribute`

**Customers / Organizations**
- `CustomerContext.Current`, `CustomerContact`, `ContactEntity.ClassName`, `ContactOrganization`
- `DataContext.Current.MetaModel.MetaClasses`, `MetaFieldBuilder`, `MetaFieldCollection`
- `IInitializationPlugin` (Business Foundation init hook)
- `ListViewProfile`, `ColumnProperties`

**Framework / DI**
- `[ServiceConfiguration]`, `ServiceInstanceScope.Singleton`, `FactoryMember`
- `[ModuleDependency]`, `[InitializableModule]`
- `IServiceConfigurationProvider.Intercept<T>`, `ServiceAccessor<T>`, `Injected<T>`
- `AddHttpContextScoped<TService, TImpl>()`
- `IMigrationStep`, `IProgressMessenger`

**Commerce Manager extension points**
- `OrderBaseUserControl`, `BaseUserControl`, `IEcfListViewTemplate`
- `EcfListViewControl`, `OrderDataSource`, `EcfListView.MakePrimaryKeyIdString`
- View XMLs: `LeftMenu.xml`, `*-List.xml`

---

## Appendix: Database tables referenced

- `CatalogContentProperty`, `MetaField`, `MetaDictionary`, `MetaKey`, `MetaMultiValueDictionary`, `MetaStringDictionaryValue`
- `CatalogEntry`, `PriceGroup`, `PriceValue`, `PriceDetail`
- `PromotionInformation`, `OrderForm`, `OrderGroup`
- `ShoppingCart` (metaclass for `Cart`)

Hidden metafield of note: `_ExcludedCatalogEntryMarkets` (multi-value dictionary on catalog entries).
