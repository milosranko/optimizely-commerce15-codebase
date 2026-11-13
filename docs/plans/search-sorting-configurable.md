# Plan: Search Sorting (Configurable)

## Context

The current `SortType` enum (`Business/Features/Search/Enums/SortType.cs`) has four hard-coded values: `Relevance`, `Alphabetical`, `Newest`, `Oldest`. The Product Search Tiles plan adds `PriceLowToHigh` and `PriceHighToLow`. All sort options are code-fixed — editors have no way to control which options users see, what labels they display, or their order.

This plan makes sorting CMS-configurable by adding a `SortOptionBlock` (placed in ContentArea on `SearchSettingsPage` alongside the existing `FacetDefinitionBlock`), a `SortFieldRegistry` mapping identifiers to EPiServer Find `OrderBy` expressions, and API/frontend changes to serve and render the configured options dynamically.

**Prerequisites assumed complete:** Faceted Search plan (provides `SearchSettingsPage`, `ProductSearchService`, `MarketplaceProductBase` with computed properties), Product Search Tiles plan (provides `SortDropdown.jsx`, API endpoint, `useProductSearch` hook).

---

## Key Design Decisions

### 1. Block-Based Configuration (Consistent with Facets)

Same pattern as `FacetDefinitionBlock` — a `SortOptionBlock` in a ContentArea on `SearchSettingsPage`. Editors reorder blocks to change dropdown order, toggle `IsEnabled`, edit labels, and mark one option as default. This is consistent with the facet configuration pattern and lives on the same settings page.

### 2. Replace Enum with Registry

The hard-coded `SortType` enum is **replaced** by a `SortFieldRegistry` that maps string identifiers to Find `OrderBy`/`OrderByDescending` expressions. The `ProductSearchService` no longer switches on enum values — it looks up the sort field in the registry and applies the expression dynamically.

The existing `SearchService.PerformGlobalSearch()` (UnifiedSearch) still uses its own `SortType` switch internally and is not affected by this change.

### 3. Credits Sort Requires Computed Property

Sorting by "Credits High to Low" requires a numeric value indexed on the product. The existing `AvailableCreditTypes` computed property returns credit type names (strings), not a count. A new `TotalCredits` computed property on `MarketplaceProductBase` sums the highest credit value across all variants (or total credits — see field mapping below). This follows the same `ServiceLocator` + Find convention pattern as `AvailableCreditTypes` and `ListPrice`.

---

## Sort Field Inventory

| Identifier | Display Label (default) | Index Property | Direction | Notes |
|---|---|---|---|---|
| `relevance` | Relevance | Find default scoring + boosting | N/A | No explicit OrderBy — uses Find's scored relevance |
| `releaseDateDesc` | Newest Releases | `MarketplaceProductBase.ReleaseDate` | Descending | Most recent first |
| `releaseDateAsc` | Oldest Releases | `MarketplaceProductBase.ReleaseDate` | Ascending | |
| `eventDateDesc` | Event Date (Upcoming) | `MarketplaceProductBase.C_EventDate` | Descending | Most relevant for events; shows nearest future events first |
| `eventDateAsc` | Event Date (Past First) | `MarketplaceProductBase.C_EventDate` | Ascending | |
| `priceLowToHigh` | Price: Low to High | `MarketplaceProductBase.ListPrice` (computed) | Ascending | Computed from first variant's default price |
| `priceHighToLow` | Price: High to Low | `MarketplaceProductBase.ListPrice` (computed) | Descending | |
| `creditsDesc` | Credits: High to Low | `MarketplaceProductBase.TotalCredits` (computed, NEW) | Descending | Sum/max of variant credit values |
| `creditsAsc` | Credits: Low to High | `MarketplaceProductBase.TotalCredits` (computed, NEW) | Ascending | |
| `titleAsc` | Title: A–Z | `MarketplaceProductBase.Name` | Ascending | |
| `titleDesc` | Title: Z–A | `MarketplaceProductBase.Name` | Descending | |
| `ratingDesc` | Rating: Highest First | `MarketplaceProductBase.Ratings` | Descending | |

All display labels are CMS-editable defaults. Editors can change "Newest Releases" to "Most Recent" or any marketplace-appropriate term.

---

## Content Type Changes

### `SortOptionBlock` (NEW — `ContentTypes/Cms/Blocks/SortOptionBlock.cs`)

| Property | Type | Notes |
|----------|------|-------|
| `FieldIdentifier` | string | Dropdown via `SortFieldSelectionFactory` — maps to registry keys above |
| `DisplayLabel` | string | CultureSpecific — what users see in the dropdown |
| `IsEnabled` | bool | Default true — toggle without removing |
| `IsDefault` | bool | Default false — at most one should be true; if none, falls back to `relevance` |

