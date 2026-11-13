# Wisbar layer map

Companion to `SKILL.md`. Folder listings are a snapshot - re-list before relying on one.

## Project references, as declared

| Project | References |
|---|---|
| `Wisbar.Web` | `Wisbar.Business`, `Wisbar.Models` |
| `Wisbar.Business` | `Wisbar.Integrations`, `Wisbar.Models`, `Wisbar.Optimizely.ContentTypes` |
| `Wisbar.Models` | `Wisbar.Optimizely.ContentTypes` |
| `Wisbar.Optimizely.ContentTypes` | `Wisbar.Optimizely.Core` |
| `Wisbar.Optimizely.Core` | *(none)* |
| `Wisbar.Integrations` | *(none)* |
| `Wisbar.Tests` | `Wisbar.Business`, `Wisbar.Web` |
| `Wisbar.Frontend` | *(not a .NET project; builds into `Wisbar.Web/wwwroot/dist/`)* |

Legal direction is strictly downward. Anything that would need an upward reference is in the wrong project.

## Wisbar.Optimizely.Core

The floor. No project references, no feature logic.

```
Attributes/        SiteImageUrlAttribute, validation attributes
Constants/         Globals, WisbarConstants
Extensions/
Helpers/           PageHelpers, CommerceHelpers, OrderMetaFieldHelper
Infrastructure/
  Cart/            Cart infrastructure
  Catalog/         Catalog infrastructure
  Channels/
  EditorDescriptors/
  Identity/        ICurrentIdentity and friends
  Middleware/
  Rendering/       SiteViewEngineLocationExpander
Personalization/
  Tracking/        WisbarTrackingDataFactory
```

### Constants worth knowing before inventing a string

`Globals`: `Tabs` (editor tabs), `GroupNames`, `CartNames`, `LineItemProperties`, `SiteUIHints`, `ContentAreaTags`, `ViewLocations`, `Images`.

`WisbarConstants`: `ProductFamily`, `DynamicDataStoreName`, `ContactDetail` / `OrganizationDetail` / `AddressDetail`, `ServiceKeys`, `Authentication`, `ScheduledContentSites`, `Delivery`, `AssetFolders`.

## Wisbar.Integrations

Third-party protocol only. References nothing.

```
Imis/                   iMIS API client, DTOs
PaymentProviders/
  Payflow/              Gateway, NVP codec, options, field/host rules, safe logging
  PayPal/               Interfaces, Mappings, Models, Results
SharePoint/
```

Rule: protocol in, DTOs out. No Commerce types, no content types, no site rules.

## Wisbar.Optimizely.ContentTypes

Everything Optimizely persists.

```
Cms/
  Pages/        Base/ (PageBase, PageBasePublic, PageBaseSeo, FunctionalPageBase,
                       StartPageBase, SiteSettingsPageBase, NotFoundPageBase, ContainerBase)
                Interfaces/ (ILayoutPageBase, IExcludeFromSitemap, ISearchable, IListable, …)
                Concrete pages
  Blocks/       Base/BlockBase, Local/ (LinkBlock, IconLinkBlock, …), Enums/, Interfaces/
  Media/        Base/ImageFileBase and friends
Commerce/
  Products/     Base/ProductBase, Interfaces/, concrete products
  Variants/     Base/VariantBase, AddOnVariantBase, Interfaces/, concrete variants
  Categories/   Base/CategoryBase
  Bundles/  Packages/
  Properties/   Custom property types (PropertyList<T> + [PropertyDefinitionTypePlugIn])
  EditorDescriptors/  SelectionFactories/  Interfaces/
Shared/         Blocks/ (SeoBlock, OpenGraphBlock, AccordionAnchorBlock), Categories/
EditorDescriptors/  Extensions/  Validators/
```

Shape, not behavior. A content type may expose computed convenience properties, but a service belongs in Business.

## Wisbar.Models

Shared vocabulary between Business and Web. References ContentTypes only.

```
Blocks/       Block view models, BlockViewModel<TBlock> base
Cart/         CartModel and friends
Categories/   Checkout/   Commerce/   Orders/   Wishlist/
```

Page view models are the exception: they live in `Wisbar.Web/Models/Pages/` because they carry `LayoutModel`, which is a Web concern.

## Wisbar.Business

```
Features/         ~25 capability folders (see below)
Infrastructure/
  Caching/        Concurrency/ (IDistributedLock, HangfireDistributedLock)
  Cookies/        DDS/ (entities) 
  Email/          Execution/ (IOutOfRequestExecutor)
Migrations/       IMigrationStep implementations + Scripts/ + Data/
ScheduledJobs/    Optimizely [ScheduledPlugIn] jobs
Synchronization/  iMIS import services, import runners, run store
```

