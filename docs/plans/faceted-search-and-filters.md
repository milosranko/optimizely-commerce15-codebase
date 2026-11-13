# Plan: Faceted Search and Filters (Configurable)

## Context

The current search uses `UnifiedSearchFor()` with a single `TermsFacetFor(SearchCategories)` that is never applied as a filter. The `SearchCriteriaModel.Facets` property captures user selections but they are discarded. The project needs a CLE (Continuing Legal Education) marketplace with 7 product families, 9 variant-level credit types, 12 facetable fields, CMS-configurable facet definitions, multi-select filtering, and URL-persistent filter state.

This is a backend-only plan. No frontend React components are included.

---

## Key Design Decisions

### 1. TypedSearch for Product Faceting

**Use `IClient.Search<MarketplaceProductBase>()` for catalog search** — not UnifiedSearch.

UnifiedSearch only exposes `SearchTitle`, `SearchText`, `SearchCategories` for faceting. Product-specific fields (`ProductLine`, `Format`, `PracticeAreaDef`, `City`, etc.) are invisible to UnifiedSearch. TypedSearch gives direct access to every indexed property.

The existing `SearchService.PerformGlobalSearch()` using UnifiedSearch is preserved for site-wide search. A new `IProductSearchService.SearchProducts()` runs alongside it.

### 2. Content Type Hierarchy

```
ProductContent (Optimizely)
  └── ProductBase (existing)
        └── MarketplaceProductBase (NEW — all shared marketplace fields)
              ├── EventProduct
              ├── BookProduct
              ├── SubscriptionProduct
              ├── MaterialProduct
              ├── MeetingProduct
              ├── OnlineFormProduct
              └── LawOfficeVideoProduct

VariationContent (Optimizely)
  └── VariantBase (existing)
        └── MarketplaceVariant (NEW — 9 credit fields + registration)
```

`Search<MarketplaceProductBase>()` automatically includes all 7 concrete types in results.

### 3. CLE Credit Aggregation

EPiServer Find indexes variants separately from products. To facet on "which credit types does this product offer", a **computed property** on `MarketplaceProductBase` aggregates variant credit data at index time:

```csharp
// On MarketplaceProductBase
public virtual IEnumerable<string> AvailableCreditTypes
{
    get
    {
        var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
        var variants = contentLoader.GetChildren<MarketplaceVariant>(ContentLink);
        var types = new HashSet<string>();
        foreach (var v in variants)
        {
            if (v.CLECredits > 0) types.Add("CLE");
            if (v.EPRCredits > 0) types.Add("EPR");
            // ... all 9 credit fields
        }
        return types;
    }
}
```

This uses the same `ServiceLocator` pattern as the existing commented-out `ProductCategories` in `ProductBase.cs:56-89`. A Find convention ensures this property is indexed. A content event handler re-indexes the parent product when a variant is published.

### 4. Enum Implementation

Use existing `EnumEditorDescriptor<TEnum>` + `EnumSelectionFactory<TEnum>` pattern (`Core/Infrastructure/EditorDescriptors/`), exactly as `SnippetBlock` uses it for `SnippetType`, `SnippetUsage`, `SnippetArea`.

### 5. Block-Based Facet Configuration

Each facet is defined as a `FacetDefinitionBlock` placed in a ContentArea on a new `SearchSettingsPage`. Admins reorder blocks to change facet display order, toggle `IsEnabled`, and edit display labels. The search service reads this configuration at query time (cached via the existing `SiteSettingsManager` + cache decorator pattern).

### 6. Filter Semantics

- **Within a facet (same field):** OR — selecting `Format=Live` + `Format=OnDemand` returns Live OR OnDemand
- **Across facets (different fields):** AND — selecting `Format=Live` + `PracticeArea=CorporateLaw` returns Live AND CorporateLaw
- This is the standard e-commerce faceted search pattern

### 7. URL Persistence

Query parameters use `f.{FieldName}={Value}` pattern:
```
/api/products/search?q=ethics&f.PracticeArea=CorporateLaw&f.PracticeArea=RealEstate&f.Format=OnDemand&p=1&ps=20
```
A custom `IModelBinder` parses `f.` prefixed keys into `Dictionary<string, string[]>`.

---

## Complete Facet Inventory

### Categorical (Terms) Facets — from Base Product
| # | Field | Source Property | Enum Type |
|---|-------|----------------|-----------|
| 1 | ProductLine | `MarketplaceProductBase.ProductLine` | `ProductLineEnum` |
| 2 | CreditType | `MarketplaceProductBase.CreditType` | `CreditTypeEnum` |
| 3 | PracticeArea | `MarketplaceProductBase.PracticeAreaDef` | `PracticeAreaEnum` |
| 4 | PracticeSubArea | `MarketplaceProductBase.PracticeSubArea` | `PracticeSubAreaEnum` |
| 5 | Format | `MarketplaceProductBase.Format` | `FormatEnum` |
| 6 | Tags | `MarketplaceProductBase.Tags` | `TagsEnum` |