Order is determined by position in the ContentArea (same as FacetDefinitionBlock).

No `SortDirection` property needed — direction is baked into the field identifier (`priceLowToHigh` vs `priceHighToLow`). This keeps the CMS experience simple for editors who don't need to think about ascending/descending.

### `SearchSettingsPage` (MODIFY — `ContentTypes/Cms/Pages/SearchSettingsPage.cs`)

Add a second ContentArea alongside the existing `FacetDefinitions`:

| Property | Type | Notes |
|----------|------|-------|
| `SortOptions` | ContentArea | `[AllowedTypes(typeof(SortOptionBlock))]` — holds SortOptionBlock items |

Existing properties (`FacetDefinitions`, `DefaultPageSize`, `MaxPageSize`) unchanged.

### `MarketplaceProductBase` (MODIFY — `ContentTypes/Commerce/Products/Base/MarketplaceProductBase.cs`)

Add computed property:

```csharp
public virtual double TotalCredits
{
    get
    {
        var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
        var variants = contentLoader.GetChildren<MarketplaceVariant>(ContentLink);
        return variants.Sum(v =>
            (v.GALDVCredits ?? 0) + (v.CCRBCredits ?? 0) + (v.CLECredits ?? 0) +
            (v.EPRCredits ?? 0) + (v.LPMCredits ?? 0) + (v.GALADCredits ?? 0) +
            (v.GALMINCredits ?? 0) + (v.GALFCredits ?? 0) + (v.LAUCredits ?? 0));
    }
}
```

This is the maximum total credits across all variants. Indexed by the existing `MarketplaceProductConvention` (from Faceted Search plan Phase 7) — add `IncludeField(x => x.TotalCredits)` alongside `AvailableCreditTypes` and `ListPrice`.

---

## Backend Changes

### `SortFieldRegistry` (NEW — `Business/Features/Search/Configuration/SortFieldRegistry.cs`)

