# Project Specification

> **Status:** Retroactive — documents what exists as of 2026-02-09
> **Version:** 2.2.0

## Vision Statement

Optimizely Commerce Demo is an enterprise-grade e-commerce platform for the fashion retail domain, built on Optimizely CMS 12 and Commerce 14. It serves as both a production-ready foundation and a reference implementation demonstrating best practices for building layered, maintainable commerce applications on the Optimizely platform with a modern React frontend.

## Problem & Solution

- **Problem:** Building enterprise e-commerce on Optimizely requires significant boilerplate — content type hierarchies, caching infrastructure, search integration, payment gateways, SEO, cookie consent, and proper layered architecture. Starting from scratch is slow and error-prone.
- **Target Users:** Development teams building fashion/retail e-commerce on Optimizely CMS/Commerce. Also serves as a demo platform for stakeholder presentations.
- **Solution:** A fully-structured, layered commerce application with pre-built content types, search, PayPal payments, SEO support, cookie consent, and demo content scaffolding — ready to customize and extend.

## Core Features (Existing)

### 1. Content Management System (CMS Pages)

**User Story:** As a content editor, I want to manage site pages through Optimizely's editorial interface so that I can publish and update content without developer involvement.

**Existing Page Types:**
| Page Type | Purpose | Available Under |
|-----------|---------|----------------|
| StartPage | Home page with heading, lead text, content areas | Root |
| CartPage | Shopping cart with empty basket message | StartPage |
| CheckoutPage | Checkout flow with empty basket message | StartPage |
| LoginPage | User authentication | StartPage |
| RegistrationPage | User registration | StartPage |
| UserProfilePage | User account management | StartPage |
| SiteSettingsPage | Site-wide configuration (admin only) | StartPage |
| NotFoundPage | 404 error page with custom heading | StartPage |

**Page Base Class Hierarchy:**
```
PageData → PageBase → PageBasePublic → PageBaseSeo → StartPageBase → StartPage
                                                    → SiteSettingsPageBase → SiteSettingsPage
                                     → NotFoundPageBase → NotFoundPage
```

**Cross-Cutting Page Features:**
- SEO metadata (MetaTitle, MetaDescription, MetaNoRobots) via `PageBaseSeo` / `ISeoMeta`
- OpenGraph tags (Title, Description, Image) via `IOpenGraph` / `OpenGraphBlock`
- Snippet injection (head/body scripts/styles) via `ISnippets` / `ISnippetsWithInheritance`
- Sitemap exclusion via `IExcludeFromSitemap`
- Cookie consent management via `ICookiesConsent` / `CookiesConsentBlock`
- Searchability control via `ISearchable`
- Pagination support via `IListable`

**Acceptance Criteria:**
- [x] All page types render in Optimizely editorial UI
- [x] SEO metadata configurable per page
- [x] OpenGraph tags configurable per page
- [x] Snippets can be injected in head, after body open, before body close
- [x] Cookie consent dialog configurable from SiteSettingsPage
- [x] Pages can be excluded from sitemap and search

---

### 2. Commerce Catalog (Fashion Domain)

**User Story:** As a merchandiser, I want to manage a hierarchical fashion product catalog with categories, products, variants, bundles, and packages so that customers can browse and purchase products.

**Existing Commerce Types:**
| Type | Content Type | Key Properties |
|------|-------------|----------------|
| Category | FashionCategory | Heading, IntroText, SEO, OpenGraph |
| Product | FashionProduct | Brand, Description, LongDescription, Sizing, ProductTeaser, AvailableSizes, AvailableColors |
| Variant | FashionVariant | Size, Color, Description |
| Bundle | FashionBundle | Description |
| Package | FashionPackage | Description |

**Commerce Base Class Hierarchy:**
```
ProductContent → ProductBase → FashionProduct
NodeContent → CategoryBase → FashionCategory
VariationContent → VariantBase → FashionVariant
BundleContent → BundleBase → FashionBundle
PackageContent → PackageBase → FashionPackage
```

