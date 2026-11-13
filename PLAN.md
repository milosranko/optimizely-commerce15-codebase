# Implementation Plan

> **Status:** Retroactive — documents current architecture and forward plan as of 2026-02-09
> **Version:** 2.2.0

## Phase 1: Foundation (COMPLETE)

Core platform setup, architecture, and infrastructure.

- [x] Solution structure with layered architecture (6 .NET projects + 1 frontend)
- [x] Dependency graph: Web → Business → ContentTypes → Core
- [x] All Optimizely NuGet packages isolated in Core project
- [x] DI configuration with Scrutor (assembly scanning + decorator pattern)
- [x] Global assembly info and semantic versioning (2.2.0)
- [x] NuGet feeds configuration (nuget.org, Optimizely, local packages)
- [x] Environment-based configuration (Development, Integration, Preproduction, Production)
- [x] F3 documentation system setup

### 1.1 CMS Infrastructure (COMPLETE)
- [x] Page base class hierarchy (PageBase → PageBasePublic → PageBaseSeo → StartPageBase)
- [x] Block base class (BlockBase with CurrentPage resolution)
- [x] Property interfaces (ISeo, ISearchable, IListable, ISnippets, IOpenGraph, IExcludeFromSitemap, ICookiesConsent)
- [x] Custom view locations (Blocks, PagePartials) via SiteViewEngineLocationExpander
- [x] Editor descriptors for admin UI customization
- [x] Initialization modules
- [x] Channel configurations
- [x] Global constants (Tabs, Groups, UIHints, ContentAreaTags)
- [x] Custom validation attributes
- [x] TinyMCE editor configuration

### 1.2 Commerce Infrastructure (COMPLETE)
- [x] Commerce base classes (ProductBase, CategoryBase, VariantBase, BundleBase, PackageBase)
- [x] Catalog caching (5-minute TTL for version, collection, entry, node)
- [x] Find Commerce integration (index catalog content, remote events)
- [x] Display options (Full, Wide, Half, Narrow)
- [x] Shipping provider configurations

### 1.3 Frontend Build Pipeline (COMPLETE)
- [x] React 19 + Vite 7 project setup
- [x] Pre-build integration (npm install + build → wwwroot/dist)
- [x] ESLint configuration
- [x] Dev server (npm run dev)

---

## Phase 2: Content Types & Models (COMPLETE)

All CMS and Commerce content type definitions.

### 2.1 CMS Page Types (COMPLETE)
- [x] StartPage — Heading, LeadText, MainContentArea, AnotherMainContentArea
- [x] CartPage — EmptyBasketText
- [x] CheckoutPage — EmptyBasketText
- [x] LoginPage
- [x] RegistrationPage
- [x] UserProfilePage
- [x] SiteSettingsPage — SiteName, CookiesConsent, Snippets, API keys (admin-only)
- [x] NotFoundPage — Heading

### 2.2 Block Types (COMPLETE)
- [x] TeaserBlock — Heading (50 char), LeadText, LinkButton, Image
- [x] SnippetBlock — Name, Description, Type, Usage, Placement, Content, RequiresVisitorConsent
- [x] CookiesConsentBlock — CookieName, ExpirationDate, DialogContent, visibility/type options
- [x] SeoBlock — MetaTitle, MetaNoRobots, MetaKeywords, MetaDescription
- [x] LinkBlock — LinkText, LinkUrl (with IsEmpty check)
- [x] OpenGraphBlock — OpenGraphTitle, OpenGraphDescription, OpenGraphImage

### 2.3 Commerce Types (COMPLETE)
- [x] FashionProduct — Brand, Description, LongDescription, Sizing, ProductTeaser, AvailableSizes, AvailableColors
- [x] FashionVariant — Size, Color, Description
- [x] FashionCategory — (inherits Heading, IntroText from CategoryBase)
- [x] FashionBundle — Description
- [x] FashionPackage — Description

