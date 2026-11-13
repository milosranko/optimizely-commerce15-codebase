# Plan: Home Page (StartPage Content Type)

## Context

The current `StartPage` is a minimal demo page with `Heading`, `LeadText`, and two generic ContentAreas (`MainContentArea` restricted to `TeaserBlock`/`CarouselContainerBlock`, and an unrestricted `AnotherMainContentArea`). The view is a bare-bones placeholder. The marketplace needs a fully CMS-configurable homepage that introduces key offerings, highlights upcoming events, provides entry points into product categories, and surfaces a prominent search bar.

This plan redesigns the `StartPage` content type, adds 9 new block types for homepage composition, creates server-rendered Razor views for all blocks, and adds a `FeaturedContentService` for dynamic blocks that query the catalog.

**Prerequisites assumed complete:** Faceted Search plan (Phase 2: `MarketplaceProductBase`, `EventProduct`, `BookProduct`, etc.).

**Scope:** Backend content types + Razor views + service layer. No React components — the homepage is server-rendered via Optimizely's standard MVC pipeline.

---

## Key Design Decisions

### 1. Server-Rendered Homepage (Razor, Not React)

The homepage uses Optimizely's standard MVC/Razor rendering pipeline. No React components on the homepage. The search page at `/search` is the React SPA (per Product Search Tiles plan); the homepage is a traditional server-rendered page with CMS blocks. This keeps the homepage fast (no JavaScript framework overhead), fully CMS-editable with inline editing support, and consistent with the existing block rendering infrastructure.

### 2. ContentArea Composition

Instead of rigid layout slots, the homepage uses ContentAreas where editors compose blocks in any order. Optimizely's `[AllowedTypes]` restricts which blocks can be placed in each area. Editors control what appears, in what order, and can add/remove blocks without developer involvement.

### 3. Visitor Group Targeting (OOTB)

Audience segmentation uses Optimizely's built-in Visitor Groups. Editors can target individual blocks within ContentAreas to specific visitor groups (all users, logged-in members, specific practice areas) through the standard Optimizely personalization UI. No custom targeting implementation needed.

### 4. Publish/Unpublish Scheduling (OOTB)

Auto-expiration of promotions uses Optimizely's native content scheduling. Editors set publish and unpublish dates on blocks. Outdated promotions disappear automatically. No custom expiration logic needed.

### 5. Dynamic Blocks Query IContentLoader, Not Find

`FeaturedEventsBlock` and `FeaturedProductsBlock` query the content repository via `IContentLoader` + `IContentRepository`, not through EPiServer Find. This avoids a Find dependency for homepage rendering, works with the content cache, and is consistent with how Optimizely recommends loading known content. The `FeaturedContentService` applies in-memory filtering and sorting.

### 6. Search Form, Not React Component

The homepage search bar is a plain HTML `<form action="/search">` that navigates to the React search page with the query parameter. No JavaScript overhead for what is fundamentally a navigation action. The placeholder text is CMS-configurable.

### 7. CDN Image Pattern (Existing Convention)

All image-bearing blocks follow the existing `CarouselItemBlock` pattern: Cloudflare CDN image URLs with responsive `srcset` (480w–1920w), `loading="lazy"`, `decoding="async"`, and alt text extracted from `ImageFileBase.AlternateText`. This is already proven in the codebase.

### 8. BEM CSS Convention

Block views use BEM CSS class names following the existing `carousel-container` / `carousel-item` convention. CSS lives in a new `wwwroot/css/blocks.css` stylesheet loaded by the layout. Tailwind CSS is reserved for the React frontend only.

---

## StartPage Redesign

### Modified Properties

| Property | Type | Tab | Order | Notes |
|----------|------|-----|-------|-------|
| `ShowSearchBar` | bool | Content | 100 | Default: true. Toggles prominent search bar |
| `SearchPlaceholderText` | string | Content | 110 | Default: "Search CLEs, products, practice areas..." |
| `HeroArea` | ContentArea | Content | 120 | Restricted: `CarouselContainerBlock`, `HeroBannerBlock` |
| `MainContentArea` | ContentArea | Content | 130 | All marketplace block types allowed |
| `AnnouncementBar` | `AnnouncementBarBlock` | Announcements | 200 | Nested local block — always renders at top |

