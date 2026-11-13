# Architecture Documentation

> Last Updated: 2026-02-09 [RETROACTIVE]

## System Overview

Optimizely Commerce Demo is an enterprise e-commerce platform built on a **layered architecture** with strict dependency flow. The system uses Optimizely CMS 13 and Commerce 15 on .NET 10 as its foundation (upgraded 2026-11-11, ADR-008), with a React 19 SPA frontend served via Vite 7.

**Design Philosophy:** Separation of concerns through project boundaries, interface-driven design for testability, and decorator pattern for cross-cutting concerns. All Optimizely platform dependencies are isolated in the Core project.

## Dependency Graph

```
Commerce.Demo.Web (ASP.NET Core Host — Entry Point)
    │
    └──→ Commerce.Demo.Business (Business Logic Layer)
            │
            ├──→ Commerce.Demo.Integrations (External Systems — no Optimizely deps)
            │
            └──→ Commerce.Demo.Optimizely.ContentTypes (Content Models)
                    │
                    └──→ Commerce.Demo.Optimizely.Core (Foundation — all Optimizely NuGet packages)

Commerce.Demo.Frontend (React 19 SPA — build output copied to Web/wwwroot/dist)

Commerce.Demo.Tests (MSTest v3 — references Business only)
```

**Key Rules:**
- No circular dependencies
- All Optimizely/EPiServer NuGet packages referenced only in Core
- ContentTypes has no business logic — pure model definitions
- Integrations has no Optimizely dependencies — keeps payment code portable

## Component Architecture

### Commerce.Demo.Web
**Purpose:** ASP.NET Core host, HTTP layer, view rendering
**Responsibilities:**
- Application startup and DI configuration (Startup.cs)
- Minimal API endpoints (Api/Endpoints.cs) mounted at `/api`
- MVC Controllers for CMS pages and commerce catalog content
- Razor views and view models
- Action filters (PageContextActionFilter)
- Object mapping configuration (MapsterConfig.cs)
- Static file serving (wwwroot/dist for React SPA)

**Dependencies:** Commerce.Demo.Business
**Location:** `src/Commerce.Demo.Web/`

### Commerce.Demo.Business
**Purpose:** Business logic and service implementations
**Responsibilities:**
- Search models (Features/Search: criteria, facets, hits, pagination). The Find-based `SearchService` was removed with EPiServer Find (ADR-008); search runs on Optimizely Graph once a Graph-based service is built.
- Site settings management (ISiteSettingsManager → SiteSettingsManager)
- Cache decorator (SiteSettingsManagerCacheDecorator)
- Sitemap generation (DefaultSitemapXmlGenerator)
- Breadcrumb navigation (IBreadcrumbsService → BreadcrumbsService)
- Cache abstraction (ICacheService → CacheService wrapping EPiServer CacheManager)
- Cookie handling (CookieService)

**Dependencies:** Commerce.Demo.Integrations, Commerce.Demo.Optimizely.ContentTypes
**Location:** `src/Commerce.Demo.Business/`

### Commerce.Demo.Optimizely.Core
**Purpose:** Foundation layer — all Optimizely platform infrastructure
**Responsibilities:**
- All Optimizely/EPiServer NuGet package references
- Initialization modules (startup hooks)
- View engine expansion (SiteViewEngineLocationExpander — custom Razor view paths)
- Editor descriptors (CMS admin UI customizations)
- Channel configurations
- Scheduled jobs
- Shipping provider configurations
- CMS gadgets
- Global constants (TabNames, Categories)
- Custom validation attributes
- Extension methods for EPiServer APIs

**Dependencies:** None (foundation layer)
**Location:** `src/Commerce.Demo.Optimizely.Core/`

### Commerce.Demo.Optimizely.ContentTypes
**Purpose:** Optimizely content type definitions — pure models, no logic
**Responsibilities:**
- CMS page types (StartPage, CartPage, CheckoutPage, etc.)
- Commerce product types (FashionProduct)
- Commerce category types (FashionCategory)
- Commerce variant types (FashionVariant)
- Commerce bundle/package types (FashionBundle, FashionPackage)
- Block types (TeaserBlock, etc.)
- Media types
- Base class hierarchies (PageBase → PageBasePublic → PageBaseSeo → StartPageBase)
- Property interfaces (ISeo, ISearchable, IListable, ISnippets, etc.)

**Dependencies:** Commerce.Demo.Optimizely.Core
**Location:** `src/Commerce.Demo.Optimizely.ContentTypes/`

### Commerce.Demo.Integrations
**Purpose:** External system integrations — no Optimizely dependencies
**Responsibilities:**
- PayPal payment gateway (IPayPalPaymentGateway → PayPalPaymentGateway)
- PayPal SDK client factory (PayPalSdkClientFactory)
- Payment order/status mappers
- HTTP client abstractions with resilience patterns
- Startup extensions for HTTP client registration

**Dependencies:** None (abstract layer — PayPalServerSDK only)
**Location:** `src/Commerce.Demo.Integrations/`

### Commerce.Demo.Frontend
**Purpose:** React 19 Single Page Application
**Responsibilities:**
- Client-side UI rendering
- Built with Vite 7.2.4
- Output to `dist/` → copied to Web's `wwwroot/dist/` via pre-build step

**Dependencies:** npm packages (React 19, Vite 7)
**Location:** `src/Commerce.Demo.Frontend/`

### Commerce.Demo.Tests
**Purpose:** Unit tests
**Responsibilities:**
- MSTest v3 unit tests
- HttpContextMoq for mocking
- Code coverage via Microsoft.Testing.Extensions.CodeCoverage