### Feature folders

`Authentication`, `BackgroundJobs`, `BooksSubText`, `Breadcrumbs`, `Cart`, `Catalog`, `Checkout`, `ClassifiedAd`, `Events`, `Notifications`, `OrderConfirmation`, `OrderExport`, `Payments`, `Pci`, `Personalization`, `Pricing`, `PromotedResults`, `Registration`, `Search`, `Shipping`, `SiteContent`, `SiteSettings`, `Sitemaps`, `Tax`, `Time`, `Wishlist`.

Typical shape:

```
Features/<Capability>/
  Interfaces/     I<Capability>Service, builders
  Services/       Implementations
  Models/         Feature-internal models (shared ones go to Wisbar.Models)
  Extensions/     Optional
```

**Housekeeping note:** `Features/ClassifiedAds/` exists and is empty; the real feature is `Features/ClassifiedAd/`. Do not add to the empty one.

## Wisbar.Web

```
Api/              Minimal-API endpoint classes + Requests/ + Responses/
Components/       View components, Base/ (BlockComponentBase, AsyncBlockComponentBase, …)
Controllers/      Page and product controllers, Base/, Cms/ (admin tools)
Filters/          PageContextActionFilter, HangfireDashboardAuthorizationFilter
Helpers/          ViewModelBuilderHelpers
Infrastructure/   Startup wiring, renderers, Mapster, PCI, Recommendations, EnvironmentLogin
Models/           Pages/, Blocks/, Commerce/ (+ Builders/), Cms/, Layouts/, Components/
Resources/        Views/            wwwroot/
```

### API endpoints

Each area is a static class with an `IEndpointRouteBuilder` extension, registered in `Startup`'s `UseEndpoints`:

`AddApiEndpoints`, `AddCartEndpoints`, `AddCheckoutEndpoints`, `AddWishlistEndpoints`, `AddProductSearchEndpoints`, `AddContentTreeEndpoints`, `AddSiteContentEndpoints`, `AddOrderExportEndpoints`, `AddClassifiedAdExportEndpoints`, `AddPayflowCallbackEndpoints`, `AddDevOrderConfirmationEmailEndpoints` (Development only).

Shape to copy:

```csharp
public static IEndpointRouteBuilder AddWidgetEndpoints(this IEndpointRouteBuilder builder)
{
    var group = builder.MapGroup("/api/widgets").RequireAuthorization(Authentication.MemberPolicy);
    group.MapGet("/", GetWidgets);
    return builder;
}
```

There is no `IApiService` interface in this solution - the extension-method registration above is the whole pattern.

## Wisbar.Tests

MSTest, mirroring the Business/Web structure: `Business/`, `Features/`, `Web/`, `Infrastructure/`, `Migrations/`, `Synchronization/`, `Extensions/`, `Helpers/`, plus `MockHelpers.cs` and `TestsInitialization.cs`. `.runsettings` sets `ASPNETCORE_ENVIRONMENT=UnitTest`.

## Wisbar.Frontend

```
src/
  styles/          Global SCSS partials, registered in main.scss with @use
  components/      React components with co-located SCSS
  main.scss        bootstrap-custom.scss
sandbox/           Standalone HTML previews - never referenced from application code
bundle.config.js   Non-Vite bundle outputs (main.min.js, main.min.css)
```

Output targets: React needing server integration → `Wisbar.Web/wwwroot/ReactComponents/`; plain JS → `wwwroot/js/`; CSS → `wwwroot/css/`; Vite build → `wwwroot/dist/`.

## Startup composition

`Wisbar.Web/Startup.cs` is the composition root, roughly in order: CMS/Commerce registration, Mapster, feature services, view-model builders, Hangfire + email, payment providers, HTTP clients, options binding with validation, display options, MVC filters. Then `Configure`: middleware, Hangfire dashboard, recurring job registration, endpoints.

Conventions:

- Scoped for feature services; singleton for stateless infrastructure.
- `Decorate<>()` where Optimizely's implementation is internal or must be wrapped.
- Keyed services for the pricing lenses (`ServiceKeys`).
- Options bound with `BindConfiguration` plus `ValidateDataAnnotations().ValidateOnStart()` where practical.
- `if (env.IsDevelopment())` guards for anything that cannot work locally.

## Environments

`appsettings.{Environment}.json` for Development, Integration, Preproduction, Production. Connection strings `EPiServerDB` (CMS) and `EcfSqlConnection` (Commerce, also Hangfire's schema). Secrets live in the DXP App Settings portal. Deployment promotes Integration → Preproduction → Production with manual approval gates.

Never read or modify `appsettings.Development.json`, `.env`, or `secrets/**`.