### Categorical Facets — from Product Families
| # | Field | Source Property | Family |
|---|-------|----------------|--------|
| 7 | City | `EventProduct.City` | Event |
| 8 | Language | `BookProduct.Language` / `MaterialProduct.Language` / `LawOfficeVideoProduct.Language` | Book, Material, Video |
| 9 | Entity | `MeetingProduct.Entity` | Meeting |

### Boolean Facet
| # | Field | Source Property |
|---|-------|----------------|
| 10 | UPass | `MarketplaceProductBase.IsUPass` |

### Range Facet
| # | Field | Source Property | Notes |
|---|-------|----------------|-------|
| 11 | Ratings | `MarketplaceProductBase.Ratings` | Numeric range facet with configurable intervals |

### Aggregated Facet (from variants)
| # | Field | Source Property | Notes |
|---|-------|----------------|-------|
| 12 | CreditTypes | `MarketplaceProductBase.AvailableCreditTypes` (computed) | Aggregates 9 variant-level credit fields into terms facet |

---

## Implementation Phases

### Phase 1: Marketplace Enums
**~8 new files, ~120 lines total (all trivial enum declarations)**

New files in `ContentTypes/Commerce/Enums/`:
- `ProductLineEnum.cs`
- `CreditTypeEnum.cs`
- `PracticeAreaEnum.cs`
- `PracticeSubAreaEnum.cs`
- `FormatEnum.cs`
- `TagsEnum.cs`
- `CategoryEnum.cs`
- `CreditTypeNameEnum.cs`

Modify `Core/Constants/Globals.cs`:
- Add `GroupNames.Marketplace = "Marketplace"`
- Add `MetaClassNames` for marketplace types
- Add tab constants for marketplace product tabs (Product, Credits, Sponsor, Schedule, etc.)

### Phase 2: MarketplaceProductBase + MarketplaceVariant + MarketplaceCategory
**3 new files, ~355 lines**

- `ContentTypes/Commerce/Products/Base/MarketplaceProductBase.cs` (~250 lines — all property declarations + `AvailableCreditTypes` computed property)
- `ContentTypes/Commerce/Variants/MarketplaceVariant.cs` (~80 lines — 9 credit fields + registration fields)
- `ContentTypes/Commerce/Categories/MarketplaceCategory.cs` (~25 lines)

Note: `MarketplaceProductBase` exceeds ~300 line guideline but is purely property declarations with `[Display]` attributes — cannot meaningfully be split without breaking the Optimizely content model pattern. Approval required.

### Phase 3: Product Families — Event, Book, Subscription
**3 new files, ~400 lines**

- `ContentTypes/Commerce/Products/EventProduct.cs` (~200 lines — sponsors, tabs, location, schedule, addons)
- `ContentTypes/Commerce/Products/BookProduct.cs` (~120 lines — ISBN, publisher, tabs, subscription)
- `ContentTypes/Commerce/Products/SubscriptionProduct.cs` (~80 lines — tabs, subscription)

### Phase 4: Product Families — Material, Meeting, OnlineForm, LawOfficeVideo
**4 new files, ~430 lines**

- `ContentTypes/Commerce/Products/MaterialProduct.cs` (~100 lines)
- `ContentTypes/Commerce/Products/MeetingProduct.cs` (~150 lines)
- `ContentTypes/Commerce/Products/OnlineFormProduct.cs` (~80 lines)
- `ContentTypes/Commerce/Products/LawOfficeVideoProduct.cs` (~100 lines)

### Phase 5: FacetDefinitionBlock + SearchSettingsPage
**4 new files, ~155 lines**

- `ContentTypes/Cms/Blocks/FacetDefinitionBlock.cs` (~60 lines) — FieldIdentifier (dropdown via `FacetFieldSelectionFactory`), DisplayLabel, FacetType (enum: Terms/Range/Boolean), IsEnabled, DefaultExpanded, RangeMin/RangeMax/RangeInterval
- `ContentTypes/Cms/Blocks/Enums/FacetTypeEnum.cs` (~8 lines)
- `ContentTypes/Cms/Pages/SearchSettingsPage.cs` (~40 lines) — extends `SiteSettingsPageBase`, ContentArea of `FacetDefinitionBlock`, DefaultPageSize, MaxPageSize. AvailableContentTypes: only under StartPage. Access: WebAdmins.
- `Core/Infrastructure/EditorDescriptors/FacetFieldSelectionFactory.cs` (~45 lines) — `ISelectionFactory` returning known facetable field identifiers

### Phase 6: Search Models + FacetFieldRegistry
**5 new files, ~175 lines**

- `Business/Features/Search/Models/ProductSearchCriteriaModel.cs` (~25 lines)
- `Business/Features/Search/Models/ProductSearchResultModel.cs` (~15 lines)
- `Business/Features/Search/Models/FacetResultModel.cs` (~20 lines)
- `Business/Features/Search/Models/ProductSearchHitModel.cs` (~15 lines)
- `Business/Features/Search/Configuration/FacetFieldRegistry.cs` (~100 lines) — static dictionary mapping field identifiers to `Expression<Func<MarketplaceProductBase, T>>` lambdas for Find faceting