**Removed properties:** `Heading`, `LeadText`, `AnotherMainContentArea` (replaced by new structure).

### Homepage Layout (View Template)

```
+----------------------------------------------------------+
|  [Announcement Bar]  (from AnnouncementBar property)      |
|  "New: 2026 CLE Schedule Now Available"   [Learn More →]  |
+----------------------------------------------------------+
|  [Search Bar]  (if ShowSearchBar = true)                  |
|  🔍 Search CLEs, products, practice areas...              |
+----------------------------------------------------------+
|  [Hero Area]  (ContentArea: carousel or single banner)    |
|                                                           |
|  ┌─────────────────────────────────────────────────────┐  |
|  │  [Background Image]                                 │  |
|  │                                                     │  |
|  │  Featured CLE Program Title                         │  |
|  │  Supporting description text                        │  |
|  │  [ Register Now → ]                                 │  |
|  └─────────────────────────────────────────────────────┘  |
+----------------------------------------------------------+
|  [Main Content Area]  (editor-composed blocks)            |
|                                                           |
|  ┌── Featured Events Block ────────────────────────────┐  |
|  │  Upcoming Events                    [View All →]    │  |
|  │  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐              │  |
|  │  │Event1│ │Event2│ │Event3│ │Event4│              │  |
|  │  └──────┘ └──────┘ └──────┘ └──────┘              │  |
|  └─────────────────────────────────────────────────────┘  |
|                                                           |
|  ┌── Topic Grid Block ─────────────────────────────────┐  |
|  │  Browse by Practice Area                            │  |
|  │  ┌─────────┐ ┌─────────┐ ┌─────────┐              │  |
|  │  │Corporate│ │Real     │ │Criminal │              │  |
|  │  │  Law    │ │ Estate  │ │  Law    │              │  |
|  │  └─────────┘ └─────────┘ └─────────┘              │  |
|  └─────────────────────────────────────────────────────┘  |
|                                                           |
|  ┌── Featured Products Block ──────────────────────────┐  |
|  │  New Books & Publications               [View All →]│  |
|  │  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐              │  |
|  │  │Book1 │ │Book2 │ │Form1 │ │Book3 │              │  |
|  │  └──────┘ └──────┘ └──────┘ └──────┘              │  |
|  └─────────────────────────────────────────────────────┘  |
|                                                           |
|  ┌── Content Cards ────────────────────────────────────┐  |
|  │  ┌──────────┐  ┌──────────┐  ┌──────────┐         │  |
|  │  │Membership│  │ Seasonal │  │ Member   │         │  |
|  │  │ Benefits │  │  Promo   │  │Spotlight │         │  |
|  │  └──────────┘  └──────────┘  └──────────┘         │  |
|  └─────────────────────────────────────────────────────┘  |
|                                                           |
|  ┌── Quick Links Block ────────────────────────────────┐  |
|  │  Popular Resources                                  │  |
|  │  📄 CLE Calendar  📚 Publications  📋 Online Forms │  |
|  └─────────────────────────────────────────────────────┘  |
+----------------------------------------------------------+
```

---

## New Block Types

### 1. HeroBannerBlock

Full-width hero banner with background image, headline, and call-to-action.

| Property | Type | Notes |
|----------|------|-------|
| `BackgroundImage` | ContentReference (`UIHint.Image`) | Full-width responsive image (Required) |
| `Heading` | string | Main headline (Required, max 80 chars) |
| `SubHeading` | string (Textarea) | Supporting text |
| `CtaLink` | LinkBlock | Call-to-action button |
| `TextAlignment` | `TextAlignmentEnum` | Left, Center, Right (default: Left) |
| `OverlayOpacity` | int | 0–100, darkens image for text readability (default: 40) |

