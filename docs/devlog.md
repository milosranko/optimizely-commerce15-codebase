# Development Log

## Session 1 - 2026-02-09
**Duration:** Initial session
**Focus Area:** F3 Framework Setup (Retroactive)

### Accomplished
- Analyzed existing codebase architecture and dependency graph
- Set up F3 documentation system retroactively for existing project
- Created CLAUDE.md (F3-compliant) with project context, standards, and session protocol
- Created /docs/architecture.md documenting system design from code analysis
- Created /docs/decisions.md with 6 retroactive ADRs inferred from codebase
- Created /docs/stack.md with complete technology inventory
- Created /docs/api.md documenting existing API endpoints
- Created /docs/todo.md with initial priorities
- Created /docs/changelog.md with release history
- Reviewed solution structure and provided improvement recommendations
- Created SPEC.md (retroactive) — 12 feature areas documented with acceptance criteria
- Created PLAN.md (retroactive) — 8 phases documented, forward plan with priorities

### Decisions Made
- Adopted F3 (Foundation-First Framework) v2.3.2 for project documentation and development methodology
- Documented 6 retroactive ADRs capturing existing architectural decisions

### Blockers/Issues

**Hard Stops (Session-Ending):**
- None

**Soft Issues (Worked Around):**
- None

### Next Session
- [ ] Define current development priorities in todo.md
- [ ] Address identified architectural concerns (dual SearchService, Core project scope)
- [ ] Begin Priority 1 work: Wire up e-commerce flow (PayPal → Checkout)
- [ ] Fill in [TODO] sections across documentation

### Notes
- This is a retroactive F3 setup for an existing project
- All ADRs marked as [RETROACTIVE - Inferred] since original decisions predate F3 adoption
- Several [TODO] items in documentation need owner input

---

## Session 2 - 2026-05-30
**Duration:** Single session
**Focus Area:** Cookies Consent — analysis and hardening (P0–P4)

### Accomplished
Analyzed the cookies consent feature end-to-end and implemented a prioritized improvement roadmap. Each item was built as a discrete component with a build verification.

- **P0 — Enforcement bug fix:** Removed a malformed `;secure` suffix from the consent cookie *value* that caused the last accepted category's scripts to silently never load; added real `Secure`/`SameSite=Lax` cookie attributes.
  - `Views/Shared/_CookiesConsentDialog.cshtml`
- **P1 — GDPR/consent correctness:** Symmetric expiry for accept/deny; the CMS `CookieExpirationDate` now drives the cookie lifetime (was dead config); policy-version changes re-prompt via a companion `{CookieName}-policy` cookie; persistent "Cookie settings" withdraw/re-open trigger; essential snippets always render server-side regardless of the client payload; fixed `getCookie` value parsing.
  - `_CookiesConsentDialog.cshtml`, `Resources/Translations/Views.xml`, `Helpers/HtmlHelpers.cs`
- **P2 — Security hardening:** Enum/length validation of the consent payload; per-IP rate limiting on `POST /api/consent/audit` (429); IP anonymization before storage (GDPR data minimization); CSV formula-injection mitigation in the audit export.
  - `Api/Endpoints.cs`, `Startup.cs`, `Controllers/CookieConsentAuditController.cs`
- **P3 — Performance:** `GetStatistics` no longer materializes the whole store (predicate counts + `ICacheService` caching, invalidated on purge); action normalized to canonical casing on write; CSV export bounded by `ExportMaxRows` with truncation logging.
  - `Features/CookieConsentAudit/Services/CookieConsentAuditService.cs`, `.../Options/CookieConsentAuditOptions.cs`, `Controllers/CookieConsentAuditController.cs`
- **P4 — Accessibility:** `role="dialog"`+`aria-modal`+`aria-label`+focus management for the banner; `aria-expanded`/`aria-controls` on the customize toggle; `aria-describedby` on category inputs; Deny visually distinguished from Accept.
  - `_CookiesConsentDialog.cshtml`, `Resources/Translations/Views.xml`