**Demo Catalog Structure (auto-scaffolded):**
```
Fashion (Catalog - USD, English)
└── Men
    ├── Shoes
    │   ├── Product 1 (with Variant 1, Variant 2)
    │   ├── Product 2
    │   └── Product 3
    └── Accessories
        ├── Product 1
        ├── Product 2
        └── Product 3
```

**Acceptance Criteria:**
- [x] Fashion product catalog browsable in Optimizely Commerce UI
- [x] Products have sizes, colors, brand, descriptions
- [x] Variants represent size/color combinations
- [x] Bundles and packages supported
- [x] Categories support nested hierarchy
- [x] Demo data auto-created on first startup (development mode)
- [x] Catalog caching enabled (5-minute TTL)

---

### 3. Block System

**User Story:** As a content editor, I want reusable content blocks to compose pages visually in the editorial interface.

**Existing Block Types:**
| Block | Purpose | Key Properties |
|-------|---------|----------------|
| TeaserBlock | Promotional teaser with image | Heading (50 char max), LeadText, LinkButton, Image |
| SnippetBlock | Head/body script/style injection | Name, Description, Type, Usage, Placement, Content, RequiresVisitorConsent |
| CookiesConsentBlock | Cookie consent dialog config | CookieName, ExpirationDate, DialogContent, HideCookiesConsent, ShowCookieTypeSelection, AutoDiscoverCookies |
| SeoBlock | SEO metadata (embeddable) | MetaTitle, MetaNoRobots, MetaKeywords, MetaDescription |
| LinkBlock | Link with text | LinkText, LinkUrl |
| OpenGraphBlock | OpenGraph metadata (embeddable) | OpenGraphTitle, OpenGraphDescription, OpenGraphImage |

**Display Options:** Full width, Wide, Half, Narrow

**Acceptance Criteria:**
- [x] TeaserBlock renders in content areas with image support
- [x] SnippetBlock supports Script, Style, and HTML types
- [x] Snippet placement configurable: BeforeClosingHead, AfterOpeningBody, BeforeClosingBody
- [x] Snippet usage categories: Essential, Functional, Analytics, Performance, Marketing
- [x] Cookie consent respects visitor consent for snippet rendering
- [x] Display options available for content area rendering

---

### 4. Media Management

**User Story:** As a content editor, I want to upload and manage images, videos, and documents through the CMS.

**Existing Media Types:**
| Type | Extensions | Special Features |
|------|-----------|-----------------|
| ImageFile | jpg, jpeg, jpe, ico, gif, bmp, png | Copyright, AlternateText |
| VectorImageFile | svg | Uses BinaryData for thumbnails |
| VideoFile | flv, mp4, webm | Copyright, PreviewImage |
| GenericMedia | (all others) | Description |

**Acceptance Criteria:**
- [x] Image upload with copyright and alt text
- [x] SVG support with proper thumbnail handling
- [x] Video upload with preview image support
- [x] Generic file upload with description

---

### 5. Search (EPiServer Find)

**User Story:** As a site visitor, I want to search across all site content and products to find what I'm looking for.

**Existing Capabilities:**
- Full-text unified search via `ISearchService.PerformGlobalSearch()`
- Auto-complete suggestions via `ISearchService.GetSuggestions()`
- Sort options: Relevance, Alphabetical, Newest, Oldest
- Faceted search support
- Pagination
- Commerce content indexing enabled (`FindIndexCatalogContent = true`)
- Commerce remote events for real-time index updates

**Search Criteria Model:**
| Parameter | Query Param | Default |
|-----------|-------------|---------|
| Query | `q` | (required) |
| Page | `p` | 1 |
| PageSize | `ps` | (configurable) |
| Sort Order | `o` | Relevance |
| Facets | `f` | [] |

**Acceptance Criteria:**
- [x] Global search across CMS pages and commerce content
- [x] Auto-complete suggestions
- [x] Multiple sort options
- [x] Faceted filtering
- [x] Pagination
- [x] Content excluded from search when `ExcludeFromSearch = true`

---

### 6. Payment Integration (PayPal)