### 2.4 Media Types (COMPLETE)
- [x] ImageFile — jpg/jpeg/png/gif/bmp/ico, Copyright, AlternateText
- [x] VectorImageFile — svg, BinaryData thumbnail
- [x] VideoFile — flv/mp4/webm, Copyright, PreviewImage
- [x] GenericMedia — all other extensions, Description

---

## Phase 3: Business Services (COMPLETE)

Core business logic implementations.

### 3.1 Caching (COMPLETE)
- [x] ICacheService interface (extends IObjectInstanceCache)
- [x] CacheService implementation (wraps EPiServer CacheManager, 5-min default TTL)
- [x] CacheDecoratorBase template method for building cache decorators
- [x] GetOrSet, GetOrSetAsync, Insert, RemoveStartsWith operations

### 3.2 Search (COMPLETE)
- [x] ISearchService interface (PerformGlobalSearch, GetSuggestions)
- [x] SearchService implementation (EPiServer Find)
- [x] SearchCriteriaModel (query, page, pageSize, sortOrder, facets)
- [x] Sort types: Relevance, Alphabetical, Newest, Oldest
- [x] Faceted search support
- [x] Auto-complete suggestions

### 3.3 Site Settings (COMPLETE)
- [x] ISiteSettingsManager interface
- [x] SiteSettingsManager implementation
- [x] SiteSettingsManagerCacheDecorator (Scrutor decorator)
- [x] Multi-site and language-aware settings

### 3.4 Navigation (COMPLETE)
- [x] IBreadcrumbsService interface
- [x] BreadcrumbsService with visitor filtering
- [x] BreadcrumbData and BreadcrumbItem models
- [x] HtmlHelpers.MenuList recursive menu generation

### 3.5 Sitemaps (COMPLETE)
- [x] DefaultSitemapXmlGenerator
- [x] Commerce and CMS content sitemaps
- [x] Geta.Optimizely.Sitemaps integration with realtime caching

### 3.6 Cookies (COMPLETE)
- [x] ICookieService interface (Get, Set, Remove, Contains)
- [x] CookieService implementation

---

## Phase 4: Web Layer (COMPLETE)

HTTP layer — controllers, views, filters, API.

### 4.1 Controllers (COMPLETE)
- [x] PageControllerBase<T> — generic base for CMS page controllers
- [x] CommerceControllerBase<T> — generic base for catalog content controllers
- [x] StartPageController — renders StartPage with PageViewModel
- [x] FashionProductController — renders FashionProduct with ProductViewModel

### 4.2 View Models (COMPLETE)
- [x] PageViewModel<T> (record) — CurrentPage + Layout
- [x] ProductViewModel<T> (record) — extends PageViewModel, adds Variants, RelatedProducts, Code, Quantity
- [x] VariantsViewModel — DisplayName, ImageUrl, Url, Prices, Code, IsAvailable, Variant
- [x] BlockViewModel<TBlock> (record) — CurrentBlock, IsEditing
- [x] BlockPreviewModel — for block preview in editor
- [x] LayoutModel — MetaData, OpenGraph, Header, Footer, PageTitle, SiteName, Snippets, CookiesConsent
- [x] MetaDataModel, OpenGraphModel, HeaderModelBase, FooterModelBase
- [x] SiteSettingsViewModel
- [x] SnippetBlockViewModel, CookieItemBlockViewModel, CookiesConsentBlockViewModel
- [x] LinkViewModel, ModalViewModel

### 4.3 Filters & Middleware (COMPLETE)
- [x] PageContextActionFilter — populates Layout on page view models
- [x] ExceptionFilter middleware — JSON errors for API, redirect for pages

### 4.4 Razor Views (COMPLETE)
- [x] Layouts: _ShellLayout, _Layout, _ProductLayout, _BlockPreviewLayout
- [x] Page views: StartPage/Index, FashionProduct/Index
- [x] Block views: TeaserBlock, FashionProduct (block)
- [x] Display templates: ImageFile, LinkBlock
- [x] Shared partials: _Header, _Footer, _Product, _MetaData