### Decisions Made
- Added ADR-007 (cookie consent capture hardening — validation, rate limiting, IP anonymization, statistics caching).
- Declined adding CORS to the consent endpoint (browser-only control, bypassed by non-browser clients, default is already deny-cross-origin); the P2 controls cover the real threat.

### Blockers/Issues
**Hard Stops:** None
**Soft Issues:**
- Single-project `dotnet build` fails on a `$(SolutionDir)`-based PostBuild target; build with `-p:SolutionDir=...\src\`. File-lock errors (`MSB3026/7`) appear only when the app is already running.

### Next Session / Deferred
- [ ] Migrate audit storage off DynamicDataStore to a dedicated SQL table (ADR-worthy; enables single-`DELETE` purge + indexed aggregation)
- [ ] Verify DynamicDataStore LINQ predicate counts translate to SQL at runtime (P3)
- [ ] Configure `ForwardedHeaders` with known proxies (infra-dependent) for trustworthy client IPs
- [ ] Consider `navigator.sendBeacon` for consent capture (fixes reload race) and a vanilla-JS/React rewrite to drop the jQuery dependency

### Notes
- All changes verified with `dotnet build` (0 errors); browser/runtime verification steps recorded per component.

---

## Session 3 - 2026-11-11 — Upgrade to .NET 10 / CMS 13 / Commerce 15
**Focus Area:** Platform upgrade (optimizely-cms13-upgrade, phased). No git repo: user handles backups, no per-phase commits.
**Decisions (user):** System.Text.Json instead of Newtonsoft; enable Opti ID, Content Graph, Content Manager/Opal; empty local DBs from appsettings.Development.json (fresh install, not a data upgrade).
**Baseline (before any change):** build 0 errors / 80 warnings; tests 1/1 passed (run via test exe: `dotnet test` on SDK 10 refuses VSTest mode).

### Phase 1 — .NET 10 ✅
- All 7 `.csproj` → `net10.0`; `Microsoft.Extensions.DependencyInjection.Abstractions` 8.0.2 → 10.0.15, `Microsoft.Extensions.Http.Resilience` 8.10.0 → 10.11.0.
- Pinned `System.Security.Cryptography.Pkcs` 10.0.15 in Optimizely.Core (transitive 10.0.0 has GHSA-2fmv-rv2c-59x7 / GHSA-846f-f764-76mx).
- Added `/global.json` (SDK 10.0.100, rollForward latestFeature, test runner = Microsoft.Testing.Platform). `dotnet test` now needs `--project <csproj>`.
- CI `pipelines/build-deploy-github.yml`: `DOTNET_VERSION` 8.0.x → 10.0.x.
- Proof: `dotnet build src/Commerce.Demo.slnx -c Release` → 0 errors; `dotnet test --project src/Commerce.Demo.Tests/Commerce.Demo.Tests.csproj -c Release` → 1/1 passed.

### Phase 2 — Latest CMS 12 / Commerce 14 (stepping stone) ✅
- No version bumps needed: every Optimizely and add-on package is already on the newest release of its CMS 12 line (EPiServer.CMS 12.35.0, Commerce 14.48.0, Forms 5.10.10, CloudPlatform.Cms 1.8.0, Find 17.1.0 …).
- Obsolete sweep (CS0618/CS0612, full rebuild, no suppressions present): only `InlineBlockEditSettingsAttribute` (TeaserBlock.cs, LabelXhtmlBlock.cs). The scanner's ~60 rule hits remain the Phase 4 to-do list.
- CMS 13 add-on availability check (nuspec dependency ranges):
  - Have CMS 13 builds: Geta.NotFoundHandler(.Commerce) 7.1.0/8.0.0, Geta.Optimizely.Sitemaps(.Commerce) 4.0.0/5.0.0, Geta.Optimizely.Categories 2.x, Stott.Security 7.1.1, Stott.RobotsHandler 7.0.1, EPiServer.Forms 6.0.2, Forms.Samples 5.0.0, Labs.LanguageManager 6.0.0, CmsContentScaffolding.Optimizely 2.0.0 (local D:\Packages only).
  - **No CMS 13 build:** EPiServer.Find.Cms 17.1.0, EPiServer.Find.Commerce 12.3.1, EPiServer.Commerce.FindSearchProvider 12.3.1, DoubleJay.Epi.ConfigurableColorPicker 3.0.0, DoubleJay.Epi.EnhancedPropertyList 2.0.0, Geta.Optimizely.DdsAdmin 1.0.2, EPiServer.Labs.ContentManager 1.2.0 (superseded by EPiServer.Cms.UI.ContentManager). User decisions (2026-11-11): remove Find (3 packages, FindCommerceOptions, unused SearchService/ISearchService), Content Graph becomes the search path; ColorPicker → string hex property with regex validation + native color input editor; remove EnhancedPropertyList and DdsAdmin; Labs.ContentManager → EPiServer.Cms.UI.ContentManager. (ADR to be added in Phase 3.)
- Proof: `dotnet build src/Commerce.Demo.slnx -c Release --no-incremental` → 0 errors, 140 warnings (4 CS0618 lines = 2 unique).

### Phase 3 — CMS 13 / Commerce 15 packages ✅ (restore clean; build expected red until Phase 4)
- `src/nuget.config`: feed → `https://nuget.optimizely.com/v3/index.json` (kept `<clear />` and local `D:\Packages`, needed for CmsContentScaffolding 2.0.0).
- Bumped: EPiServer.CMS 12.35.0 → 13.6.0, Commerce 14.48.0 → 15.5.0, CloudPlatform.Cms 1.8.0 → 13.6.0, CloudPlatform.Commerce 1.2.0 → 15.1.0, Forms 5.10.10 → 6.0.2, Forms.Samples 4.2.7 → 5.0.0, Labs.LanguageManager 5.5.4 → 6.0.0, Geta.NotFoundHandler(.Commerce) 6.1.0 → 8.0.0, Geta.Optimizely.Categories 1.1.3.105 → 2.2.0, Geta.Optimizely.Sitemaps(.Commerce) 3.2.1 → 5.0.0, Stott.Optimizely.RobotsHandler 6.0.2 → 7.0.1, Stott.Security.Optimizely 6.1.0 → 7.1.1, CmsContentScaffolding.Optimizely 1.13.1 → 2.0.0.
- Added: EPiServer.CMS.UI.AspNetIdentity, EPiServer.Cms.UI.VisitorGroups, EPiServer.Cms.UI.ContentManager, EPiServer.OptimizelyIdentity (all 13.6.0); pin EPiServer.Events.ChangeNotification 13.6.0 (NU1608 from Commerce 15.5.0).
- Removed: EPiServer.Find.Cms, EPiServer.Find.Commerce, EPiServer.Commerce.FindSearchProvider, EPiServer.Labs.ContentManager, DoubleJay.Epi.ConfigurableColorPicker, DoubleJay.Epi.EnhancedPropertyList, Geta.Optimizely.DdsAdmin; and the Phase 1 Pkcs pin (CMS 13 resolves patched 10.0.13).
- ImageSharp now 3.2.0 via CMS 13 → its NU190x advisories are gone. `.gitignore` `modules/` already matches at any depth.
- ADR-008 added to decisions.md.
- Proof: `dotnet restore src/Commerce.Demo.slnx --force` → 0 warnings/errors. `dotnet build` → 3 errors in Optimizely.Core (UriSupport, GeolocationProviderBase) — downstream projects not yet compiled.

