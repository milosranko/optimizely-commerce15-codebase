# Plan: Product Search Tiles

## Context

The two previous plans (Search Relevance Boosting and Faceted Search) established the backend search infrastructure: `IProductSearchService.SearchProducts()` with TypedSearch, `FacetFieldRegistry`, CMS-configurable `FacetDefinitionBlock`, `SearchBoostOptions`, and `FacetSelectionModelBinder`. This plan builds the **API response layer** and the **React frontend** that consumes it — the full search results experience with product tiles, facets, sorting, pagination, and URL-persisted state.

**Prerequisites assumed complete:** Search Relevance Boosting plan, Faceted Search plan (all 10 phases).

**Scope:** Full stack — backend API endpoint + response models + React 19 frontend. No MVC/Razor views.

---

## Key Design Decisions

### 1. Pre-Formatted Tile API Response

The API returns tile-specific data (formatted dates, absolute image URLs, display-name enums, truncated descriptions). The frontend is a thin rendering layer — no domain formatting logic. This keeps the API contract clean and the React components simple.

### 2. Tailwind CSS v4

Utility-first styling via the `@tailwindcss/vite` plugin. No `tailwind.config.js` needed — CSS-first configuration with `@import "tailwindcss"`.

### 3. React Router v7 + URL State

All search state (query, sort, page, facet selections) lives in URL search params via `useSearchParams`. Enables bookmarking, sharing, and back-button navigation. No Redux/Zustand needed.

### 4. Plain `fetch` API Client

No Axios or React Query. A custom `useProductSearch` hook wraps `fetch` with loading/error state and URL-to-API parameter mapping. Adequate for read-only search.

### 5. Inline SVGs for Icons

Stars, badges, and search icons use inline SVGs. No icon library dependency. Only a handful of icons are needed.

### 6. "Your Price" as Nullable Placeholder

`YourPrice` is included in the API response model as a nullable field. Actual personalization logic (user auth, membership lookup) is a future phase. The frontend conditionally renders it when present.

---

## API Response Shape

### `GET /api/products/search`

Query parameters (matches previous faceted search plan):
```
?q=ethics&o=relevance&p=1&ps=20&f.PracticeArea=CorporateLaw&f.Format=OnDemand
```

### Response Models (`Web/Api/Responses/Products/`)

**ProductSearchResponse** (top-level):
```
{
  results: ProductTileResponse[],
  facets: FacetResponse[],
  activeFilters: ActiveFilterResponse[],
  query: string,
  sortBy: string,
  page: number,
  pageSize: number,
  totalResults: number,
  totalPages: number,
  errors: string[] | null
}
```

**ProductTileResponse** (one tile):
| Field | Type | Source |
|-------|------|--------|
| `code` | string | Product code |
| `url` | string | Absolute URL to product detail page |
| `title` | string | `ShortDisplayName ?? Name` |
| `imageUrl` | string | Absolute URL with `?w=400` resize |
| `description` | string | `MarketingDescription` truncated to 160 chars, plain text |
| `format` | string | Format enum display name ("Live", "OnDemand", etc.) |
| `productLine` | string | ProductLine enum display name |
| `productType` | string | Concrete type name ("EventProduct", "BookProduct") |
| `eventDate` | DateTime? | `C_EventDate` for events, `ReleaseDate` for others |
| `eventDateFormatted` | string | Pre-formatted: "Mar 15, 2026" |
| `pricing` | ProductPriceResponse | Pricing sub-object |
| `rating` | double? | 0-5 from `Ratings` field |
| `badges` | ProductBadgesResponse | Badge flags |

**ProductPriceResponse:**
| Field | Type | Notes |
|-------|------|-------|
| `memberPrice` | decimal? | |
| `nonMemberPrice` | decimal? | |
| `yourPrice` | decimal? | Nullable — future personalization |
| `discountedPrice` | decimal? | Non-null when on sale |
| `currencyCode` | string | "USD" |

**ProductBadgesResponse:**
| Field | Type | Notes |
|-------|------|-------|
| `isUPass` | bool | |
| `isFeatured` | bool | |
| `isOnSale` | bool | |
| `isNewRelease` | bool | `ReleaseDate` within last 30 days |
| `productLineIcon` | string | Identifier for icon lookup ("cle", "cpe") |