### 4.5 HTML Helpers (COMPLETE)
- [x] Snippet rendering with consent awareness
- [x] Meta tag and OpenGraph tag helpers
- [x] Recursive menu list generation
- [x] Editable field rendering (edit/view mode)
- [x] Image handling (src, alt, srcset, sizes)
- [x] Text styling (markers to HTML tags, newline to BR)
- [x] Block rendering by content reference

### 4.6 Object Mapping (COMPLETE)
- [x] MapsterConfig.cs — centralized mapping configuration

### 4.7 API Endpoints (PARTIAL)
- [x] Minimal API infrastructure (MapGroup "/api")
- [x] Response base classes (ResponseList<T>, ResponseSingle<T>)
- [x] IApiService / ApiService service layer
- [ ] Actual business logic in endpoints (currently stubs)
- [ ] API authentication (.RequireAuthorization commented out)
- [ ] Culture/language parameterization

---

## Phase 5: Integrations (PARTIAL)

External system integrations.

### 5.1 PayPal (COMPLETE — Not Wired)
- [x] IPayPalPaymentGateway interface (CreateOrder, CaptureOrder, GetOrderDetails)
- [x] PayPalPaymentGateway implementation
- [x] PayPalSdkClientFactory
- [x] PayPalOptions configuration
- [x] Order/capture status enums and mappers
- [x] PayPalOperationResult<T> with error handling
- [x] Domain models (OrderDetails, OrderItem, Money, PayerInfo, etc.)
- [ ] **Integration with CheckoutPage** [TODO]
- [ ] **PayPal service registration in Startup.cs** [TODO: Verify]

### 5.2 HTTP Clients (PARTIAL)
- [x] StartupExtensions.AddHttpClients() with resilience patterns
- [x] ExampleClient placeholder
- [ ] Actual HTTP client implementations for external APIs [TODO]

---

## Phase 6: Testing (PARTIAL)

### 6.1 Unit Tests (PARTIAL)
- [x] MSTest v3 test project setup
- [x] HttpContextMoq for mocking
- [x] Code coverage tooling
- [x] TRX report generation
- [ ] Business layer test coverage [TODO: Assess current coverage]
- [ ] Web controller tests [TODO: Add project reference]
- [ ] Integration layer tests [TODO: Add project reference]

---

## Phase 7: Content Scaffolding & Demo (COMPLETE)