### Phase 4a — Startup + removed add-on code ✅ (not yet compiler-verified downstream of Optimizely.Core)
- `Startup.cs`: added `AddCms()` (required before `AddCommerce()` in CMS 13), `AddVisitorGroupsCore/Mvc/UI()`, `AddForms()`; removed `AddFind`, `AddFindSearchProvider`, Labs `AddContentManager` (new Content Manager registered in Phase 7), `AddConfigurableColorPicker`, `AddEnhancedPropertyList`, `AddOptimizelyDdsAdmin`, `FindCommerceOptions`, `ISearchService` registration and their usings.
- Deleted Find-based `ISearchService` / `SearchService` (no callers). Kept Find-free search models/enum for a future Graph port.
- JSON: no Newtonsoft attributes/usage anywhere → MVC/minimal API already on System.Text.Json; no wire-format change.
- Colour: `EventTuitionVariant.ColorPicker` (IColor) → new `DisplayColor` string property with `[UIHint(HexColor)]`, `[RegularExpression(#RRGGBB)]`, `[StringLength(7)]`; new `HexColorEditorDescriptor` (Core) + `wwwroot/ClientResources/Scripts/editors/HexColorPicker.js` (native colour input + hex text box). `Globals.SiteUIHints.HexColor`, `Globals.Validation.HexColorPattern` added.
- Removed stale module folders of dropped add-ons from `Core/modules/_protected` and `Web/modules/_protected` (DoubleJay ×2, Find, Geta.DdsAdmin, episerver-labs-content-manager) — generated, git-ignored; would otherwise be loaded by the shell at startup.
- Pulled forward from 4e (blocked all verification): `UriSupport` → `IUriSupport.AbsoluteUrlBySettings`, `GeolocationProviderBase` → `IGeolocationProvider.Lookup` (signatures confirmed in 13.6.0 XML docs).
- Proof: `dotnet build` → Optimizely.Core compiles; next blocker 4 errors in ContentTypes (`InlineBlockEditSettings` removed in CMS 13 → step 4d). Web/ContentTypes 4a edits verified only once the build reaches them.