**FacetResponse** (sidebar facet group):
```
{ fieldName, displayLabel, defaultExpanded, options: [{ value, label, count, selected }] }
```

**ActiveFilterResponse** (chip):
```
{ fieldName, displayLabel, value, valueLabel }
```

---

## Product Tile Layout

```
+------------------------------------------+
|  [Image - 4:3 ratio]        [Badge row]  |
|                              UPass  New   |
|                              Sale Feature |
+------------------------------------------+
|  [Format badge]     [Event date]         |
|  "Live"             Mar 15, 2026         |
|                                          |
|  [Title - 2-line clamp, linked]          |
|  Product Name Goes Here and May Be...    |
|                                          |
|  [Star Rating]                           |
|  ★★★★☆ (4.2)                            |
|                                          |
|  [Pricing]                               |
|  Member: $299  |  Non-Member: $399       |
|  Your Price: $249 (when available)       |
+------------------------------------------+
```

Star rating: 5-star visual with filled (yellow-400), half-filled (clip-path), empty (gray-300). Shows numeric value.

Badges: Small pill/tag elements positioned over the image corner. Color-coded per type.

---

## Frontend Component Tree

```
<BrowserRouter>
  <Routes>
    <Route path="/search" element={<SearchPage />} />
  </Routes>
</BrowserRouter>

<SearchPage>
  <SearchBar />
  <div className="flex">
    <FacetSidebar>
      <FacetGroup>
        <FacetCheckbox />
      </FacetGroup>
    </FacetSidebar>
    <main>
      <ResultsCount /> + <SortDropdown />
      <ActiveFilters />
      <LoadingOverlay>
        <ProductGrid>
          <ProductTile>
            <ProductBadges />
            <StarRating />
            <ProductPrice />
          </ProductTile>
        </ProductGrid>
      </LoadingOverlay>
      <NoResults />
      <Pagination />
    </main>
  </div>
</SearchPage>
```

## Frontend File Structure

```
src/
  main.jsx                              # BrowserRouter wrapper
  App.jsx                               # Route definitions
  index.css                             # @import "tailwindcss"
  api/
    productSearchApi.js                 # fetch wrapper for /api/products/search
  hooks/
    useProductSearch.js                 # Central search hook (URL state + fetch)
  pages/
    SearchPage.jsx                      # Full page assembly
  components/
    search/
      SearchBar.jsx                     # Keyword input + submit
      SortDropdown.jsx                  # Sort selector
      ResultsCount.jsx                  # "X results for 'query'"
      ActiveFilters.jsx                 # Filter chips with remove
    products/
      ProductTile.jsx                   # Single tile card
      ProductGrid.jsx                   # Responsive tile grid
      ProductBadges.jsx                 # Badge/icon row
      StarRating.jsx                    # 5-star visual
      ProductPrice.jsx                  # Price display
    facets/
      FacetSidebar.jsx                  # Sidebar container
      FacetGroup.jsx                    # Collapsible facet section
      FacetCheckbox.jsx                 # Checkbox + count
    layout/
      Pagination.jsx                    # Page nav with ellipsis
      NoResults.jsx                     # Empty state + suggestions
      LoadingOverlay.jsx                # Loading indicator
```

---

## `useProductSearch` Hook (Central State)

The hook reads URL params via React Router's `useSearchParams`, calls the API on change, and exposes helper functions that update URL params (triggering re-fetch):

| Export | Purpose |
|--------|---------|
| `data` | API response (`ProductSearchResponse`) |
| `loading` | Boolean fetch state |
| `error` | Error message string |
| `query`, `sort`, `page`, `pageSize`, `facets` | Parsed from URL |
| `setQuery(q)` | Updates `q` param, resets to page 1 |
| `setSort(s)` | Updates `sort` param, resets to page 1 |
| `setPage(p)` | Updates `page` param |
| `toggleFacet(field, value)` | Adds/removes facet selection, resets to page 1 |
| `removeFacet(field, value)` | Removes specific facet selection |
| `clearAllFilters()` | Removes all `f.*` params |

