# Plan: Relevant Results and Ranking — Search Boosting Configuration

## Context

The search service (`SearchService.cs`) uses EPiServer Find's `UnifiedSearchFor()` for global search but has **no custom relevance boosting**. The `SortType.Relevance` case is a no-op — it falls through to Find's default scoring. This means product name matches aren't prioritized over body text matches, products don't rank above pages, and recency has no effect on relevance ordering.

This plan adds code-based field weighting, content type boosting, and recency boosting to improve search result quality for the fashion e-commerce domain.

## Approach: UnifiedSearch with `BoostMatching()`

**Stay with UnifiedSearch** (not typed search). Rationale:
- Global search spans all content types — UnifiedSearch is purpose-built for this
- Typed search would require multiple queries + result merging (significant complexity)
- `BoostMatching()` is supported on unified search for `SearchTitle`, `SearchPublishDate`, and `MatchTypeHierarchy()`
- No changes to `ISearchService` interface — all changes are internal to the implementation

**Configuration via Options Pattern** (`IOptions<SearchBoostOptions>`), consistent with existing `Configure<T>()` usage in Startup.cs.

## Field Weight Matrix

| Boost Target | Method | Default Value | Rationale |
|---|---|---|---|
| Title match | `BoostMatching(x => x.SearchTitle.Match(query))` | **3.0** | Highest-signal field — title matches should rank first |
| Products | `BoostMatching(x => x.MatchTypeHierarchy(typeof(FashionProduct)))` | **2.0** | Primary content users search for in e-commerce |
| Bundles/Packages | `MatchTypeHierarchy(typeof(FashionBundle))` + `typeof(FashionPackage)` | **1.5** | Purchasable items, less common than products |
| Categories | `MatchTypeHierarchy(typeof(FashionCategory))` | **1.2** | Navigation value, should not outrank products |
| Recent content | `BoostMatching(x => x.SearchPublishDate.After(recentDate))` | **1.3** | Content published within last 30 days boosted |

All values are configurable via `SearchBoostOptions` without code changes.

## Files to Change (4 files, ~130 lines net new)

### 1. NEW: `Commerce.Demo.Business/Features/Search/Configuration/SearchBoostOptions.cs`
- POCO options class with properties: `TitleMatchBoost`, `ProductTypeBoost`, `BundlePackageTypeBoost`, `CategoryTypeBoost`, `RecencyBoost`, `RecencyWindowDays`, `Enabled`
- Sensible defaults in property initializers (values from matrix above)
- ~45 lines

### 2. MODIFY: `Commerce.Demo.Business/Features/Search/Services/SearchService.cs`
- Add `IOptions<SearchBoostOptions>` + `ILogger<SearchService>` to constructor
- Add private `ApplyRelevanceBoosting(ITypeSearch<UnifiedSearchHit> search, string query)` method
- Call it in the `SortType.Relevance` switch case
- Graceful degradation: try/catch around boosting so misconfiguration never crashes search
- Add structured logging for search queries and results (INFO) and boosting details (DEBUG)
- ~30 lines net new (file goes from 64 to ~95 lines)

### 3. MODIFY: `Commerce.Demo.Web/Startup.cs`
- Add `.Configure<SearchBoostOptions>(o => { })` after the existing `FindCommerceOptions` block (~line 223)
- Add `using Commerce.Demo.Business.Features.Search.Configuration;`
- ~5 lines

### 4. NEW: `Commerce.Demo.Tests/Features/Search/SearchBoostOptionsTests.cs`
- Test default values are correct
- Test `Enabled = false` skips boosting
- Test empty query skips boosting
- ~80 lines

## Key Implementation Details

**Boosting only applies when sort = Relevance.** Alphabetical/Newest/Oldest are explicit orderings where boosting would be counterproductive.

**Recency is binary, not decayed.** UnifiedSearch doesn't support `DecayByDate()`. Content within `RecencyWindowDays` gets boosted; older content does not. Multi-tier recency can be added later if needed.

**Namespace note:** `SearchService.cs` uses namespace `Commerce.Demo.Optimizely.Core.Features.Search.Services` despite living in the Business project. The modification maintains this existing namespace.

**Content type references:** Business project already references ContentTypes (`Commerce.Demo.Business.csproj` line 11), so `FashionProduct`, `FashionCategory`, `FashionBundle`, `FashionPackage` types are directly accessible.

**No interface changes.** `ISearchService` contract is unchanged — consumers are unaffected.

## Implementation Sequence

1. Create `SearchBoostOptions.cs` (no dependencies)
2. Modify `SearchService.cs` (depends on step 1)
3. Modify `Startup.cs` registration (depends on step 1)
4. Create unit tests (depends on steps 1-3)

## Verification

1. `dotnet build src\Commerce.Demo.sln` — solution compiles
2. `dotnet test src\Commerce.Demo.Tests` — all tests pass (existing + new)
3. Manual: run site, search for a product name → product results should rank higher than category/page results
4. Manual: set `Enabled = false` in options → results revert to default Find scoring

## Doc Updates Required

| Document | Update |
|---|---|
| `docs/decisions.md` | ADR: UnifiedSearch boosting over typed search |
| `docs/architecture.md` | Add search boosting pipeline to data flow |
| `docs/devlog.md` | Session entry |
| `docs/todo.md` | Mark complete |