**Dependencies:** Commerce.Demo.Business
**Location:** `src/Commerce.Demo.Tests/`

## Content Type Hierarchy

### CMS Pages
```
PageData (EPiServer)
  └─ PageBase (abstract) ── ILayoutPageBase
       └─ PageBasePublic (abstract)
            └─ PageBaseSeo (abstract)
                 ├─ SiteSettingsPageBase (abstract)
                 └─ StartPageBase (abstract)
                      └─ StartPage (concrete)
```

### Commerce Content
```
ProductContent (EPiServer)                NodeContent (EPiServer)
  └─ ProductBase (abstract)                 └─ CategoryBase (abstract)
       └─ FashionProduct                         └─ FashionCategory

VariationContent (EPiServer)
  └─ VariantBase (abstract)
       └─ FashionVariant
```

## Data Flow

### Page Request Flow
1. HTTP request → ASP.NET Core middleware pipeline
2. `UseRouting()` → `MapContent()` routes to Optimizely content
3. `PageControllerBase<T>` resolves page controller
4. `PageContextActionFilter` enriches ViewBag with layout data
5. Razor view renders using view models from `/Models`

### API Request Flow
1. HTTP request → `/api/*` route group
2. Minimal API handler in `Endpoints.cs`
3. `IContentLoader` (Optimizely) resolves content
4. `IApiService` processes business logic
5. Response DTO (`ResponseList<T>` or `ResponseSingle<T>`) returned

### Frontend Build Flow
1. Web project pre-build triggers `npm install && npm run build` in Frontend
2. Vite builds React SPA → `dist/` folder
3. Output copied to `Web/wwwroot/dist/`
4. Static files served with 1-week cache headers

## Integration Points

| Service | Purpose | Auth Method |
|---------|---------|-------------|
| Optimizely Graph (Content Graph) | Indexing + GraphQL delivery/search of CMS and catalog content (replaced EPiServer Find) | AppKey/Secret (user-secrets / DXP App Settings); registered only when configured |
| Opti ID | Back-office single sign-on (optional) | InstanceId/ClientId/ClientSecret; registered only when configured |
| PayPal (PayPalServerSDK) | Payment processing | OAuth (client ID/secret) |
| Azure Blob Storage | Media storage (non-dev) | Connection string |
| Optimizely DXP | Cloud platform support (non-dev) | Platform config |
| SQL Server | Content and commerce data | Connection string |
| SendGrid/SMTP | Email notifications | API key / SMTP credentials |

## Security Architecture

- **Authentication:** ASP.NET Core Identity (CmsAspNetIdentity<ApplicationUser>)
- **Authorization:** Role-based (e.g., "WebAdmins" for NotFoundHandler)
- **Security Headers:** Stott.Security.Optimizely
- **Robots:** Stott.Optimizely.RobotsHandler
- **Session:** HTTP-only, essential cookies, 20s idle timeout
- **Static Files:** 1-week cache with must-revalidate

## Caching Strategy

| Layer | Mechanism | TTL |
|-------|-----------|-----|
| Catalog content | CatalogOptions cache | 5 min (version, collection, entry, node) |
| Application cache | ICacheService (wraps EPiServer CacheManager) | 5 min default |
| Site settings | SiteSettingsManagerCacheDecorator | Via ICacheService |
| Sitemaps | Geta.Optimizely.Sitemaps (realtime caching) | Configured |
| Static files | Cache-Control header | 1 week |
| Output cache | ASP.NET Core OutputCache | Default |

## Infrastructure

| Environment | Purpose | Notes |
|-------------|---------|-------|
| Development | Local | SQL Server LocalDB, scheduler disabled, debug resources |
| Integration | CI/CD | Azure Blob, DXP support |
| Preproduction | Staging | Azure Blob, DXP support |
| Production | Live | Azure Blob, DXP support, full caching |

## Key Design Patterns

| Pattern | Implementation | Location |
|---------|----------------|----------|
| Decorator | SiteSettingsManagerCacheDecorator wraps ISiteSettingsManager | Business/Features/SiteSettings |
| Factory | PayPalSdkClientFactory | Integrations/PaymentProviders |
| Adapter/Mapper | Mapster, PayPal order/status mappers | Web/Infrastructure, Integrations |
| Template Method | CacheDecoratorBase | Business/Infrastructure |
| Feature Folders | Features/Search, Features/SiteSettings, etc. | Business |
| Minimal API | Endpoints.cs with MapGroup("/api") | Web/Api |
| Content Scaffolding | UseCmsContentScaffolding (dev only) | Web/Startup.cs |

## Known Architectural Notes

- ~~[RETROACTIVE] SearchService exists in both Core and Business~~ — resolved 2026-11-11: the Find-based SearchService/ISearchService were removed (no callers) with EPiServer Find (ADR-008).
- CMS 13: site definitions are replaced by Applications (`IApplicationResolver`); requests are routed by matching the host to an application.
- [RETROACTIVE] Tests project only references Business — no coverage for Web controllers, Core infrastructure, or Integrations.
- [RETROACTIVE] Optimizely.Core contains both infrastructure plumbing AND feature code (search, shipping, gadgets, scheduled jobs) — could be split.
- [RETROACTIVE] Frontend is minimal (App.jsx, main.jsx) — active development.
- [RETROACTIVE] API endpoints are stub/demo level — under development.
- [RETROACTIVE] PayPal integration appears complete but not wired into checkout flow.