### Phase 4b — InlineBlockEditSettings + mechanical renames ✅ (Web project not yet compiled)
- `InlineBlockEditSettings` (removed in CMS 13; inline blocks have no Name): removed from TeaserBlock and LabelXhtmlBlock; `InlineBlockNamePropertiesOptions` in Startup labels them by `TeaserBlock.Heading` / `LabelXhtmlBlock.Label` (per Optimizely docs "Improve publishing with inline edit blocks").
- `EPiServer.Framework.Validator` (removed, CS0619) → `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrEmpty` in 3 selection factories (pulled forward, blocked compile).
- `[ScheduledPlugIn]` → `[ScheduledJob]` (CookieConsentAuditPurgeJob; GUID kept, DisplayName 27 chars < 50). Commented-out SiteMapScheduledJob left alone.
- `[PropertyDefinitionTypePlugIn]` → `[PropertyDefinitionType]` in 7 property-list types; no `SaveData` overrides.
- `ContentArea.FilteredItems` → new `ContentAreaExtensions.GetFilteredItems()` (Core/Extensions) in PageViewContextFactory, MapsterConfig.
- `PageReference` extensions → `ContentReference`; removed the former PageReference `GetPage<T>` overload (duplicate signature with existing `GetPage<T>(ContentReference)`; no callers).
- `context.Locate.Advanced` / `context.Locate.ContentEvents()` → `context.Services` in CmsInitialization (scanner missed it), CommerceInitialization, OrderEventInitialization.
- `SiteDefinition.Current.StartPage` → `ContentReference.StartPage` (SiteSettingsManager, BlockPreviewController, _Header.cshtml); `GlobalAssetsRoot` → `SystemDefinition.Current`.
- Proof: `dotnet build` → Core, ContentTypes, Models clean; Business 9 errors, all pre-existing 4d/4e items (static CacheManager ×7, Sitemaps 5 generator ctor, Commerce `AddTaxValueRow`). New obsolete warnings to handle in 4d/5: `PropertyDictionaryMultiple` (FashionProduct ×2, EventQuestionProperty), `IPageRouteHelper`, remaining SiteDefinition APIs.