URL state contract:
```
/search?q=ethics&sort=relevance&page=1&ps=20&f.PracticeArea=CorporateLaw&f.Format=OnDemand
```

---

## Backend: SortType Extension

Add to existing `SortType` enum (`Business/Features/Search/Enums/SortType.cs`):
```csharp
PriceLowToHigh,    // NEW
PriceHighToLow     // NEW
```

Requires a computed `ListPrice` property on `MarketplaceProductBase` (same `ServiceLocator` pattern as `AvailableCreditTypes` and existing `ProductContentExtensions.GetListPrice()`), indexed by Find convention, so `OrderBy`/`OrderByDescending` works at the search engine level.

---

## Backend: ProductSearchTileMapper

**`Web/Api/Services/ProductSearchTileMapper.cs`** — manual projection (not Mapster, since this involves domain logic):

| Mapping | Logic |
|---------|-------|
| Title | `product.ShortDisplayName ?? product.Name` |
| ImageUrl | `UrlResolver.GetUrl(product.CommerceMediaCollection?.FirstOrDefault()) + "?w=400"` → absolute |
| Description | XhtmlString → plain text → truncate 160 chars |
| EventDate | `product is EventProduct ep ? ep.StartDateTime : product.ReleaseDate` |
| Format | Enum → display name via localization or `.ToString()` |
| Pricing | `IPriceService.GetDefaultPrice()` following existing `ProductContentExtensions.GetListPrice()` pattern |
| IsNewRelease | `product.ReleaseDate > DateTime.Now.AddDays(-30)` |
| ProductType | `product.GetType().Name` |

---

## Implementation Phases

### Phase A: Frontend Foundation (Tailwind + React Router + Vite Proxy)
**5 files modified/deleted, ~30 lines**

- Install: `npm install react-router` + `npm install -D tailwindcss @tailwindcss/vite`
- MODIFY `vite.config.js` — add Tailwind plugin + `/api` proxy to .NET backend
- MODIFY `main.jsx` — wrap with `<BrowserRouter>`
- REPLACE `App.jsx` — route definitions (`/search` → SearchPage)
- REPLACE `index.css` — `@import "tailwindcss"`
- DELETE `App.css`, `src/assets/react.svg`, `public/vite.svg`

### Phase B: API Response Models (Backend)
**7 new files, ~120 lines**

- `Web/Api/Responses/Products/ProductTileResponse.cs`
- `Web/Api/Responses/Products/ProductPriceResponse.cs`
- `Web/Api/Responses/Products/ProductBadgesResponse.cs`
- `Web/Api/Responses/Products/FacetResponse.cs`
- `Web/Api/Responses/Products/FacetOptionResponse.cs`
- `Web/Api/Responses/Products/ProductSearchResponse.cs`
- `Web/Api/Responses/Products/ActiveFilterResponse.cs`

### Phase C: SortType Extension + ListPrice Property (Backend)
**2 files modified, ~25 lines**

- MODIFY `Business/Features/Search/Enums/SortType.cs` — add `PriceLowToHigh`, `PriceHighToLow`
- MODIFY `ContentTypes/Commerce/Products/Base/MarketplaceProductBase.cs` — add `ListPrice` computed property

### Phase D: Tile Mapper Service (Backend)
**2 new + 1 modified, ~180 lines**

- NEW `Web/Api/Services/IProductSearchTileMapper.cs` (~15 lines)
- NEW `Web/Api/Services/ProductSearchTileMapper.cs` (~165 lines)
- MODIFY `Web/Startup.cs` — register `IProductSearchTileMapper` as scoped

### Phase E: Search Endpoint (Backend)
**1 new + 1 modified, ~80 lines**

- NEW `Web/Api/ProductSearchEndpoints.cs` (~70 lines)
- MODIFY `Web/Startup.cs` — `endpoints.AddProductSearchEndpoints()`

### Phase F: API Client + Search Hook (Frontend)
**2 new files, ~150 lines**

- NEW `src/api/productSearchApi.js` (~30 lines)
- NEW `src/hooks/useProductSearch.js` (~120 lines)

### Phase G: Core Tile Components (Frontend)
**5 new files, ~200 lines**