**User Story:** As a customer, I want to pay for my order using PayPal so that I can complete my purchase securely.

**Existing Capabilities:**
- `IPayPalPaymentGateway` with three operations:
  - `CreateOrderAsync()` — Create PayPal order
  - `CaptureOrderAsync()` — Capture payment for approved order
  - `GetOrderDetailsAsync()` — Retrieve order details
- Result types with success/failure tracking, debug IDs, and error details
- Order status tracking: Unknown, Created, Approved, Captured, Failed
- Capture status tracking: Unknown, Completed, Declined, Pending, Failed
- SDK client factory pattern
- Configurable via `PayPalOptions`
- Mapper classes for order/status/error conversion

**Acceptance Criteria:**
- [x] PayPal order creation
- [x] PayPal payment capture
- [x] Order details retrieval
- [x] Error handling with structured error models
- [x] Status mapping (order and capture statuses)
- [ ] **Not yet wired into checkout flow** [TODO]

---

### 7. Site Settings & Configuration

**User Story:** As an administrator, I want to manage site-wide settings (site name, snippets, cookie consent, API keys) through the CMS without code changes.

**Existing Capabilities:**
- `ISiteSettingsManager` loads `SiteSettingsPageBase` content
- Cached via decorator pattern (`SiteSettingsManagerCacheDecorator`)
- Settings include: SiteName, CookiesConsent block, Snippets content area, API keys
- Multi-site aware via `SiteDefinition`
- Language-aware settings loading

**Acceptance Criteria:**
- [x] Site settings manageable through CMS editor
- [x] Settings cached for performance
- [x] Multi-site and multi-language support
- [x] Cookie consent configurable from settings

---

### 8. Navigation & Breadcrumbs

**User Story:** As a site visitor, I want navigation menus and breadcrumb trails so I can orient myself and move through the site.

**Existing Capabilities:**
- `IBreadcrumbsService.GetBreadcrumbs()` — generates breadcrumb trail for any page
- `HtmlHelpers.MenuList()` — recursive menu generation with filtering
- Visitor-based content filtering for breadcrumbs
- Menu items include: Page, Selected state, HasChildren

**Acceptance Criteria:**
- [x] Breadcrumb trail for any page in the content tree
- [x] Recursive navigation menu generation
- [x] Content filtering (visibility, permissions)

---

### 9. SEO & Sitemaps

**User Story:** As a marketing manager, I want proper SEO metadata and XML sitemaps so that search engines can properly index the site.

**Existing Capabilities:**
- Per-page SEO metadata (title, description, norobots)
- OpenGraph tags for social sharing
- XML sitemap generation via Geta.Optimizely.Sitemaps
- Commerce and CMS content in sitemaps
- Content excludable from sitemap via `IExcludeFromSitemap`
- Robots.txt management via Stott.Optimizely.RobotsHandler

**Acceptance Criteria:**
- [x] SEO metadata on all public pages
- [x] OpenGraph tags for social sharing
- [x] XML sitemap generated for CMS and commerce content
- [x] Pages can be excluded from sitemap
- [x] Robots.txt manageable through admin UI

---

### 10. Error Handling & Security

**User Story:** As a site visitor, I should see friendly error pages. As a developer, errors should be logged and security headers set.

**Existing Capabilities:**
- `ExceptionFilter` middleware — catches exceptions, returns JSON for `/api`, redirects for pages
- `UseStatusCodePagesWithReExecute("/{0}")` for status code pages
- 404 handling via Geta.NotFoundHandler with automatic redirects
- Security response headers via Stott.Security.Optimizely
- HTTP-only session cookies (essential, 20s idle timeout)
- Static file caching (1 week, must-revalidate)

**Acceptance Criteria:**
- [x] API errors return structured JSON responses
- [x] Page errors show friendly error pages
- [x] 404s handled with automatic redirect suggestions
- [x] Security headers configured
- [x] Session cookies HTTP-only and essential

---

### 11. Custom API Endpoints

**User Story:** As a frontend developer, I want REST API endpoints to fetch content data for the React SPA.