### Phase 4c — Tab (group) names ✅
- `Globals.Tabs`: "Aside content" → `Aside_content`, "Site Settings" → `Site_Settings`, "Cookies Consent" → `Cookies_Consent` (CMS 13 rejects spaces). Editor labels unchanged via existing `[Display(Name)]`; comment added in Globals.cs.
- Only consumer: `SiteSettingsPageBase` (via constant). No inline `GroupName` literals with spaces, no tab translations, no `SystemTabNames.PageHeader`.
- **Follow-up for deployment:** tab access rights / ordering set in Admin mode for the three old tab names must be re-applied (new tab identifiers).
- Proof: scanner → 0 `group-*` hits; `dotnet build` → same 9 Business errors as after 4b (no new errors).

### Phase 4d-1 — Remaining build errors ✅ — solution builds green
- `CacheService`: static `CacheManager` (removed) → constructor-injected `ISynchronizedObjectInstanceCache` (`Get`/`Insert`/`Remove`/`RemoveLocal`); absolute expiry kept.
- `DefaultSitemapXmlGenerator`: Sitemaps 5 base ctor (probed via reflection) — `ISiteDefinitionRepository` → `IApplicationRepository`, dropped `ISynchronizedObjectInstanceCache`/`IMemoryCache`; `AddFilteredContentElement` override unchanged.
- `Migration3TaxInitialization`: Commerce 15 `AddTaxValueRow` lost the trailing SiteId `Guid` (column removed; code passed `Guid.Empty` = all sites) → 5-arg overload (probed).
- Web, first time compiled: `using EPiServer.DependencyInjection` for `AddCms`/`AddAzureBlobProvider`/`AddCmsCloudPlatformSupport`/`AddAdminUserRegistration`; `UriSupport` → `IUriSupport` in PageViewContextFactory; `CustomContentAreaRenderer` merge rewritten for `RenderSettings` now `IDictionary<string,string>` (same precedence); `_ShellLayout.cshtml` → `<platform-navigation />` / `<platform-navigation-wrapper>` + `@addTagHelper *, EPiServer.Shell.UI` in `_ViewImports.cshtml`.
- This confirms the 4a/4b Web edits compile (Startup, init modules, FilteredItems, _Header, BlockPreviewController).
- Proof: `dotnet build src/Commerce.Demo.slnx -c Release` → **0 errors**, 41 warnings; `dotnet test --project src/Commerce.Demo.Tests/Commerce.Demo.Tests.csproj -c Release` → **1/1 passed** (= baseline).
- Left for 4d-2: SiteDefinition API (ISiteSettingsManager signature, cache key Id, SiteAssetsRoot), IPageRouteHelper ×3, PropertyDictionaryMultiple ×3, UploadOptions → MediaFileOptions ×3, Searchable (keep).

### Phase 4d-2 — Obsolete-API judgement calls ✅
- SiteDefinition → Applications (members confirmed in 13.6.0 XML docs):
  - `ISiteSettingsManager.SpecificSiteSettings(SiteDefinition, …)` → `(IRoutableApplication, …)` — **interface signature change** (no callers); `site.StartPage` → `application.EntryPoint`.
  - `SiteSettingsManagerCacheDecorator`: cache key `SiteDefinition.Current.Id` → `IApplicationResolver.GetByContext().Name`. Fixed latent bug: `SpecificSiteSettings` was cached under the *current* site's key; now keyed by the requested application.
  - `FilesImportService.ResolveAssetsRoot`: `SiteAssetsRoot` → `(GetByContext() as IResourceableApplication)?.AssetsRoot`; `IApplicationResolver` injected.
