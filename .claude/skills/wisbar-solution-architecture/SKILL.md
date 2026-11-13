---
name: wisbar-solution-architecture
description: Orient in the Wisbar solution and decide where new code belongs - the seven projects and their real dependency direction, what each layer may and may not reference, the feature-folder convention, where services, models, endpoints, content types and infrastructure live, the cross-cutting seams (identity, caching, pricing keys, concurrency, out-of-request execution), and which specialized skill covers each area. Use when starting work in an unfamiliar part of the solution, when deciding which project a new class belongs in, when a project reference or layering question comes up, or when planning a change that spans more than one project.
---

# Wisbar solution architecture

An Optimizely Cloud DXP marketplace: CMS 12 + Commerce Connect 14 on .NET 8, with a React 19 frontend. This skill is for deciding **where code belongs** and **what may reference what**. For how to build a specific thing, jump to the specialized skill in Step 6.

## Step 1: The dependency graph

Verify before trusting any summary, including this one:

```bash
for p in Wisbar.Web Wisbar.Business Wisbar.Models Wisbar.Optimizely.ContentTypes Wisbar.Optimizely.Core Wisbar.Integrations; do
  echo "### $p"; grep -o 'ProjectReference Include="[^"]*"' $p/$p.csproj | grep -o '[A-Za-z.]*\.csproj'
done
```

As it actually stands:

```
Wisbar.Web  ──────────►  Wisbar.Business  ──────►  Wisbar.Integrations   (leaf)
     │                        │
     │                        ├─────────────────►  Wisbar.Models
     ▼                        │                         │
Wisbar.Models  ◄──────────────┘                         ▼
                              │              Wisbar.Optimizely.ContentTypes
                              └─────────────────────────┘         │
                                                                  ▼
                                                    Wisbar.Optimizely.Core  (leaf)

Wisbar.Tests ──► Wisbar.Business, Wisbar.Web
Wisbar.Frontend: no project reference; builds into Wisbar.Web/wwwroot/dist/
```

Bottom to top: **Core and Integrations (leaves) → ContentTypes → Models → Business → Web**.

Two easy mistakes:

- **`Wisbar.Models` is a real layer**, not just a bag of DTOs - it sits between ContentTypes and Business, and Web references it directly.
- **`Wisbar.Integrations` is a leaf that Business depends on**, not a sibling of Business. It references nothing.

## Step 2: What each project is for

| Project | Holds | Must not |
|---|---|---|
| `Wisbar.Optimizely.Core` | CMS/Commerce infrastructure with no site semantics: constants (`Globals`, `WisbarConstants`), attributes, extensions, helpers, identity, middleware, rendering, catalog and cart infrastructure, personalization tracking | Reference anything. It is the floor - keep it free of feature logic |
| `Wisbar.Integrations` | Third-party clients and their models only: `Imis/`, `PaymentProviders/` (Payflow, PayPal), `SharePoint/` | Know about Commerce, content types, or site features. Protocol in, DTOs out |
| `Wisbar.Optimizely.ContentTypes` | Everything Optimizely persists: `Cms/` (pages, blocks, media), `Commerce/` (products, variants, categories, custom property types), `Shared/`, editor descriptors, validators | Contain business logic. These are shapes editors edit |
| `Wisbar.Models` | View and API models: `Blocks/`, `Cart/`, `Checkout/`, `Commerce/`, `Orders/`, `Wishlist/`, `Categories/` | Reference Business or Web. It is a shared vocabulary, not a service layer |
| `Wisbar.Business` | The work: `Features/` (one folder per capability), `Infrastructure/`, `Migrations/`, `ScheduledJobs/`, `Synchronization/` | Reference Web or depend on `HttpContext` |
| `Wisbar.Web` | Delivery: controllers, view components, Razor views, minimal-API endpoints, filters, web infrastructure, wwwroot | Hold business rules that belong in a feature service |
| `Wisbar.Frontend` | React 19 + Vite + SCSS, output to `Wisbar.Web/wwwroot/dist/` | Reference sandbox files from application code |

## Step 3: Where does this go?

| You are adding | Project and folder |
|---|---|
| A page, block, product, variant, category type | `ContentTypes/Cms/…` or `ContentTypes/Commerce/…` |
| A view model for a page or block | `Wisbar.Models/Blocks/` (blocks) or `Wisbar.Web/Models/Pages/` (pages) |
| Business logic for a capability | `Business/Features/<Capability>/Services/` + `Interfaces/` |
| A model shared between Business and Web | `Wisbar.Models/<Area>/` |
| A third-party API client | `Integrations/<Vendor>/` |
| A controller, view component, Razor view | `Wisbar.Web/…` |
| A JSON endpoint | `Wisbar.Web/Api/<Area>Endpoints.cs` |
| A one-off data or schema change | `Business/Migrations/` |
| A CMS-admin-driven job | `Business/ScheduledJobs/` |
| A queued or cron job | `Business/Features/BackgroundJobs/Jobs/` |
| Cross-cutting plumbing (cache, locks, email, DDS) | `Business/Infrastructure/<Concern>/` |
| A constant, attribute, or CMS/Commerce helper | `Optimizely.Core/` |