**Rendering:** Server-rendered Razor partial. Responsive `srcset` image with text overlay. Edit mode shows `PropertyFor` for inline editing.

---

### 2. AnnouncementBarBlock (Local)

Thin announcement strip rendered at the top of the page. Nested property on `StartPage` (not in ContentArea).

| Property | Type | Notes |
|----------|------|-------|
| `Message` | string | Short announcement text (Required, max 120 chars) |
| `LinkButton` | LinkBlock | Optional CTA |
| `ColorScheme` | `AnnouncementColorEnum` | Info (blue), Warning (amber), Success (green), Neutral (gray) |
| `IsEnabled` | bool | Toggle visibility without removing content |

**Note:** `AvailableInEditMode = false` — this is a nested local block on StartPage, not placed in ContentAreas.

---

### 3. ContentCardBlock

Versatile card block for promotional content, membership benefits, and announcements.

| Property | Type | Notes |
|----------|------|-------|
| `Image` | ContentReference (`UIHint.Image`) | Card image |
| `Heading` | string | Card title (Required) |
| `Description` | XhtmlString | Rich text body |
| `LinkButton` | LinkBlock | CTA button |
| `CardStyle` | `CardStyleEnum` | Default, Highlighted, Compact |

**Rendering:** Card with optional image, heading, rich text, and CTA. CardStyle controls visual emphasis (border color, background).

---

### 4. QuickLinksBlock

Section of categorized navigation links.

| Property | Type | Notes |
|----------|------|-------|
| `Heading` | string | Section heading |
| `Description` | string (Textarea) | Optional section description |
| `Links` | ContentArea (`[AllowedTypes(typeof(LinkItemBlock))]`) | Ordered link items |
| `Layout` | `QuickLinksLayoutEnum` | Horizontal, Grid |

---

### 5. LinkItemBlock

Single navigation link with icon and description. Used inside `QuickLinksBlock`.

| Property | Type | Notes |
|----------|------|-------|
| `Icon` | ContentReference (`UIHint.Image`) | Small icon image |
| `LinkText` | string | Display text (Required) |
| `LinkUrl` | Url | Target URL (Required) |
| `Description` | string (Textarea) | Short description |

---

### 6. TopicGridBlock

Grid of practice area or topic tiles for category navigation.

| Property | Type | Notes |
|----------|------|-------|
| `Heading` | string | Section heading |
| `Description` | string (Textarea) | Optional section description |
| `Topics` | ContentArea (`[AllowedTypes(typeof(TopicTileBlock))]`) | Topic tiles |
| `ColumnsPerRow` | int | 3 or 4 columns (default: 3) |

---

### 7. TopicTileBlock

Single topic/practice area tile. Used inside `TopicGridBlock`.

| Property | Type | Notes |
|----------|------|-------|
| `Icon` | ContentReference (`UIHint.Image`) | Topic icon |
| `Title` | string | Topic name (Required) |
| `Description` | string (Textarea) | Short description |
| `LinkUrl` | Url | Target URL — typically `/search?f.PracticeArea={value}` |

---

### 8. FeaturedEventsBlock (Dynamic)

Dynamically pulls upcoming events from the catalog. Rendered via ViewComponent with service injection.

| Property | Type | Notes |
|----------|------|-------|
| `Heading` | string | Section heading (default: "Upcoming Events") |
| `MaxItems` | int | Number of events to display (default: 4) |
| `DaysAhead` | int | Look-ahead window in days (default: 90) |
| `FallbackMessage` | string | Message when no upcoming events found |
| `ShowViewAllLink` | bool | Show "View All Events" link |
| `ViewAllLinkUrl` | Url | URL for "View All" — typically `/search?f.Format=Live` |

**Rendering:** ViewComponent queries `IFeaturedContentService.GetUpcomingEvents()`, returns event cards with title, date, format, and CTA.

---

### 9. FeaturedProductsBlock (Dynamic)

Dynamically pulls products from the catalog based on configurable filters.