- `IPageRouteHelper.Page` → `IContentRouteHelper.Content as PageData` in BlockBase, BlockComponentBase, AsyncBlockComponentBase (`Lazy<PageData?>`; null on non-page routes, e.g. catalog).
- `UploadOptions` → `MediaFileOptions` (`FileSizeLimit`, 60 MB) in Startup and FilesImportController. `appsettings.json` `EPiServer:CmsUI:Upload` section now only feeds the obsolete options (code sets the limit).
- `[Searchable]` ×17 kept as-is (still `IndexingType.Searchable` in CMS 13; used by Content Graph).
- `PropertyDictionaryMultiple` → `PropertyStringDictionaryMultiple` (FashionProduct.AvailableSizes/AvailableColors, EventQuestionProperty.PurchaseClaims) — user decision: switch **without** data migration. Probed: old = `PropertyLongString` ("S, M, L"), new = `PropertyJson`; existing values in populated DBs must be re-entered. Enables Content Graph string-list indexing.
- Proof: `dotnet build --no-incremental` → 0 errors, **0 CS0618/CS0612**; tests 1/1 passed.

### Phase 5 — Obsolete sweep ✅
- Scanner after: only `searchable-attribute` ×17 left (kept by decision); OptimizelyIdentity runtime check now passes. Build: 0 errors, 0 CS0618/CS0612.
- Razor views / code / config: no remaining CMS 12 helpers (AddOns.Helpers, IPageRouteHelper, platform navigation HTML helpers, FilteredItems, SiteDefinition — comments only).
- Back-office paths: no IP allow-list / path-based access middleware exists. Hangfire dashboard at `/episerver/hangfire` is protected by role checks (WebAdmins/CmsAdmins outside Development) + `[Authorize]` on HangfireCms — not path-dependent; path left unchanged (moving under `/optimizely` could collide with CMS 13 module routing). Pre-existing: `Views/HangfireCms/Index.cshtml` hard-codes the path instead of `HangfireOptions.DashboardPath`.
- JSON: no Newtonsoft attributes/usages anywhere → no silent wire-format change.
- Config: removed dead `EPiServer:Find` and `EPiServer:FindCommerce` sections from `appsettings.Development.json`. **Security:** the Find ServiceUrl contained an index private key (32-char path segment) — revoke/rotate it in the Find portal (value not copied anywhere).
- Proof: `dotnet build --no-incremental` → 0 errors; tests 1/1 passed.

### Phase 6 — Database (fresh install) + first site start ✅ (blocker resolved 2026-11-11)
- DBs: local `Optimizely.Cms13.Demo` / `Optimizely.Commerce15.Demo` (SQL Server 2022), empty → **fresh install**, not an upgrade over data. Schema created on first start: CMS `sp_DatabaseVersion` = **21007** (CMS 13.6). Upgrade-over-existing-data path (custom index cleanup via `ISchemaValidator`, references/database.md) **not verified** — needs a production copy.
- Startup bugs found and fixed (build + tests were green for all of them):
  1. `EPiServer.Cms.UI.ContentManager` referenced but not registered → DI crash (`ContentManagerConfigurationResolver`). Added `AddContentManager()`.
  2. Commerce metafield clash: new hex `Color` property vs `FashionVariant.Color` (catalog properties are shared metafields by name) → renamed to `EventTuitionVariant.DisplayColor`.
  3. **Forms 6.0.2 bug** (decompiled): `InitializationModule` runs `UpdateUploadFolderACLJob` when its job row is missing and `DisableJob()` dereferences that missing row → NRE. In CMS 13 jobs are registered later by the `SchedulerService` hosted service, so every fresh DB crashes. Added `Infrastructure/FormsAclJobRegistrationInitialization` (registers the row first; same module dependencies as Forms' module so the engine's dependency-count/full-name ordering runs it first — verified in log).
  4. `EPiServer.Labs.LanguageManager` 6 needs explicit `AddLanguageManager()` (`ILanguageManagerConfig` not registered).
  5. `CookieConsentAuditPurgeJob` used placeholder GUID `A1B2C3D4-…-EF1234567890`, which Commerce 15's webhooks job also uses → whole scheduled-job sync failed. New GUID `DDA1D3ED-5B6D-45E2-98BF-F058DF430AAA` (`CookieConsentAuditPurgeJob.JobId`, also used by StartPageController). Existing environments lose this job's history.
  6. Removed 4 stale CMS 12 module folders from `modules/_protected` (Core + Web): EPiServer.Cms.TinyMce 4.8.3, EPiServer.Cms.UI.Admin/Settings/VisitorGroups 12.35.0 (CMS 13 renamed modules; old folders were never overwritten).