**Existing Endpoints (Stub/Demo):**
| Method | Path | Purpose | Status |
|--------|------|---------|--------|
| GET | /api/subpage | List start page children | Stub (hardcoded culture) |
| GET | /api/subpage/{id} | Get specific page | Stub (ignores ID) |
| POST | /api/post | Create resource | Stub (echoes request) |

**Acceptance Criteria:**
- [x] Minimal API infrastructure in place
- [ ] **Actual business logic in endpoints** [TODO]
- [ ] **API authentication enabled** [TODO]
- [ ] **Culture/language configurable** [TODO]

---

### 12. React Frontend

**User Story:** As a customer, I want a modern, responsive shopping experience built with React.

**Current Status:** Placeholder — Vite + React default template with counter demo. Not yet integrated with backend APIs.

**Acceptance Criteria:**
- [x] React 19 + Vite 7 build pipeline
- [x] Build output automatically copied to Web project
- [x] Dev server available via `npm run dev`
- [ ] **Actual UI components** [TODO]
- [ ] **Backend API integration** [TODO]

---

## User Flows

### Primary Flow: Browse and Purchase (Target)
1. Visitor lands on StartPage → sees teasers and promotions
2. Navigates to category (e.g., Men → Shoes) via menu
3. Browses product listing with search and facets
4. Views product detail with variants (size/color)
5. Adds product variant to cart
6. Proceeds to checkout
7. Pays via PayPal
8. Receives order confirmation

**Current Status:** Steps 1-4 partially implemented (CMS/Commerce content exists). Steps 5-8 have infrastructure (CartPage, CheckoutPage, PayPal gateway) but are not wired together.

### Secondary Flow: Content Management
1. Editor logs into Optimizely admin
2. Creates/edits pages, products, categories
3. Manages site settings (snippets, cookie consent)
4. Publishes content
5. Content appears on public site

**Current Status:** Fully functional via Optimizely editorial interface.

## Success Metrics

- [TODO: Define performance targets (page load, API response time)]
- [TODO: Define uptime/availability targets]
- Catalog caching: 5-minute TTL across all catalog entity types
- Static assets: 1-week client-side cache

## Technical Constraints

- **Platform:** Optimizely CMS 12 + Commerce 14 (EPiServer)
- **Runtime:** .NET 8.0
- **Database:** SQL Server (required by Optimizely)
- **Search:** EPiServer Find (SaaS, requires API key)
- **Cloud:** Optimizely DXP for non-development environments
- **NuGet:** Must use Optimizely private feed for platform packages
- **Frontend Build:** Must integrate with dotnet build pipeline (pre-build npm step)

## Non-Functional Requirements (Production Standards)

### Logging Requirements
- [x] Exception middleware logs errors with context
- [ ] Application logging for key user actions [TODO: Expand coverage]
- [ ] Structured logging format across all services [TODO]
- [x] No sensitive data in logs (enforced by convention)

### Error Handling Requirements
- [x] ExceptionFilter catches and handles API/page errors
- [x] 404 handling with automatic redirects
- [x] User-friendly error pages
- [ ] Consistent error patterns across all services [TODO: Standardize]

### Security Requirements
- [x] Security response headers (Stott.Security)
- [x] HTTP-only session cookies
- [x] Role-based access for admin features
- [ ] Input validation on API endpoints [TODO]
- [ ] CSRF protection on state-changing requests [TODO: Verify]
- [x] Secrets in appsettings (not hardcoded in code)
- [ ] API authentication [TODO: Enable]

## Out of Scope (Current Phase)

- Multi-language support (infrastructure exists but content is English-only)
- Multi-currency support (infrastructure exists but only USD configured)
- Optimizely Content Delivery API (configured but commented out)
- Content Delivery Forms API
- Commerce Management API
- Advanced product recommendations
- Wishlist functionality
- Order history and tracking
- Customer reviews and ratings

## Design Requirements

- [TODO: Define visual design system]
- [TODO: Define responsive breakpoints]
- [TODO: Define accessibility targets (WCAG level)]
- Display options: Full, Wide, Half, Narrow for content areas
