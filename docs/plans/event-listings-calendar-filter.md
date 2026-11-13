# Plan: Event Listings & Highlights (Calendar within General Search)

## Context

Events currently appear as standard tiles in search results alongside books, subscriptions, and other products — the Product Search Tiles plan handles this. However, there is no way to filter by date. Users looking for upcoming events must scroll through all products or rely on text searches.

This plan adds a **calendar date filter** to the existing search filter panel — appearing alongside facets like Format, Practice Area, and Credit Type. It provides preset date ranges (Today, Tomorrow, This Week, Next Week, This Month) plus a custom date range picker. When a date filter is active, the system applies additional rules to exclude expired or unavailable events, and the results update in place using the same tile grid, pagination, sorting, and URL state as the rest of the search page.

This replaces the need for a separate calendar/event listing view. Event discovery is embedded directly in the primary search experience.

**Prerequisites assumed complete:** Faceted Search plan, Product Search Tiles plan, Search Sorting plan.

---

## Key Design Decisions

### 1. Date Filter Is Not a Standard Facet

Unlike categorical facets (Practice Area, Format), the date filter uses **range queries** — it's a date range applied to `C_EventDate` / `StartDateTime`, not a terms facet with discrete values. It cannot be a `FacetDefinitionBlock` in the existing facet ContentArea. Instead, it's a dedicated filter component with its own CMS configuration block (`DateFilterBlock` on `SearchSettingsPage`).

### 2. Preset Ranges + Custom Date Range

The filter offers two modes:
- **Presets:** Today, Tomorrow, This Week, Next Week, This Month — each maps to a calculated `dateFrom`/`dateTo` pair on the backend
- **Custom:** User picks explicit start and end dates via date inputs

This avoids the complexity of a full visual calendar grid while covering the most common event discovery patterns. A visual month-view calendar can be added as a future enhancement.

### 3. Date Filter Triggers Event-Only Mode

When any date filter is active, the search automatically adds a filter to show only event-type products (`EventProduct`, `MeetingProduct`) and excludes expired events. This is because date-based browsing is inherently about events — non-event products don't have meaningful event dates.

When the date filter is cleared, the search returns to showing all product types.

### 4. Event Expiration Logic