- Tried and reverted (not the cause): scheduler `Paused` instead of `Enabled=false`; removing duplicate `MapRazorPages()`.
- Site now **starts**: initialization completes, scheduled-job sync OK, `/` → 302 `/Util/Register` (fresh install, no users), `/api/subpage` 200, `/robots.txt` 200, `/episerver/hangfire` 200, MVC auth challenge works. Expected warnings: Content Graph content-source provisioning 401 (no Graph keys until Phase 7).

**BLOCKER — CMS back-office routes return 404 on the fresh install**
- **Issue:** `/Util/Register`, `/Util/Login`, `/Optimizely/CMS/`, `/Optimizely/Shell/` → 404 (site NotFound page), on both `http://localhost:5987` and the configured app host `https://localhost:5000`.
- **Impact:** cannot create the first admin user or open the CMS/Commerce UI → full smoke test (page/block/catalog/cart, Phase 8) and Opti ID/Graph/Content Manager configuration (Phase 7) cannot be verified.
- **Evidence:** routing debug log — the shell endpoint *is* a candidate (`DefaultShellModuleController.Index`, pattern `Optimizely/CMS`) but the request ends "did not match any endpoints", i.e. an endpoint selector policy rejects it after matching. All 21 shell modules load (13.6 names); only CMS's own matcher policies exist (`ContentMatcherPolicy`, `MethodOverrideMatcherPolicy`) plus `ControllerActionEndpointSelectorPolicy` / `PageActionEndpointSelectorPolicy`. Application row exists (`Wisbar_Marketplace_Development`, host `localhost:5000`, type Primary).
- **Not yet ruled out:** `ControllerActionEndpointSelectorPolicy` rejecting on unexpected route values; ASP.NET built-in Host/HttpMethod policies; middleware order (`UseNotFoundHandler` + `UseStatusCodePagesWithReExecute` before `UseRouting`); whether the same occurs on a vanilla CMS 13.6 site.
- **Proposed resolution:** temporary Development-only diagnostic endpoint that dumps the shell endpoints' metadata and runs the candidate through each policy, or compare with a minimal CMS 13.6 + Commerce 15.5 site against a scratch DB. Awaiting user decision.
- Proof: `dotnet build` 0 errors; tests 1/1 passed; start logs in scratchpad (run1–run15).
- **Resolution:** user verified manually (Debug build from the launch profile, `https://localhost:5000`): site runs, first admin registered via `/Util/Register`, CMS login and UI work. The 404s were specific to the headless harness (`dotnet run -c Release --no-build --no-launch-profile`); exact cause not determined — treat headless back-office probes as unreliable for this solution.
- Production DB upgrade guide: `docs/plans/cms13-database-upgrade.md` (discovery query + `ISchemaValidator` cleanup template; target `sp_DatabaseVersion` 21007).