### 7.1 Demo Content (COMPLETE)
- [x] StartPage with TeaserBlock
- [x] Fashion catalog hierarchy (Men → Shoes/Accessories → Products → Variants)
- [x] Default currency: USD, language: English
- [x] BuildMode.OnlyIfEmpty (won't recreate if content exists)
- [x] Site configuration: "Demo" site at https://localhost:5000

---

## Phase 8: Security & Infrastructure (MOSTLY COMPLETE)

### 8.1 Security (MOSTLY COMPLETE)
- [x] ASP.NET Core Identity (CmsAspNetIdentity<ApplicationUser>)
- [x] Role-based authorization (WebAdmins)
- [x] Security response headers (Stott.Security)
- [x] Robots.txt management (Stott.RobotsHandler)
- [x] HTTP-only session cookies
- [x] 404 handling with automatic redirects (Geta.NotFoundHandler)
- [ ] API endpoint authentication [TODO]
- [ ] CSRF verification [TODO: Audit]

### 8.2 Caching (COMPLETE)
- [x] Catalog caching (5-minute TTL)
- [x] Application-level cache service
- [x] Site settings cache decorator
- [x] Sitemap realtime caching
- [x] Static file caching (1-week headers)
- [x] Output cache middleware

### 8.3 Cloud / DXP (COMPLETE)
- [x] Azure Blob Storage for media (non-dev)
- [x] Optimizely Cloud Platform support (CMS + Commerce)
- [x] Environment-specific configuration files

---

## Forward Plan — Remaining Work

### Priority 1: Wire Up E-Commerce Flow
- [ ] Connect PayPal gateway to CheckoutPage
- [ ] Implement cart functionality (add/remove/update items)
- [ ] Implement checkout flow (shipping, payment, confirmation)
- [ ] Register PayPal services in DI

### Priority 2: API Development
- [ ] Implement real API endpoint logic (replace stubs)
- [ ] Enable API authentication
- [ ] Add API input validation
- [ ] Make culture/language configurable on endpoints
- [ ] Evaluate enabling Optimizely Content Delivery APIs

### Priority 3: React Frontend
- [ ] Define component architecture
- [ ] Implement product listing page
- [ ] Implement product detail page
- [ ] Implement cart UI
- [ ] Implement checkout UI
- [ ] Connect to backend APIs

### Priority 4: Testing & Quality
- [ ] Expand unit test coverage across all layers
- [ ] Add integration tests
- [ ] Add Web controller tests (requires adding project reference)
- [ ] Add PayPal gateway tests

### Priority 5: Architecture Improvements
- [ ] Resolve dual SearchService (Core vs Business)
- [ ] Consider splitting Optimizely.Core (infrastructure vs features)
- [ ] Consider adding Abstractions project for shared interfaces

---

## Database Schema

Managed by Optimizely CMS and Commerce — no custom migrations. Schema includes:
- CMS content tables (pages, blocks, media)
- Commerce catalog tables (products, variants, categories, bundles)
- Commerce order/cart tables
- EPiServer Find index
- Identity tables (ASP.NET Core Identity)
- NotFoundHandler redirect tables

---

## API Endpoints

### Current (Stub)
| Method | Endpoint | Purpose | Status |
|--------|----------|---------|--------|
| GET | /api/subpage | List start page children | Stub |
| GET | /api/subpage/{id} | Get specific page | Stub |
| POST | /api/post | Create resource | Stub |

### Planned [TODO]
| Method | Endpoint | Purpose | Priority |
|--------|----------|---------|----------|
| GET | /api/products | Product listing with search/facets | High |
| GET | /api/products/{code} | Product detail with variants | High |
| GET | /api/categories | Category tree | High |
| POST | /api/cart/items | Add item to cart | High |
| PUT | /api/cart/items/{code} | Update cart item quantity | High |
| DELETE | /api/cart/items/{code} | Remove cart item | High |
| GET | /api/cart | Get current cart | High |
| POST | /api/checkout/paypal/create | Create PayPal order | High |
| POST | /api/checkout/paypal/capture | Capture PayPal payment | High |
| GET | /api/search | Search products/content | Medium |
| GET | /api/search/suggestions | Auto-complete suggestions | Medium |

---

## Component Hierarchy (React Frontend — Planned)

```
App
├── Layout
│   ├── Header (navigation, search, cart icon)
│   └── Footer
├── Pages
│   ├── HomePage (start page content)
│   ├── CategoryPage (product grid + facets)
│   ├── ProductPage (detail + variants)
│   ├── CartPage (items + totals)
│   ├── CheckoutPage (shipping + payment)
│   ├── LoginPage
│   ├── RegistrationPage
│   ├── UserProfilePage
│   └── NotFoundPage
└── Shared
    ├── ProductCard
    ├── SearchBar
    ├── Breadcrumbs
    ├── Pagination
    └── CookieConsent
```

---

## Dependencies

### Production (.NET)
- EPiServer.CMS (12.x)
- EPiServer.Commerce (14.x)
- EPiServer.Find
- EPiServer.CloudPlatform.Cms
- Scrutor 7.0.0
- Mapster
- PayPalServerSDK 2.2.0
- Geta.Optimizely.Sitemaps
- Geta.NotFoundHandler
- Stott.Security.Optimizely
- Stott.Optimizely.RobotsHandler
- CmsContentScaffolding

### Production (Frontend)
- React 19.2.0
- React-DOM 19.2.0
- Vite 7.2.4

### Development
- MSTest v3
- HttpContextMoq 1.7.0
- ESLint + plugins
- Microsoft.Testing.Extensions.CodeCoverage