A static registry mapping sort identifiers to Find expressions. Each entry contains:
- `Identifier` (string) — matches `SortOptionBlock.FieldIdentifier`
- `ApplySort` — `Func<ITypeSearch<MarketplaceProductBase>, ITypeSearch<MarketplaceProductBase>>` that applies the correct `OrderBy`/`OrderByDescending`
- `IsRelevance` (bool) — true only for `relevance` (skip OrderBy, use Find's default scoring)

```csharp
public static class SortFieldRegistry
{
    private static readonly Dictionary<string, SortFieldDescriptor> _fields = new()
    {
        ["relevance"] = new("Relevance", search => search, IsRelevance: true),
        ["releaseDateDesc"] = new("Newest Releases", search => search.OrderByDescending(x => x.ReleaseDate)),
        ["releaseDateAsc"] = new("Oldest Releases", search => search.OrderBy(x => x.ReleaseDate)),
        ["eventDateDesc"] = new("Event Date (Upcoming)", search => search.OrderByDescending(x => x.C_EventDate)),
        ["eventDateAsc"] = new("Event Date (Past First)", search => search.OrderBy(x => x.C_EventDate)),
        ["priceLowToHigh"] = new("Price: Low to High", search => search.OrderBy(x => x.ListPrice)),
        ["priceHighToLow"] = new("Price: High to Low", search => search.OrderByDescending(x => x.ListPrice)),
        ["creditsDesc"] = new("Credits: High to Low", search => search.OrderByDescending(x => x.TotalCredits)),
        ["creditsAsc"] = new("Credits: Low to High", search => search.OrderBy(x => x.TotalCredits)),
        ["titleAsc"] = new("Title: A–Z", search => search.OrderBy(x => x.Name)),
        ["titleDesc"] = new("Title: Z–A", search => search.OrderByDescending(x => x.Name)),
        ["ratingDesc"] = new("Rating: Highest First", search => search.OrderByDescending(x => x.Ratings)),
    };

    public static SortFieldDescriptor? Get(string identifier) => _fields.GetValueOrDefault(identifier);
    public static IEnumerable<string> AllIdentifiers => _fields.Keys;
}

public record SortFieldDescriptor(
    string DefaultLabel,
    Func<ITypeSearch<MarketplaceProductBase>, ITypeSearch<MarketplaceProductBase>> ApplySort,
    bool IsRelevance = false
);
```

### `SortFieldSelectionFactory` (NEW — `Core/Infrastructure/EditorDescriptors/SortFieldSelectionFactory.cs`)

`ISelectionFactory` that returns `SortFieldRegistry.AllIdentifiers` as dropdown options for the CMS editor. Each entry shows the default label (e.g., "Relevance", "Price: Low to High") and stores the identifier string.

### `ProductSearchService` (MODIFY — `Business/Features/Search/Services/ProductSearchService.cs`)

Replace the `SortType` enum switch with a registry lookup:

```csharp
// Before (enum-based):
switch (sortOrder)
{
    case SortType.Alphabetical: search = search.OrderBy(x => x.Name); break;
    // ...
}

// After (registry-based):
var sortDescriptor = SortFieldRegistry.Get(criteria.SortBy ?? "relevance");
if (sortDescriptor is not null && !sortDescriptor.IsRelevance)
{
    search = sortDescriptor.ApplySort(search);
}
```

The method also loads sort configuration from `SearchSettingsPage` to validate that the requested sort is enabled and to build the available sort options list for the API response.

### `ProductSearchCriteriaModel` (MODIFY — `Business/Features/Search/Models/ProductSearchCriteriaModel.cs`)

Replace the `SortType? Order` property with a string-based sort identifier:

```csharp
[FromQuery(Name = "o")]
public string? SortBy { get; set; }    // Registry identifier, e.g., "priceLowToHigh"
```

### API Response: Sort Options in `ProductSearchResponse`

Add to the response model (from Product Search Tiles plan):

```csharp
public record SortOptionResponse
{
    public string Identifier { get; init; }    // "priceLowToHigh"
    public string Label { get; init; }         // "Price: Low to High" (CMS-configured)
    public bool IsSelected { get; init; }      // Currently active
    public bool IsDefault { get; init; }       // Marked as default in CMS
}
```

Add to `ProductSearchResponse`:
```csharp
public IEnumerable<SortOptionResponse> SortOptions { get; init; }
```

The frontend `SortDropdown.jsx` reads `data.sortOptions` instead of a hard-coded list, dynamically rendering whatever options the CMS provides.

---

## Frontend Changes

### `SortDropdown.jsx` (MODIFY — from Product Search Tiles plan)

Currently planned as a hard-coded dropdown. Change to data-driven:

```jsx
function SortDropdown({ sortOptions, currentSort, onSortChange }) {
  return (
    <select
      value={currentSort}
      onChange={(e) => onSortChange(e.target.value)}
      className="border border-gray-300 rounded-md px-3 py-2 text-sm bg-white"
    >
      {sortOptions.map(option => (
        <option key={option.identifier} value={option.identifier}>
          {option.label}
        </option>
      ))}
    </select>
  )
}
```

### `useProductSearch.js` (MODIFY — from Product Search Tiles plan)

Minor change: the `sort` state reads from `o` query param and uses string identifiers instead of enum names. Already designed this way in the Product Search Tiles plan — no breaking change.

---

## Files to Change (Summary)

### New Files (6)
| File | Project | Lines |
|------|---------|-------|
| `ContentTypes/Cms/Blocks/SortOptionBlock.cs` | ContentTypes | ~40 |
| `Business/Features/Search/Configuration/SortFieldRegistry.cs` | Business | ~60 |
| `Core/Infrastructure/EditorDescriptors/SortFieldSelectionFactory.cs` | Core | ~30 |
| `Web/Api/Responses/Products/SortOptionResponse.cs` | Web | ~10 |
| `Tests/Features/Search/SortFieldRegistryTests.cs` | Tests | ~60 |
| `Tests/Features/Search/SortConfigurationTests.cs` | Tests | ~50 |

### Modified Files (6)
| File | Project | Change |
|------|---------|--------|
| `ContentTypes/Cms/Pages/SearchSettingsPage.cs` | ContentTypes | Add `SortOptions` ContentArea (~5 lines) |
| `ContentTypes/Commerce/Products/Base/MarketplaceProductBase.cs` | ContentTypes | Add `TotalCredits` computed property (~15 lines) |
| `Business/Features/Search/Services/ProductSearchService.cs` | Business | Replace enum switch with registry lookup + load CMS config (~20 lines) |
| `Business/Features/Search/Models/ProductSearchCriteriaModel.cs` | Business | Change `SortType? Order` → `string? SortBy` (~2 lines) |
| `Business/Features/Search/Conventions/MarketplaceProductConvention.cs` | Business | Add `IncludeField(x => x.TotalCredits)` (~1 line) |
| `Web/Api/Responses/Products/ProductSearchResponse.cs` | Web | Add `SortOptions` property (~1 line) |

### Frontend Modified (from Product Search Tiles plan)
| File | Change |
|------|--------|
| `src/components/search/SortDropdown.jsx` | Data-driven from `sortOptions` prop instead of hard-coded list |

**Total: 6 new files, 7 modified files, ~295 lines new code**

---

## Implementation Phases

### Phase 1: SortOptionBlock + SearchSettingsPage Update
**2 new + 1 modified, ~45 lines**

- NEW `ContentTypes/Cms/Blocks/SortOptionBlock.cs`
- NEW `Core/Infrastructure/EditorDescriptors/SortFieldSelectionFactory.cs`
- MODIFY `ContentTypes/Cms/Pages/SearchSettingsPage.cs` — add `SortOptions` ContentArea

### Phase 2: SortFieldRegistry + TotalCredits Property
**1 new + 2 modified, ~75 lines**

- NEW `Business/Features/Search/Configuration/SortFieldRegistry.cs`
- MODIFY `ContentTypes/Commerce/Products/Base/MarketplaceProductBase.cs` — add `TotalCredits`
- MODIFY `Business/Features/Search/Conventions/MarketplaceProductConvention.cs` — index `TotalCredits`

### Phase 3: ProductSearchService + Model Updates
**1 new + 2 modified, ~35 lines**

- NEW `Web/Api/Responses/Products/SortOptionResponse.cs`
- MODIFY `Business/Features/Search/Models/ProductSearchCriteriaModel.cs` — `string? SortBy`
- MODIFY `Business/Features/Search/Services/ProductSearchService.cs` — registry-based sort + load CMS config
- MODIFY `Web/Api/Responses/Products/ProductSearchResponse.cs` — add `SortOptions`

### Phase 4: Frontend SortDropdown Update
**1 modified, ~10 lines**

- MODIFY `src/components/search/SortDropdown.jsx` — read `sortOptions` from API response

### Phase 5: Tests
**2 new files, ~110 lines**

- NEW `Tests/Features/Search/SortFieldRegistryTests.cs` — verify all 12 identifiers resolve, verify Apply functions produce valid queries
- NEW `Tests/Features/Search/SortConfigurationTests.cs` — verify CMS fallback when no config exists, verify default sort selection

---

## Phase Dependencies

```
Phase 1 (CMS Blocks) ──> Phase 2 (Registry + TotalCredits) ──> Phase 3 (Service + API) ──> Phase 4 (Frontend)
                                                                         │
                                                                         └──> Phase 5 (Tests)
```

All phases are sequential — each depends on the previous.

---

## Verification

1. `dotnet build src\Commerce.Demo.sln` — compiles
2. `dotnet test src\Commerce.Demo.Tests` — all tests pass
3. In CMS: create `SearchSettingsPage`, add `SortOptionBlock` items for Relevance, Price Low-High, Event Date, Title A-Z
4. `GET /api/products/search` — response includes `sortOptions` array matching CMS config
5. `GET /api/products/search?o=priceLowToHigh` — results sorted by price ascending
6. `GET /api/products/search?o=creditsDesc` — results sorted by total credits descending
7. `GET /api/products/search?o=invalidValue` — falls back to relevance (graceful degradation)
8. Disable a sort option in CMS → it disappears from `sortOptions` in API response
9. Change a label in CMS → label updates in API response
10. Frontend dropdown renders options from API, selecting one updates URL `o` param and re-sorts results

## Critical Existing Files (Reference)

| File | Why |
|------|-----|
| `Business/Features/Search/Enums/SortType.cs` | Current enum to be superseded by registry |
| `Business/Features/Search/Services/SearchService.cs` | Existing sort switch pattern (UnifiedSearch) — preserved unchanged |
| `ContentTypes/Cms/Blocks/SnippetBlock.cs` | Block pattern with SelectionFactory dropdown |
| `Core/Infrastructure/EditorDescriptors/EnumSelectionFactory.cs` | Pattern for `SortFieldSelectionFactory` (but uses ISelectionFactory directly, not enum-based) |
| `Business/Features/Search/Configuration/FacetFieldRegistry.cs` | Parallel registry pattern from Faceted Search plan |
| `ContentTypes/Cms/Pages/SearchSettingsPage.cs` | Settings page to extend with SortOptions ContentArea |
| `Core/Extensions/ProductContentExtensions.cs` | `GetListPrice()` — ServiceLocator pattern for computed properties |

## Doc Updates Required

| Document | Update |
|---|---|
| `docs/decisions.md` | ADR: registry-based sort replacing enum; ADR: TotalCredits computed property |
| `docs/architecture.md` | Sort configuration flow: CMS → SortFieldRegistry → ProductSearchService → API → SortDropdown |
| `docs/api.md` | `sortOptions` in response schema; `o` parameter with valid identifiers |
| `docs/devlog.md` | Session entries per phase |
| `docs/todo.md` | Mark phases complete |