### Phase 7 — Optional CMS 13 features ✅ (code + wiring verified; real credentials pending)
- Local URL already `https://localhost:5000` (launchSettings, scaffolding SiteHost) = Opti ID's registered local redirect URI → no port changes.
- **Content Manager:** registered since Phase 6 (`AddContentManager()`); needs Graph keys to be useful.
- **Content Graph:** added `Optimizely.Graph.Cms` 13.6.0 + `Optimizely.Graph.Commerce` 15.5.0 (Core). `services.AddCommerceGraph()` (calls `AddContentGraph()` itself; indexes CMS + catalog → replaces Find) — registered **only when** `Optimizely:ContentGraph:AppKey` + `Secret` are set. Verified: with keys set (env vars) the site reaches Graph ("Invalid credentials" with dummies). Not always-registered because Graph's `EnsureSchemaInitializationModule` contacts Graph at startup and the site fails to start without valid keys (tested). Without keys: 4 Graph scheduled jobs log "Failed to create an instance" at startup (expected), plus the existing content-source 401 warnings from Content Manager.
- **Opti ID:** `AddOptimizelyIdentity(useAsDefault: false, bind EPiServer:OptimizelyIdentity)` — back office only, ASP.NET Identity kept for the site; registered **only when** InstanceId + ClientId + ClientSecret are set. Options bound explicitly (convention binding verified not to work: ClientId stayed empty → every request 500). Verified with dummy credentials: site starts, `/api` 200.
- **Opal:** `AddOpalChat` is inside `AddCms()`; `OpalChatOptions` bound explicitly from `EPiServer:OpalChat` (InstanceId). Needs the Opal entitlement.
- `appsettings.json`: empty placeholders for `Optimizely:ContentGraph` (GatewayAddress `https://cg.optimizely.com`), `EPiServer:OptimizelyIdentity`, `EPiServer:OpalChat`. Web project got a `UserSecretsId` for local secrets. Startup logs feature status (never values).
- Found (pre-existing): `StartPageController` posted a notification from user `"admin"` on every visit → 500 for all visitors when no user named "admin" exists (seen with Opti ID enabled). **Fixed by the user** (demo notification, price-type and job-log lines commented out).
- Proof: build 0 errors; tests 1/1; starts verified: nothing configured (OK, /api 200), Graph dummy keys (reaches Graph), Opti ID dummy credentials (OK, /api 200).

### Phase 8 — Verify and report ✅
- Clean build (`--no-incremental`): 0 errors, 0 CS0618/CS0612, 0 NuGet warnings; 78 warnings, all pre-existing nullable (CS86xx). Tests 1/1 (only an empty `TestMethod1` exists — no behavioural coverage).
- Final start (https://localhost:5000, Development, nothing configured): `/` 200, `/api/subpage` 200, `/robots.txt` 200, no errors logged. CMS UI verified manually by the user (Phase 6).
- Scanner before → after: scheduled-plugin 1→0, property-definition-plugin 7→0, filtered-items 5→0, page-reference 2→0, site-definition-current 3→0, site-definition-api 1→0, page-route-helper 3→0, platform-navigation-html 2→0, init-locate-advanced 8→0, static-cache-manager 6→0, group-definition-const-with-space 3→0, site-definition-any 9→0, searchable-attribute 17→17 (kept by decision). OptimizelyIdentity runtime check: failing → passing.
- Hygiene: no production secrets committed; local SQL passwords inline in appsettings.Development.json / appsettings.unittests.json (recommend user-secrets); Find private key removed in Phase 5 (must be rotated); no upgrade-tool leftovers; CI paths wrong (pre-existing).
- Docs updated: CLAUDE.md (stack, commands, feeds, session), stack.md, architecture.md, todo.md, changelog.md, decisions.md (ADR-008), plans/cms13-database-upgrade.md.
- `--filter` verified with `dotnet test --project …`; a filter matching zero tests exits with an error (MTP).

### Session 3 Summary
- **Accomplished:** .NET 10 / CMS 13.6 / Commerce 15.5 upgrade, Phases 0–8.
- **Decisions:** ADR-008.
- **Next:** see todo.md "Up Next" (credentials, prod-copy DB upgrade, Find key rotation, Graph search service, CI paths).

---

## Quick Status
Last Session: 2026-11-11
Current Phase: Active Development (v2.2.0)
Overall Progress: Platform upgraded to .NET 10 / CMS 13.6 / Commerce 15.5 (Session 3); cookies consent hardened (P0–P4)
Active Blockers: None