An event is considered **available** when:
- `EndDateTime >= DateTime.UtcNow` (event hasn't ended) — for EventProduct/MeetingProduct
- OR `C_EventDate >= DateTime.Today` (base product event date hasn't passed) — fallback for products without EndDateTime
- AND `StopPublish` is null or in the future (not unpublished)

This is applied as a `.Filter()` in the TypedSearch query alongside the date range filter.

### 5. URL State

Date filter state lives in URL query params alongside existing facet params:

| Param | Values | Example |
|-------|--------|---------|
| `dp` | Preset identifier: `today`, `tomorrow`, `thisWeek`, `nextWeek`, `thisMonth` | `dp=thisWeek` |
| `dateFrom` | ISO date string (custom range start) | `dateFrom=2026-03-01` |
| `dateTo` | ISO date string (custom range end) | `dateTo=2026-03-31` |

Rules:
- `dp` and `dateFrom`/`dateTo` are mutually exclusive — if `dp` is set, `dateFrom`/`dateTo` are ignored
- If neither is set, no date filter is active (all products shown)
- Custom range requires both `dateFrom` and `dateTo`

### 6. CMS Configuration

A `DateFilterBlock` on `SearchSettingsPage` controls:
- Whether the date filter is shown at all (`IsEnabled`)
- Which preset options are available (bitflag or multi-select)
- Display label for the filter section
- Whether custom range is allowed

---

## Date Range Calculations (Server-Side)

All calculations use the server's local time zone (or a configured marketplace time zone).

| Preset | `dateFrom` | `dateTo` |
|--------|------------|----------|
| `today` | Start of today | End of today |
| `tomorrow` | Start of tomorrow | End of tomorrow |
| `thisWeek` | Start of current week (Monday) | End of current week (Sunday) |
| `nextWeek` | Start of next Monday | End of next Sunday |
| `thisMonth` | 1st of current month | Last day of current month |

"Start of day" = `00:00:00`, "End of day" = `23:59:59`.

The backend resolves presets to concrete dates — the frontend never calculates date ranges. This ensures consistency regardless of user timezone differences.

---

## Backend Changes

### New Models

**`DateFilterPreset` enum** (`Business/Features/Search/Enums/DateFilterPreset.cs`):
```csharp
public enum DateFilterPreset
{
    Today,
    Tomorrow,
    ThisWeek,
    NextWeek,
    ThisMonth
}
```

**`DateFilterBlock`** (`ContentTypes/Cms/Blocks/DateFilterBlock.cs`):
| Property | Type | Notes |
|----------|------|-------|
| `DisplayLabel` | string | CultureSpecific — "Filter by Date" default |
| `IsEnabled` | bool | Default true |
| `AllowCustomRange` | bool | Default true |
| `AvailablePresets` | string | Comma-separated preset identifiers; if empty, all presets shown. Editor uses a checkbox list via a custom `DatePresetSelectionFactory` |
| `DefaultExpanded` | bool | Default false |

### `SearchSettingsPage` (MODIFY)

Add alongside existing `FacetDefinitions` and `SortOptions`:

```csharp
[Display(
    Name = "Date Filter Configuration",
    GroupName = Globals.Tabs.Search,
    Order = 300)]
[AllowedTypes(typeof(DateFilterBlock))]
public virtual ContentArea DateFilter { get; set; }
```

Only one `DateFilterBlock` is expected in this ContentArea (not a list). If empty, date filter is hidden.

### `ProductSearchCriteriaModel` (MODIFY)

Add date filter parameters:

```csharp
[FromQuery(Name = "dp")]
public string? DatePreset { get; set; }        // "today", "thisWeek", etc.

[FromQuery(Name = "dateFrom")]
public DateTime? DateFrom { get; set; }        // Custom range start (ISO format)

[FromQuery(Name = "dateTo")]
public DateTime? DateTo { get; set; }          // Custom range end (ISO format)
```

### `DateFilterService` (NEW — `Business/Features/Search/Services/DateFilterService.cs`)

Resolves presets to date ranges and applies filters to the search query. Single responsibility — separated from `ProductSearchService` for testability.

```csharp
public interface IDateFilterService
{
    DateRange? ResolveDateRange(string? preset, DateTime? dateFrom, DateTime? dateTo);
    ITypeSearch<MarketplaceProductBase> ApplyDateFilter(
        ITypeSearch<MarketplaceProductBase> search, DateRange range);
    ITypeSearch<MarketplaceProductBase> ApplyEventAvailabilityFilter(
        ITypeSearch<MarketplaceProductBase> search);
}
```

**`DateRange`** record:
```csharp
public record DateRange(DateTime From, DateTime To);
```

**`ApplyDateFilter`:**
```csharp
search = search.Filter(x => x.C_EventDate.InRange(range.From, range.To));
```

**`ApplyEventAvailabilityFilter`:**
When date filter is active, restrict to event types and exclude expired:
```csharp
search = search
    .Filter(x => x.MatchTypeHierarchy(typeof(EventProduct))
              | x.MatchTypeHierarchy(typeof(MeetingProduct)))
    .Filter(x => x.C_EventDate.After(DateTime.Today)
              | !x.C_EventDate.Exists());  // Allow events without explicit end
```

Note: `C_EventDate` on `MarketplaceProductBase` serves as the canonical event date for filtering and sorting. `EventProduct.StartDateTime`/`EndDateTime` are display fields. This simplifies the query since TypedSearch on `MarketplaceProductBase` can access `C_EventDate` directly without type-casting to `EventProduct`.

### `ProductSearchService` (MODIFY)

Add `IDateFilterService` to constructor injection. In `SearchProducts()`:

```csharp
// After building base query and applying facet filters...
var dateRange = _dateFilterService.ResolveDateRange(
    criteria.DatePreset, criteria.DateFrom, criteria.DateTo);

if (dateRange is not null)
{
    search = _dateFilterService.ApplyDateFilter(search, dateRange);
    search = _dateFilterService.ApplyEventAvailabilityFilter(search);
}
```

### API Response: Date Filter State

Add to `ProductSearchResponse`:

```csharp
public DateFilterStateResponse DateFilter { get; init; }
```

**`DateFilterStateResponse`:**
```csharp
public record DateFilterStateResponse
{
    public bool IsEnabled { get; init; }
    public string DisplayLabel { get; init; }
    public bool AllowCustomRange { get; init; }
    public bool DefaultExpanded { get; init; }
    public IEnumerable<DatePresetResponse> Presets { get; init; }
    public string ActivePreset { get; init; }       // Currently selected preset, or null
    public string ActiveDateFrom { get; init; }     // Currently active start date (ISO)
    public string ActiveDateTo { get; init; }       // Currently active end date (ISO)
}

public record DatePresetResponse
{
    public string Identifier { get; init; }         // "today", "thisWeek"
    public string Label { get; init; }              // "Today", "This Week"
    public bool IsSelected { get; init; }
}
```

---

## Frontend Changes

### `DateFilter.jsx` (NEW — `src/components/facets/DateFilter.jsx`)

A filter panel component rendered inside `FacetSidebar`, above or below the regular facet groups. Layout:

```
+------------------------------------+
| Filter by Date              [▾/▴]  |
+------------------------------------+
| ○ Today                            |
| ○ Tomorrow                         |
| ● This Week                        |
| ○ Next Week                        |
| ○ This Month                       |
|                                    |
| ─── Custom Range ───               |
| From: [2026-03-01]                 |
| To:   [2026-03-31]                 |
| [Apply]                            |
|                                    |
| [Clear date filter]                |
+------------------------------------+
```

- Preset options rendered as radio buttons (single-select — only one date range at a time)
- Custom range: two `<input type="date">` fields + Apply button
- "Clear date filter" link removes all date params
- Collapsible section with `defaultExpanded` from API

Props:
```javascript
{ dateFilter, activePreset, activeDateFrom, activeDateTo, onPresetChange, onCustomRangeApply, onClear }
```

### `useProductSearch.js` (MODIFY)

Add date filter state parsing and setter functions:

```javascript
// Parse URL params
const datePreset = searchParams.get('dp') || null
const dateFrom = searchParams.get('dateFrom') || null
const dateTo = searchParams.get('dateTo') || null

// Setters
const setDatePreset = useCallback((preset) => {
  setSearchParams(prev => {
    if (preset) {
      prev.set('dp', preset)
      prev.delete('dateFrom')
      prev.delete('dateTo')
    } else {
      prev.delete('dp')
    }
    prev.set('page', '1')
    return prev
  })
}, [setSearchParams])

const setCustomDateRange = useCallback((from, to) => {
  setSearchParams(prev => {
    prev.delete('dp')
    prev.set('dateFrom', from)
    prev.set('dateTo', to)
    prev.set('page', '1')
    return prev
  })
}, [setSearchParams])

const clearDateFilter = useCallback(() => {
  setSearchParams(prev => {
    prev.delete('dp')
    prev.delete('dateFrom')
    prev.delete('dateTo')
    prev.set('page', '1')
    return prev
  })
}, [setSearchParams])
```

Add to return: `datePreset, dateFrom, dateTo, setDatePreset, setCustomDateRange, clearDateFilter`

Add `dp`, `dateFrom`, `dateTo` to the `searchProducts` API call params.

### `productSearchApi.js` (MODIFY)

Add date params to the fetch URL builder:

```javascript
if (params.datePreset) url.searchParams.set('dp', params.datePreset)
if (params.dateFrom) url.searchParams.set('dateFrom', params.dateFrom)
if (params.dateTo) url.searchParams.set('dateTo', params.dateTo)
```

### `FacetSidebar.jsx` (MODIFY)

Add `DateFilter` component at the top of the sidebar, before facet groups:

```jsx
{data?.dateFilter?.isEnabled && (
  <DateFilter
    dateFilter={data.dateFilter}
    activePreset={datePreset}
    activeDateFrom={dateFrom}
    activeDateTo={dateTo}
    onPresetChange={setDatePreset}
    onCustomRangeApply={setCustomDateRange}
    onClear={clearDateFilter}
  />
)}
```

### `ActiveFilters.jsx` (MODIFY)

When a date filter is active, show it as a chip alongside facet chips:

```jsx
{datePreset && (
  <FilterChip
    label={`Date: ${presetLabel}`}
    onRemove={clearDateFilter}
  />
)}
{dateFrom && dateTo && (
  <FilterChip
    label={`Date: ${dateFrom} – ${dateTo}`}
    onRemove={clearDateFilter}
  />
)}
```

---

## Files to Change (Summary)

### New Files (7)
| File | Project | Lines |
|------|---------|-------|
| `Business/Features/Search/Enums/DateFilterPreset.cs` | Business | ~10 |
| `Business/Features/Search/Models/DateRange.cs` | Business | ~5 |
| `Business/Features/Search/Services/IDateFilterService.cs` | Business | ~10 |
| `Business/Features/Search/Services/DateFilterService.cs` | Business | ~90 |
| `ContentTypes/Cms/Blocks/DateFilterBlock.cs` | ContentTypes | ~40 |
| `Web/Api/Responses/Products/DateFilterStateResponse.cs` | Web | ~25 |
| `Frontend/src/components/facets/DateFilter.jsx` | Frontend | ~80 |

### Modified Files (7)
| File | Change | Lines |
|------|--------|-------|
| `ContentTypes/Cms/Pages/SearchSettingsPage.cs` | Add `DateFilter` ContentArea | ~5 |
| `Business/Features/Search/Models/ProductSearchCriteriaModel.cs` | Add `DatePreset`, `DateFrom`, `DateTo` params | ~10 |
| `Business/Features/Search/Services/ProductSearchService.cs` | Inject `IDateFilterService`, call in search pipeline | ~10 |
| `Web/Api/Responses/Products/ProductSearchResponse.cs` | Add `DateFilter` property | ~1 |
| `Web/Startup.cs` | Register `IDateFilterService` as scoped | ~2 |
| `Frontend/src/hooks/useProductSearch.js` | Add date state parsing + setter functions | ~35 |
| `Frontend/src/api/productSearchApi.js` | Add date params to fetch URL | ~3 |

### Modified Files (Frontend — from Product Search Tiles plan)
| File | Change | Lines |
|------|--------|-------|
| `Frontend/src/components/facets/FacetSidebar.jsx` | Render `DateFilter` above facet groups | ~8 |
| `Frontend/src/components/search/ActiveFilters.jsx` | Show date filter chip | ~10 |

### Test Files (2)
| File | Lines |
|------|-------|
| `Tests/Features/Search/DateFilterServiceTests.cs` | ~100 |
| `Tests/Features/Search/DateFilterPresetTests.cs` | ~50 |

**Total: 9 new files, 9 modified files, ~395 lines new code**

---

## Implementation Phases

### Phase 1: DateFilterBlock + CMS Configuration
**2 new + 1 modified, ~45 lines**

- NEW `ContentTypes/Cms/Blocks/DateFilterBlock.cs`
- NEW `Business/Features/Search/Enums/DateFilterPreset.cs`
- MODIFY `ContentTypes/Cms/Pages/SearchSettingsPage.cs` — add `DateFilter` ContentArea

### Phase 2: DateFilterService + Search Integration
**4 new + 3 modified, ~130 lines**

- NEW `Business/Features/Search/Models/DateRange.cs`
- NEW `Business/Features/Search/Services/IDateFilterService.cs`
- NEW `Business/Features/Search/Services/DateFilterService.cs`
- NEW `Web/Api/Responses/Products/DateFilterStateResponse.cs`
- MODIFY `Business/Features/Search/Models/ProductSearchCriteriaModel.cs` — add date params
- MODIFY `Business/Features/Search/Services/ProductSearchService.cs` — inject and call DateFilterService
- MODIFY `Web/Api/Responses/Products/ProductSearchResponse.cs` — add DateFilter property
- MODIFY `Web/Startup.cs` — register IDateFilterService

### Phase 3: Frontend DateFilter Component
**1 new + 3 modified, ~135 lines**

- NEW `Frontend/src/components/facets/DateFilter.jsx`
- MODIFY `Frontend/src/hooks/useProductSearch.js` — date state + setters
- MODIFY `Frontend/src/api/productSearchApi.js` — date params in fetch
- MODIFY `Frontend/src/components/facets/FacetSidebar.jsx` — render DateFilter
- MODIFY `Frontend/src/components/search/ActiveFilters.jsx` — date filter chip

### Phase 4: Tests
**2 new files, ~150 lines**

- NEW `Tests/Features/Search/DateFilterServiceTests.cs` — preset resolution, range calculations, edge cases (end of month, year boundary)
- NEW `Tests/Features/Search/DateFilterPresetTests.cs` — each preset produces correct date range

---

## Phase Dependencies

```
Phase 1 (CMS Config) ──> Phase 2 (Service + API) ──> Phase 3 (Frontend)
                                    │
                                    └──> Phase 4 (Tests)
```

Phases 3 and 4 can run in parallel after Phase 2.

---

## Edge Cases

| Scenario | Handling |
|----------|----------|
| `dateFrom` without `dateTo` (or vice versa) | Ignore incomplete custom range, no date filter applied |
| `dp=thisWeek` AND `dateFrom`/`dateTo` both set | Preset takes priority, custom range ignored |
| Invalid preset value (e.g., `dp=nextYear`) | Ignored, no date filter applied, log warning |
| No events match date range | Standard NoResults component shown |
| `C_EventDate` is null on a product | Product excluded from date-filtered results (only events with dates are returned) |
| Event spans multiple days (`StartDateTime` to `EndDateTime`) | Matched if any part of the event overlaps with the filter range |
| Time zone differences | Server resolves all presets using configured marketplace time zone |

---

## Verification

1. `dotnet build src\Commerce.Demo.sln` — compiles
2. `dotnet test src\Commerce.Demo.Tests` — all tests pass
3. `npm run build` + `npm run lint` — frontend builds clean
4. In CMS: add `DateFilterBlock` to `SearchSettingsPage` with all presets enabled
5. `GET /api/products/search` — response includes `dateFilter` with presets and config
6. `GET /api/products/search?dp=thisWeek` — returns only events this week, expired events excluded
7. `GET /api/products/search?dateFrom=2026-04-01&dateTo=2026-04-30` — returns events in April
8. `GET /api/products/search?dp=today&f.Format=Live` — combines date + facet filters correctly
9. `GET /api/products/search?dp=today&o=eventDateAsc` — date filter + sorting work together
10. Clear date filter → results return to all product types
11. Frontend: date filter presets render as radio buttons, selecting one updates URL and results
12. Frontend: custom date range with Apply button works
13. Frontend: date filter chip appears in ActiveFilters, clicking X clears it
14. Frontend: back button restores previous date filter state
15. Copy URL with date filter → open in new tab → same filtered results

## Critical Existing Files (Reference)

| File | Why |
|------|-----|
| `Business/Features/Search/Services/ProductSearchService.cs` | Search pipeline to integrate date filtering into |
| `Business/Features/Search/Models/ProductSearchCriteriaModel.cs` | Add date params alongside existing facet params |
| `ContentTypes/Cms/Pages/SearchSettingsPage.cs` | Add DateFilter ContentArea alongside FacetDefinitions and SortOptions |
| `ContentTypes/Cms/Blocks/FacetDefinitionBlock.cs` | Pattern for DateFilterBlock (CMS block with config properties) |
| `Frontend/src/hooks/useProductSearch.js` | Central hook to add date state management |
| `Frontend/src/components/facets/FacetSidebar.jsx` | Container to render DateFilter alongside facet groups |

## Doc Updates Required

| Document | Update |
|---|---|
| `docs/decisions.md` | ADR: date filter as separate component (not a facet); ADR: preset resolution on server not client |
| `docs/architecture.md` | Date filter flow: URL → ProductSearchCriteriaModel → DateFilterService → Find range query |
| `docs/api.md` | `dp`, `dateFrom`, `dateTo` params; `dateFilter` in response |
| `docs/devlog.md` | Session entries per phase |
| `docs/todo.md` | Mark phases complete |