| Property | Type | Notes |
|----------|------|-------|
| `Heading` | string | Section heading |
| `MaxItems` | int | Number of products to display (default: 4) |
| `ProductTypeFilter` | string (SelectionFactory) | Filter by product type name (e.g., "BookProduct") |
| `FeaturedOnly` | bool | Only show products with `IsFeatured = true` |
| `FallbackMessage` | string | Message when no products match |
| `ShowViewAllLink` | bool | Show "View All" link |
| `ViewAllLinkUrl` | Url | URL for "View All" |

**Rendering:** ViewComponent queries `IFeaturedContentService.GetFeaturedProducts()`, returns product cards with title, image, format, and price.

---

## Enums

All enums use the existing `EnumEditorDescriptor<TEnum>` + `EnumSelectionFactory<TEnum>` pattern.

| Enum | Values | Location |
|------|--------|----------|
| `TextAlignmentEnum` | Left, Center, Right | `ContentTypes/Cms/Blocks/Enums/` |
| `AnnouncementColorEnum` | Info, Warning, Success, Neutral | `ContentTypes/Cms/Blocks/Enums/` |
| `CardStyleEnum` | Default, Highlighted, Compact | `ContentTypes/Cms/Blocks/Enums/` |
| `QuickLinksLayoutEnum` | Horizontal, Grid | `ContentTypes/Cms/Blocks/Enums/` |

---

## FeaturedContentService

**`Business/Features/Homepage/Interfaces/IFeaturedContentService.cs`**
**`Business/Features/Homepage/Services/FeaturedContentService.cs`**

Encapsulates catalog queries for dynamic homepage blocks. Uses `IContentLoader` (not Find) for cache-friendly content loading.

### `GetUpcomingEvents(int maxItems, int daysAhead)`

1. Load all children of the catalog root that are `EventProduct`
2. Filter: `StartDateTime > DateTime.Now` AND `StartDateTime < DateTime.Now.AddDays(daysAhead)`
3. Order by `StartDateTime` ascending (soonest first)
4. Take `maxItems`
5. Return `IEnumerable<EventProduct>`

### `GetFeaturedProducts(int maxItems, string? productTypeFilter, bool featuredOnly)`

1. Load all children of the catalog root that are `MarketplaceProductBase`
2. Filter by concrete type name if `productTypeFilter` is set
3. Filter by `IsFeatured == true` if `featuredOnly` is set
4. Order by `Created` descending (newest first)
5. Take `maxItems`
6. Return `IEnumerable<MarketplaceProductBase>`

### Catalog Root Discovery

Uses `IReferenceConverter.GetRootLink()` to find the commerce catalog root, then `IContentLoader.GetDescendents()` combined with `IContentLoader.GetItems<T>()` for efficient batch loading.

### Caching

`IContentLoader` responses are automatically cached by Optimizely's content cache. No additional caching layer needed for typical homepage traffic. If performance requires it, a cache decorator can be added later (following the existing `SiteSettingsManagerCacheDecorator` pattern).

---

## ProductTypeSelectionFactory

**`Core/Infrastructure/EditorDescriptors/ProductTypeSelectionFactory.cs`** (~25 lines)

`ISelectionFactory` that returns the concrete product type names for the `FeaturedProductsBlock.ProductTypeFilter` dropdown:

```
[{ Text: "(All)", Value: "" },
 { Text: "Event Product", Value: "EventProduct" },
 { Text: "Book Product", Value: "BookProduct" },
 { Text: "Subscription Product", Value: "SubscriptionProduct" },
 { Text: "Material Product", Value: "MaterialProduct" },
 { Text: "Meeting Product", Value: "MeetingProduct" },
 { Text: "Online Form Product", Value: "OnlineFormProduct" },
 { Text: "Law Office Video Product", Value: "LawOfficeVideoProduct" }]
```

---

## Implementation Phases

### Phase 1: Block Enums
**4 new files, ~30 lines total**