**The pressure test:** if a class needs `HttpContext`, it belongs in Web. If it needs a third-party protocol, it belongs in Integrations. If Optimizely persists it, it belongs in ContentTypes. Everything else is Business.

## Step 4: Feature folders

`Business/Features/` is the primary organizing unit - roughly 25 of them (Cart, Checkout, Catalog, Search, Pricing, Payments, Wishlist, Shipping, Tax, Registration, OrderExport, SiteSettings, Personalization, Notifications, Pci, and so on).

A feature folder is typically `Interfaces/`, `Services/`, `Models/`, sometimes `Extensions/` or `Export/`. New capability, new folder. Growing an existing one, extend it rather than starting a parallel folder - **there is currently an empty `Features/ClassifiedAds/` beside the real `Features/ClassifiedAd/`**, which is exactly the confusion to avoid.

Services get an interface and are registered in `Startup.ConfigureServices`. Keep the interface next to the implementation's feature, not in a global `Interfaces` project.

## Step 5: Cross-cutting seams

These exist because the naive approach is wrong in a specific way. Use them.

| Seam | Why it exists |
|---|---|
| `ICurrentIdentity` | The CMS scheme is the application default, so `User.Identity.IsAuthenticated` is **true for a signed-in editor**. Use `IsMember`, `IsCmsUser`, `CustomerId`, `MemberId` |
| `IAccountUrls` | The single source for the sign-in URL, with ReturnUrl. Never hardcode `/login` |
| `ICacheService` | The site's cache abstraction; Commerce catalog cache is 5 minutes |
| `IDistributedLock` | Cluster-wide mutual exclusion over Hangfire SQL storage, for at-most-once side effects |
| `IOutOfRequestExecutor` | Runs work off the request thread with a fresh DI scope and suppressed execution-context flow |
| Keyed `IPriceService` | `ServiceKeys.NativePricing` / `CatalogPricing` / `ProfileOnly`. The unkeyed one is cart-aware - a catalog surface must use the catalog lens or two pages disagree about the same product |
| `Decorate<>()` | Used where Optimizely's implementation is internal or must be wrapped (`SiteSettingsManagerCacheDecorator`, `WisbarEntryAttributeService`) |
| `Globals` / `WisbarConstants` | Tabs, content-area tags, cart names, line-item property keys, service keys, view locations. Check here before inventing a string |

## Step 6: Which skill covers this

| Area | Skill |
|---|---|
| Blocks | `optimizely-block` |
| Page types | `optimizely-page-type` |
| Products, variants, categories | `optimizely-catalog-content-type` |
| Order/contact metafields | `commerce-metadata` |
| Data and schema migrations | `optimizely-commerce-migration-step` |
| CMS-admin jobs | `optimizely-scheduled-job` |
| Queued and cron jobs | `hangfire-background-jobs` |
| Small operational state | `optimizely-dds-store` |
| Payments | `payflow-payments` |
| Recommendations | `optimizely-product-recommendations` |
| Wishlist and named carts | `commerce-wishlist-cart` |
| CI/CD workflows and DXP deploys | `github-actions-ci-cd` |

Rules live in `.claude/rules/` (commerce, content-types, frontend) and are always in force. Commerce and Optimizely API reference: `.claude/knowledge-base/`.

## Step 7: Working across layers

A feature that spans layers is normally built bottom-up: content type → model → feature service → endpoint or controller → view → frontend.

- Check whether a **base type** already carries what you need (`PageBaseSeo`, `ProductBase`, `VariantBase`, `BlockBase`) before adding to a concrete type.
- Register services in `Startup.ConfigureServices`; scoped is the default for feature services.
- Mapster for object mapping, configured in `MapsterRegister`. Not AutoMapper.
- Strongly typed options bound from configuration with startup validation, not scattered `IConfiguration` reads.
- Secrets belong in the DXP App Settings portal, read through `IConfiguration`. Never in committed config, and never read `appsettings.Development.json`.

## Step 8: Build and verify

```bash
dotnet build src/Wisbar.sln
dotnet test src/Wisbar.Tests/Wisbar.Tests.csproj
cd src/Wisbar.Frontend && npm run build
```

A layering mistake usually shows up as a missing project reference. **Adding a reference to climb the graph is almost always the wrong fix** - it means the class is in the wrong project. Move the code instead, and say so if the request seems to require the reference.

## Checklist

- [ ] New code sits in the lowest layer that can hold it
- [ ] No new project reference that reverses the dependency direction
- [ ] Business logic is in a feature folder, not a controller or a content type
- [ ] Content types hold shape, not behavior
- [ ] Existing base type checked before adding to a concrete type
- [ ] Cross-cutting seams used instead of re-implementing (identity, cache, locks, pricing lens)
- [ ] Constants taken from `Globals`/`WisbarConstants`, not invented
- [ ] Service registered in `Startup`; options strongly typed and validated
- [ ] Solution, tests, and frontend all build

Detailed map: `references/layer-map.md`.
