# Technology Stack

> Last Updated: 2026-11-11 (upgrade to .NET 10 / CMS 13 / Commerce 15 — ADR-008)

## Stack Overview

| Layer | Technology | Version |
|-------|------------|---------|
| Runtime | .NET | 10.0 (SDK pinned in `/global.json`: 10.0.100, rollForward latestFeature) |
| Language | C# | 14 |
| CMS | Optimizely CMS | 13.6.0 |
| Commerce | Optimizely Commerce | 15.5.0 |
| Search | Optimizely Graph (Content Graph) | 13.6.0 / Commerce 15.5.0 — enabled only when keys are configured |
| Frontend | React | 19.2.0 |
| Bundler | Vite | 7.2.4 |
| Database | SQL Server | Via EPiServer (CMS schema 21007) |
| Testing | MSTest on Microsoft.Testing.Platform | 4.4.1 |
| Cloud | Optimizely DXP / Azure | N/A |

## Backend (.NET 10)

All Optimizely packages are referenced only in `Commerce.Demo.Optimizely.Core` (ADR-002). Keep every `EPiServer.*` CMS package on the same patch version.

### Core Platform
| Package | Version | Purpose |
|---------|---------|---------|
| EPiServer.CMS | 13.6.0 | CMS metapackage (host, core, UI, admin, TinyMCE, Opal) |
| EPiServer.CMS.UI.AspNetIdentity | 13.6.0 | ASP.NET Identity for CMS users (split out of EPiServer.CMS in 13) |
| EPiServer.Cms.UI.VisitorGroups | 13.6.0 | Visitor groups / personalisation (split out in 13) |
| EPiServer.Cms.UI.ContentManager | 13.6.0 | Content Manager editing UI (replaces EPiServer.Labs.ContentManager) |
| EPiServer.OptimizelyIdentity | 13.6.0 | Opti ID; also required at runtime by Commerce 15 even when Opti ID is off |
| EPiServer.Commerce | 15.5.0 | E-commerce platform |
| EPiServer.Events.ChangeNotification | 13.6.0 | Pin: aligns Commerce 15.5's dependency with Framework 13.6 (NU1608) |
| EPiServer.CloudPlatform.Cms / .Commerce | 13.6.0 / 15.1.0 | DXP cloud support |
| EPiServer.Forms / Forms.Samples | 6.0.2 / 5.0.0 | Forms (startup workaround: `FormsAclJobRegistrationInitialization`) |
| EPiServer.Labs.LanguageManager | 6.0.0 | Language management (`AddLanguageManager()` required) |
| Optimizely.Graph.Cms / Optimizely.Graph.Commerce | 13.6.0 / 15.5.0 | Content Graph for CMS + catalog content (replaces EPiServer Find) |

Removed in the upgrade (no CMS 13 build): EPiServer.Find.Cms, EPiServer.Find.Commerce, EPiServer.Commerce.FindSearchProvider, EPiServer.Labs.ContentManager, DoubleJay.Epi.ConfigurableColorPicker, DoubleJay.Epi.EnhancedPropertyList, Geta.Optimizely.DdsAdmin.

### Third-Party Libraries
| Library | Version | Purpose |
|---------|---------|---------|
| Scrutor | 7.0.0 | Assembly scanning and decorator DI registration |
| Mapster | 7.4.0 | Object-to-object mapping |
| PayPalServerSDK | 2.4.0 | PayPal payment gateway |
| Microsoft.Extensions.Http.Resilience | 10.11.0 | HTTP client resilience (10.x sets HttpClient.Timeout to infinite; timeouts live in the pipeline) |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.15 | DI abstractions (Integrations) |
| Hangfire.Core / .SqlServer / .AspNetCore | 1.8.* | Background jobs |
| Serilog.AspNetCore / Settings.Configuration / Sinks.File | 10.0.0 / 10.0.1 / 7.0.0 | Logging (file sink only) |