- `ContentTypes/Cms/Blocks/Enums/TextAlignmentEnum.cs` (~8 lines)
- `ContentTypes/Cms/Blocks/Enums/AnnouncementColorEnum.cs` (~8 lines)
- `ContentTypes/Cms/Blocks/Enums/CardStyleEnum.cs` (~8 lines)
- `ContentTypes/Cms/Blocks/Enums/QuickLinksLayoutEnum.cs` (~6 lines)

### Phase 2: Hero & Promotional Block Types
**3 new files, ~150 lines**

- `ContentTypes/Cms/Blocks/HeroBannerBlock.cs` (~60 lines)
- `ContentTypes/Cms/Blocks/Local/AnnouncementBarBlock.cs` (~40 lines)
- `ContentTypes/Cms/Blocks/ContentCardBlock.cs` (~50 lines)

### Phase 3: Navigation & Topic Block Types
**4 new files, ~165 lines**

- `ContentTypes/Cms/Blocks/QuickLinksBlock.cs` (~45 lines)
- `ContentTypes/Cms/Blocks/LinkItemBlock.cs` (~35 lines)
- `ContentTypes/Cms/Blocks/TopicGridBlock.cs` (~45 lines)
- `ContentTypes/Cms/Blocks/TopicTileBlock.cs` (~40 lines)

### Phase 4: StartPage Content Type Modification
**1 modified file, ~50 lines net change**

- MODIFY `ContentTypes/Cms/Pages/StartPage.cs` — replace `Heading`/`LeadText`/`MainContentArea`/`AnotherMainContentArea` with `ShowSearchBar`, `SearchPlaceholderText`, `HeroArea`, `MainContentArea` (expanded AllowedTypes), and nested `AnnouncementBar` property on a new Announcements tab

### Phase 5: Hero & Promotional Block Views
**3 new files, ~230 lines**

- `Views/Shared/Blocks/HeroBannerBlock.cshtml` (~100 lines — responsive srcset image, text overlay, CTA, edit mode support)
- `Views/Shared/Blocks/AnnouncementBarBlock.cshtml` (~50 lines — color-coded bar with optional link)
- `Views/Shared/Blocks/ContentCardBlock.cshtml` (~80 lines — card with image, heading, rich text, CTA, style variants)

### Phase 6: Navigation & Topic Block Views
**4 new files, ~200 lines**

- `Views/Shared/Blocks/QuickLinksBlock.cshtml` (~50 lines)
- `Views/Shared/Blocks/LinkItemBlock.cshtml` (~30 lines)
- `Views/Shared/Blocks/TopicGridBlock.cshtml` (~60 lines — responsive grid with configurable columns)
- `Views/Shared/Blocks/TopicTileBlock.cshtml` (~40 lines — icon + title + description + link)

### Phase 7: StartPage View + Search Form
**1 modified + 1 new file, ~120 lines**

- MODIFY `Views/StartPage/Index.cshtml` (~80 lines — new layout: announcement bar → search form → hero area → main content area)
- NEW `Views/Shared/_SearchBar.cshtml` (~40 lines — reusable search form partial: `<form action="/search">` with configurable placeholder)

### Phase 8: Dynamic Block Content Types + SelectionFactory
**3 new files, ~140 lines**

- `ContentTypes/Cms/Blocks/FeaturedEventsBlock.cs` (~55 lines)
- `ContentTypes/Cms/Blocks/FeaturedProductsBlock.cs` (~60 lines)
- `Core/Infrastructure/EditorDescriptors/ProductTypeSelectionFactory.cs` (~25 lines)

### Phase 9: FeaturedContentService
**2 new files, ~180 lines**

- `Business/Features/Homepage/Interfaces/IFeaturedContentService.cs` (~15 lines)
- `Business/Features/Homepage/Services/FeaturedContentService.cs` (~165 lines — `GetUpcomingEvents`, `GetFeaturedProducts`, catalog root discovery, logging)

### Phase 10: Dynamic Block ViewComponents + Views
**4 new files, ~260 lines**