### Phase 7: ProductSearchService + Find Convention
**3 new files, ~300 lines**

- `Business/Features/Search/Interfaces/IProductSearchService.cs` (~10 lines)
- `Business/Features/Search/Services/ProductSearchService.cs` (~250 lines) — loads facet config from `SearchSettingsPage` via `ISiteSettingsManager`, builds `Search<MarketplaceProductBase>()` query, adds facets dynamically from config, applies filters from `FacetSelections`, handles Terms/Range/Boolean facet types, returns `ProductSearchResultModel`
- `Business/Features/Search/Conventions/MarketplaceProductConvention.cs` (~40 lines) — registers `AvailableCreditTypes` computed property for Find indexing

### Phase 8: Query Binding + DI + Event Handlers
**2 new + 2 modified files, ~95 lines**

- `Business/Features/Search/Infrastructure/FacetSelectionModelBinder.cs` (~50 lines) — parses `f.` prefix query params into `Dictionary<string, string[]>`
- `Business/Features/Search/Infrastructure/FacetSelectionModelBinderProvider.cs` (~20 lines)
- MODIFY `Web/Startup.cs` (~15 lines) — register `IProductSearchService`, model binder provider, Find convention
- MODIFY `Web/Infrastructure/` or initialization module (~10 lines) — re-index parent product when `MarketplaceVariant` is published

### Phase 9: API Endpoint
**1 new file, ~40 lines**

- `Web/Api/ProductSearchEndpoints.cs` — `GET /api/products/search` mapped to `IProductSearchService.SearchProducts()`

### Phase 10: Unit Tests
**3 new files, ~290 lines**

- `Tests/Features/Search/FacetFieldRegistryTests.cs` (~60 lines)
- `Tests/Features/Search/ProductSearchServiceTests.cs` (~150 lines)
- `Tests/Features/Search/FacetSelectionModelBinderTests.cs` (~80 lines)

---

## Phase Dependencies

```
Phase 1 (Enums) ──┐
                   ├──> Phase 2 (Base Types) ──> Phase 3 (Families 1) ──> Phase 4 (Families 2)
                   │
Phase 5 (Facet CMS blocks) ──┐
                              ├──> Phase 6 (Models + Registry) ──> Phase 7 (Search Service)
                              │         ──> Phase 8 (Binding + DI) ──> Phase 9 (API) ──> Phase 10 (Tests)
```

Phases 3-4 and Phase 5 can run in parallel. Phase 6 requires Phases 1-5. Phases 7-10 are sequential.

---

## Critical Existing Files (Reference)

| File | Why |
|------|-----|
| `ContentTypes/Commerce/Products/Base/ProductBase.cs` | Base class for `MarketplaceProductBase`; contains `ProductCategories` computed property pattern (line 56-89) |
| `ContentTypes/Cms/Blocks/SnippetBlock.cs` | Pattern for `FacetDefinitionBlock` — block with enum properties via `EnumEditorDescriptor<T>` |
| `Core/Infrastructure/EditorDescriptors/EnumEditorDescriptor.cs` | Reuse for all marketplace enums |
| `Core/Infrastructure/EditorDescriptors/EnumSelectionFactory.cs` | Reuse for all marketplace enum dropdowns |
| `ContentTypes/Cms/Pages/SiteSettingsPage.cs` | Pattern for `SearchSettingsPage` — extends `SiteSettingsPageBase`, child of StartPage, WebAdmins access |
| `Business/Features/SiteSettings/Services/SiteSettingsManager.cs` | How `ProductSearchService` will load facet configuration |
| `Business/Features/Search/Services/SearchService.cs` | Existing service preserved; new `ProductSearchService` runs alongside it |
| `Core/Constants/Globals.cs` | Add marketplace tab/group constants |

---

## Verification

1. `dotnet build src\Commerce.Demo.sln` — compiles after each phase
2. `dotnet test src\Commerce.Demo.Tests` — all tests pass
3. Create a `SearchSettingsPage` under StartPage in CMS, add `FacetDefinitionBlock` items to configure facets
4. `GET /api/products/search?q=ethics&f.Format=OnDemand` — returns filtered results with facet counts
5. Verify facet counts update when filters are applied (OR within same facet, AND across)
6. Publish a `MarketplaceVariant` with credit values → verify parent product's `AvailableCreditTypes` facet updates

## Doc Updates Required

| Document | Update |
|---|---|
| `docs/decisions.md` | ADR: TypedSearch for product faceting; ADR: computed property for CLE credit aggregation; ADR: block-based facet configuration |
| `docs/architecture.md` | Add marketplace content type hierarchy, facet configuration flow, product search pipeline |
| `docs/api.md` | `GET /api/products/search` endpoint with query parameters |
| `docs/stack.md` | Note marketplace enums and content types |
| `docs/devlog.md` | Session entries per phase |
| `docs/todo.md` | Mark phases complete |