- NEW `src/components/products/StarRating.jsx` (~45 lines)
- NEW `src/components/products/ProductPrice.jsx` (~40 lines)
- NEW `src/components/products/ProductBadges.jsx` (~35 lines)
- NEW `src/components/products/ProductTile.jsx` (~55 lines)
- NEW `src/components/products/ProductGrid.jsx` (~25 lines)

### Phase H: Search + Facet Components (Frontend)
**5 new files, ~230 lines**

- NEW `src/components/search/SearchBar.jsx` (~40 lines)
- NEW `src/components/search/SortDropdown.jsx` (~40 lines)
- NEW `src/components/facets/FacetCheckbox.jsx` (~25 lines)
- NEW `src/components/facets/FacetGroup.jsx` (~50 lines)
- NEW `src/components/facets/FacetSidebar.jsx` (~75 lines)

### Phase I: Page Assembly (Frontend)
**5 new files, ~255 lines**

- NEW `src/components/search/ResultsCount.jsx` (~15 lines)
- NEW `src/components/search/ActiveFilters.jsx` (~40 lines)
- NEW `src/components/layout/Pagination.jsx` (~65 lines)
- NEW `src/components/layout/NoResults.jsx` (~35 lines)
- NEW `src/components/layout/LoadingOverlay.jsx` (~20 lines)
- NEW `src/pages/SearchPage.jsx` (~80 lines — assembles all components)

### Phase J: Backend Tests
**2 new files, ~120 lines**

- NEW `Tests/Api/ProductSearchTileMapperTests.cs` (~80 lines)
- NEW `Tests/Api/ProductSearchEndpointTests.cs` (~40 lines)

---

## Phase Dependencies

```
Phase A (Frontend Foundation) ──────> Phase F (API Client) ──> Phase G (Tiles) ──> Phase H (Search+Facets) ──> Phase I (Page)
                                                                                                                    │
Phase B (Response Models) ──> Phase D (Tile Mapper) ──> Phase E (Endpoint) ──> Phase J (Tests) ─────────────────────┘
                                  │
Phase C (SortType + ListPrice) ───┘
```

Frontend phases F-I can proceed with mock data while backend phases B-E are in progress. Phase I depends on all prior phases for full integration.

---

## Verification

1. `dotnet build src\Commerce.Demo.sln` — compiles
2. `dotnet test src\Commerce.Demo.Tests` — all tests pass
3. `npm run build` in Frontend — Tailwind + React builds without errors
4. `npm run lint` in Frontend — no ESLint violations
5. Start .NET backend + `npm run dev` — Vite proxy routes `/api/*` to backend
6. Navigate to `/search` — search page renders with search bar, empty state
7. Enter search query — tiles render with image, title, date, format, pricing, rating, badges
8. Click facet checkboxes — results filter, URL updates, back button works
9. Change sort — results reorder
10. Paginate — page changes, URL updates
11. Copy URL, open in new tab — same search state restored
12. Search with no results — NoResults component with suggestions shown

## Critical Existing Files (Reference)

| File | Why |
|------|-----|
| `Web/Api/Endpoints.cs` | Minimal API pattern to follow |
| `Web/Api/Responses/Base/ResponseBase.cs` | Base class with `Errors` |
| `Web/Api/Responses/Base/ResponseList.cs` | Pagination pattern |
| `Core/Extensions/ProductContentExtensions.cs` | `GetListPrice()` — pricing via `IPriceService` |
| `Web/Models/Commerce/VariantsViewModel.cs` | Existing pricing fields (member/non-member/discounted) |
| `Frontend/vite.config.js` | Extend with Tailwind + proxy |
| `Frontend/src/main.jsx` | Wrap with BrowserRouter |
| `Business/Features/Search/Enums/SortType.cs` | Extend with price sorts |

## Doc Updates Required

| Document | Update |
|---|---|
| `docs/api.md` | `GET /api/products/search` with full response schema |
| `docs/architecture.md` | Frontend component tree, API → tile mapper → search service flow |
| `docs/decisions.md` | ADR: pre-formatted tile API vs raw content; ADR: Tailwind CSS; ADR: React Router URL state |
| `docs/stack.md` | Tailwind CSS v4, React Router v7 |
| `docs/devlog.md` | Session entries per phase |
| `docs/todo.md` | Mark phases complete |