- `Web/Components/FeaturedEventsBlockComponent.cs` (~40 lines)
- `Web/Components/FeaturedProductsBlockComponent.cs` (~40 lines)
- `Views/Shared/Blocks/FeaturedEventsBlock.cshtml` (~90 lines — event cards with date, format, title, CTA)
- `Views/Shared/Blocks/FeaturedProductsBlock.cshtml` (~90 lines — product cards with image, title, format, price)

### Phase 11: DI Registration + CSS Stylesheet
**1 modified + 1 new file, ~80 lines**

- MODIFY `Web/Startup.cs` (~5 lines — register `IFeaturedContentService` as scoped)
- NEW `wwwroot/css/blocks.css` (~75 lines — BEM styles for all new blocks: hero-banner, announcement-bar, content-card, quick-links, topic-grid, featured-events, featured-products)

### Phase 12: Tests
**3 new files, ~200 lines**

- `Tests/Features/Homepage/FeaturedContentServiceTests.cs` (~120 lines — mock IContentLoader, test event filtering/sorting, test product type filtering)
- `Tests/Features/Homepage/AnnouncementBarBlockTests.cs` (~40 lines — test IsEnabled toggle, empty link handling)
- `Tests/Features/Homepage/HeroBannerBlockTests.cs` (~40 lines — test overlay defaults, text alignment)

---

## Phase Dependencies

```
Phase 1 (Enums) ──┐
                   ├──> Phase 2 (Hero/Promo blocks) ──> Phase 5 (Hero/Promo views)
                   ├──> Phase 3 (Nav/Topic blocks) ──> Phase 6 (Nav/Topic views)
                   │
                   └──> Phase 4 (StartPage modify) ──> Phase 7 (StartPage view)
                                                              │
Phase 8 (Dynamic block types) ──> Phase 9 (Service) ──> Phase 10 (Components + views)
                                                              │
Phase 11 (DI + CSS) ←────────────────────────────────────────┘
       │
Phase 12 (Tests) ←───────────────────────────────────────────┘
```

- Phases 2–3 can run in parallel (both depend only on Phase 1)
- Phases 5–6 can run in parallel
- Phase 4 requires Phase 2 (HeroBannerBlock referenced in AllowedTypes)
- Phase 7 requires Phases 4–6
- Phases 8–10 depend on Faceted Search plan Phase 2 (`EventProduct`, `MarketplaceProductBase`)
- Phase 11 depends on Phases 7 and 10
- Phase 12 depends on Phase 9

Phases 1–7 (static blocks) have **no dependency on other plans** and can be implemented immediately. Phases 8–12 (dynamic blocks) require the Faceted Search plan's `MarketplaceProductBase` and `EventProduct` types.

---

## Optimizely Platform Features Leveraged (No Custom Code Needed)

| Feature | Platform Capability | How Editors Use It |
|---------|--------------------|--------------------|
| Block ordering | ContentArea drag-and-drop | Reorder blocks in any content area |
| Audience targeting | Visitor Groups on ContentArea items | Right-click block → Personalize → Select visitor group |
| Auto-expiration | Content scheduling | Set publish/unpublish dates on individual blocks |
| A/B testing | Optimizely Experimentation | Create content variations within ContentArea |
| Inline editing | On-page editing (OPE) | Click any `PropertyFor` field to edit in context |
| Preview | Preview mode | Preview page as different visitor groups |
| Versioning | Content versioning | Full version history on all blocks and pages |

---

## SEO & Performance

### SEO (Already Handled)

- `PageBaseSeo` provides `MetaTitle`, `MetaDescription`, `MetaNoRobots`, Open Graph properties
- `_MetaData.cshtml` renders all meta tags including OG tags
- `Html.CanonicalLink()` and `Html.AlternateLinks()` in layout
- No additional SEO work needed — the existing infrastructure covers it

### Performance