### Optimizely Add-ons
| Package | Version | Purpose |
|---------|---------|---------|
| Geta.Optimizely.Sitemaps (+ .Commerce) | 5.0.0 | XML sitemap generation (Applications model) |
| Geta.NotFoundHandler.Optimizely (+ .Commerce) | 8.0.0 | 404 handling and redirects (migrates its own schema v2→v3 at startup) |
| Geta.Optimizely.Categories | 2.2.0 | Category content and routing |
| Stott.Security.Optimizely | 7.1.1 | Security response headers |
| Stott.Optimizely.RobotsHandler | 7.0.1 | Robots.txt management |
| CmsContentScaffolding.Optimizely | 2.0.0 | Demo content auto-generation (local feed `D:\Packages` only) |

### Testing
| Package | Version | Purpose |
|---------|---------|---------|
| MSTest | 4.4.1 | Test framework + MTP runner |
| Microsoft.NET.Test.Sdk | 18.10.1 | Test SDK |
| Microsoft.Testing.Extensions.CodeCoverage | 18.11.2 | Code coverage |
| Microsoft.Testing.Extensions.TrxReport | 2.4.1 | Test reporting |
| HttpContextMoq | 1.7.0 | HTTP context mocking |

## Frontend (React 19 + Vite 7)

### Core
| Library | Version | Purpose |
|---------|---------|---------|
| React | 19.2.0 | UI framework |
| React-DOM | 19.2.0 | DOM rendering |
| Vite | 7.2.4 | Build tool and dev server |
| @vitejs/plugin-react | Latest | React support for Vite |

### Development
| Library | Purpose |
|---------|---------|
| ESLint | Code quality / linting |
| @eslint/js | ESLint configuration |
| eslint-plugin-react-hooks | React hooks linting rules |
| eslint-plugin-react-refresh | React refresh linting |
| globals | Global variable definitions |

## NuGet Feeds

Defined in `src\nuget.config`:

| Feed | URL |
|------|-----|
| nuget.org | https://api.nuget.org/v3/index.json |
| optimizely | https://nuget.optimizely.com/v3/index.json |
| Local packages | D:\Packages |

## Development Environment

### Prerequisites
- .NET 10 SDK (10.0.1xx or later, see `/global.json`)
- Node.js (for frontend build)
- SQL Server (LocalDB for development)
- Visual Studio 2022 or VS Code

### Setup
```bash
# Clone and build
git clone [repo-url]
cd OptimizelyCommerceCodebase
dotnet build src\Commerce.Demo.slnx -c Release

# Frontend development (separate terminal)
cd src\Commerce.Demo.Frontend
npm install
npm run dev
```

### Environment Variables / Configuration

| File | Purpose | Environment |
|------|---------|-------------|
| appsettings.json | Base config (DB, logging, SMTP) | All |
| appsettings.Development.json | Local overrides | Development |
| appsettings.Integration.json | CI/CD config | Integration |
| appsettings.Preproduction.json | Staging config | Preproduction |
| appsettings.Production.json | Production config | Production |

### Key Configuration
- **Database:** `EPiServerDB` connection string
- **Azure Blobs:** `EPiServerAzureBlobs` connection string (non-dev)
- **Content Graph:** `Optimizely:ContentGraph:AppKey` / `Secret` (Base64) / `SingleKey` — user-secrets locally, DXP App Settings in cloud
- **Opti ID:** `EPiServer:OptimizelyIdentity:InstanceId` / `ClientId` / `ClientSecret`; local site must run on https://localhost:5000
- **Opal:** `EPiServer:OpalChat:InstanceId` (needs Opal entitlement)
- **PayPal:** Client ID/Secret via PayPalOptions
- **SendGrid/SMTP:** Email provider configuration

### Development Mode Specifics
- Scheduler disabled (`SchedulerOptions.Enabled = false`)
- Debug client resources enabled
- Admin registration for local requests only
- SQL Server LocalDB (App_Data directory)
- Content scaffolding auto-creates demo data (BuildMode.OnlyIfEmpty)