| Technique | Implementation |
|-----------|---------------|
| Responsive images | `srcset` with 5 breakpoints (480w–1920w) on all image blocks |
| Lazy loading | `loading="lazy"` and `decoding="async"` on all images except hero (hero uses `loading="eager"`) |
| Content caching | `IContentLoader` responses cached by Optimizely content cache |
| Minimal JavaScript | No JS framework on homepage — pure server-rendered HTML + CSS |
| CDN images | Cloudflare CDN image optimization (existing `cdn-cgi/image/` pattern) |
| CSS | Single `blocks.css` file — no CSS-in-JS overhead |

### Structured Data (Future Enhancement)

JSON-LD for `Organization` and `WebSite` schema can be added to the layout as a future SEO enhancement. Not in scope for this plan to keep the homepage implementation focused.

---

## Verification

1. `dotnet build src\Commerce.Demo.sln` — compiles after each phase
2. `dotnet test src\Commerce.Demo.Tests` — all tests pass
3. CMS: Create `HeroBannerBlock` in StartPage's HeroArea — renders with responsive image, heading, CTA
4. CMS: Add `AnnouncementBarBlock` content on StartPage Announcements tab — renders at top of page
5. CMS: Add `ContentCardBlock`, `QuickLinksBlock`, `TopicGridBlock` to MainContentArea — renders in editor-chosen order
6. CMS: Add `FeaturedEventsBlock` — automatically shows upcoming events from catalog
7. CMS: Add `FeaturedProductsBlock` with ProductTypeFilter="BookProduct" — shows only books
8. Toggle `ShowSearchBar` off — search bar disappears from homepage
9. Set `AnnouncementBar.IsEnabled` to false — announcement bar hidden without removing content
10. Reorder blocks in MainContentArea via drag-and-drop — page layout updates
11. Set publish/unpublish date on a `ContentCardBlock` — block appears/disappears on schedule
12. Apply visitor group to a block — block only visible to targeted segment

## Critical Existing Files (Reference)

| File | Why |
|------|-----|
| `ContentTypes/Cms/Pages/StartPage.cs` | Content type being modified |
| `ContentTypes/Cms/Pages/Base/StartPageBase.cs` | Base class (empty, extends PageBaseSeo) |
| `ContentTypes/Cms/Blocks/Base/BlockBase.cs` | Base class for all blocks |
| `ContentTypes/Cms/Blocks/CarouselItemBlock.cs` | Pattern for image block with CDN srcset |
| `ContentTypes/Cms/Blocks/Local/LinkBlock.cs` | Reusable nested link pattern |
| `ContentTypes/Cms/Blocks/SnippetBlock.cs` | Pattern for blocks with enum properties |
| `Web/Components/CarouselItemBlockComponent.cs` | ViewComponent pattern for blocks |
| `Web/Components/Base/PartialContentComponentBase.cs` | Base ViewComponent class |
| `Web/Models/Blocks/BlockViewModel.cs` | Block view model wrapper |
| `Web/Controllers/StartPageController.cs` | StartPage controller (unchanged) |
| `Views/StartPage/Index.cshtml` | StartPage view (being redesigned) |
| `Views/Shared/Blocks/CarouselItemBlock.cshtml` | CDN image rendering pattern |
| `Views/Shared/Layouts/_Layout.cshtml` | Layout template (CSS link added) |
| `Core/Infrastructure/EditorDescriptors/EnumEditorDescriptor.cs` | Enum editor pattern |
| `Core/Infrastructure/EditorDescriptors/EnumSelectionFactory.cs` | Enum dropdown pattern |
| `Web/Startup.cs` | DI registration |

## Doc Updates Required

| Document | Update |
|---|---|
| `docs/architecture.md` | Homepage block composition diagram, FeaturedContentService data flow |
| `docs/decisions.md` | ADR: Server-rendered homepage (not React); ADR: IContentLoader over Find for dynamic blocks; ADR: Visitor Groups for audience targeting |
| `docs/stack.md` | Note BEM CSS convention for blocks |
| `docs/devlog.md` | Session entries per phase |
| `docs/todo.md` | Mark phases complete |
