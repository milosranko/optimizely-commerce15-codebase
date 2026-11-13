# Architecture Decision Records

## ADR-001: Layered Architecture with Strict Dependency Flow
Date: [RETROACTIVE - Inferred from codebase]
Status: Accepted

### Context
Need a maintainable, testable architecture for an enterprise e-commerce platform built on Optimizely CMS/Commerce.

### Decision
Adopt a layered architecture with strict top-down dependency flow:
- Web → Business → ContentTypes → Core
- Business → Integrations (side branch, no Optimizely deps)

### Options Considered
1. **Layered architecture (chosen):** Clear boundaries, testable layers, strict dependency graph
2. **Monolithic single project:** Simpler setup but harder to maintain, test, and enforce boundaries
3. **Clean Architecture / Onion:** More abstractions layers — overkill for current project size

### Rationale
Layered architecture provides the right balance of separation and simplicity for an Optimizely-based solution. Isolating all Optimizely NuGet packages in Core prevents leaking platform dependencies.

### Consequences
- All Optimizely packages must be referenced only in Core
- Business logic cannot directly reference Optimizely APIs without going through ContentTypes/Core
- Adding a new Optimizely feature requires changes in Core first

---

## ADR-002: Isolate All Optimizely NuGet Packages in Core Project
Date: [RETROACTIVE - Inferred from codebase]
Status: Accepted

### Context
Optimizely CMS and Commerce have many NuGet package dependencies. Spreading them across projects creates tight coupling.

### Decision
All EPiServer/Optimizely NuGet packages are referenced exclusively in `Commerce.Demo.Optimizely.Core`. Other projects access Optimizely APIs transitively.

### Options Considered
1. **Centralized in Core (chosen):** Single source of truth for platform dependencies
2. **Per-project references:** Each project references what it needs — leads to version conflicts and scattered dependencies

### Rationale
Centralizing packages prevents version mismatches, simplifies upgrades, and makes the platform dependency boundary explicit.

### Consequences
- Core project is the "gravity well" — everything eventually depends on it
- Core may accumulate too many responsibilities over time (see ADR-005)

---

## ADR-003: Decorator Pattern for Cross-Cutting Concerns
Date: [RETROACTIVE - Inferred from codebase]
Status: Accepted

### Context
Need to add caching to services without polluting business logic.

### Decision
Use the decorator pattern via Scrutor's `Decorate<TInterface, TDecorator>()`. Example: `SiteSettingsManagerCacheDecorator` wraps `ISiteSettingsManager`.

### Options Considered
1. **Decorator pattern (chosen):** Clean separation, testable, follows OCP
2. **Inline caching in services:** Simpler but mixes concerns
3. **AOP / interceptors:** More powerful but adds complexity and magic

### Rationale
Decorator pattern with Scrutor is idiomatic for .NET DI, keeps services focused on business logic, and is easy to test in isolation.

### Consequences
- Each cached service needs a decorator class
- Registration order matters in Startup.cs (Decorate must follow initial registration)

---

## ADR-004: Mapster for Object Mapping
Date: [RETROACTIVE - Inferred from codebase]
Status: Accepted

### Context
Need to map between content types, view models, and API response DTOs.

### Decision
Use Mapster (configured in `MapsterConfig.cs`) instead of AutoMapper.

### Options Considered
1. **Mapster (chosen):** Faster performance, less configuration, code generation support
2. **AutoMapper:** More mature but slower, heavier configuration
3. **Manual mapping:** No dependency but repetitive and error-prone

### Rationale
Mapster offers better performance than AutoMapper with less boilerplate. Centralized configuration in MapsterConfig.cs.

### Consequences
- All mapping config in one file
- Team must be familiar with Mapster conventions

---

## ADR-005: Integrations Project Without Optimizely Dependencies
Date: [RETROACTIVE - Inferred from codebase]
Status: Accepted

### Context
PayPal integration and future external service integrations should be portable and testable without the Optimizely platform.

### Decision
`Commerce.Demo.Integrations` has zero Optimizely NuGet references. It depends only on standard .NET libraries and the PayPalServerSDK.

### Options Considered
1. **Separate project, no Optimizely deps (chosen):** Portable, independently testable, clean boundary
2. **Integration code in Business:** Simpler but couples external integrations to platform
3. **Integration code in Core:** Wrong layer — Core is infrastructure, not business integrations

### Rationale
Keeping integrations free of Optimizely dependencies means the PayPal gateway code can be reused, unit tested without Optimizely, and replaced independently.

### Consequences
- Business project acts as the bridge between Integrations and Optimizely
- Integrations cannot directly access Optimizely content

---

## ADR-006: Minimal API for Custom Endpoints
Date: [RETROACTIVE - Inferred from codebase]
Status: Accepted

### Context
Need custom API endpoints alongside Optimizely's built-in content routing.

### Decision
Use ASP.NET Core Minimal API pattern with `MapGroup("/api")` in `Endpoints.cs`.

### Options Considered
1. **Minimal API (chosen):** Lightweight, modern, co-exists with MapContent()
2. **API Controllers:** More ceremony, separate controller classes per resource
3. **Optimizely Content Delivery API:** Platform-native but less flexible for custom logic

### Rationale
Minimal API keeps custom endpoints simple and colocated. Optimizely's `MapContent()` handles CMS/Commerce routing. Commented-out Content Delivery API sections suggest this was evaluated and deferred.

### Consequences
- All custom endpoints in a single file (may need splitting as API grows)
- No built-in model validation or API versioning without additional setup

---

## ADR-007: Cookie Consent Capture Hardening
Date: 2026-05-30
Status: Accepted

### Context
The cookie-consent audit endpoint (`POST /api/consent/audit`) is anonymous and writes to an EPiServer DynamicDataStore. As built it accepted arbitrary unbounded input, was unthrottled, stored the raw client IP for the full 3-year retention, and the audit CSV export was vulnerable to spreadsheet formula injection. The admin statistics also materialized the entire store on every dashboard load.

### Decision
Harden the capture path in the Web layer without changing the storage model (yet):
- Validate `action` against the `ConsentAction` enum and `categoriesSelected` against `SnippetUsage`; cap all field lengths.
- Apply a per-IP fixed-window rate limit (`consent-audit`, 10/min) via ASP.NET Core's built-in rate limiter.
- Anonymize the client IP before storage (drop last IPv4 octet / last 80 bits of IPv6).
- Neutralize CSV formula injection in the export; cap export row count.
- Cache dashboard statistics (`ICacheService`) and count via store-side predicates instead of in-memory materialization.

### Options Considered
1. **In-place Web-layer hardening (chosen):** Low risk, no schema/data change, ships immediately.
2. **CORS / origin allowlist:** Rejected — browser-only, bypassed by non-browser clients (the real abuse vector); default is already deny-cross-origin.
3. **Migrate audit store to dedicated SQL now:** Higher value long-term but ADR-worthy on its own and requires a migration — deferred.

### Rationale
The actual threats (flooding, injection, PII retention) are addressed at the boundary with built-in framework features and no data migration. CORS would add config without closing a real gap. The SQL-store migration is the proper scalability fix but is sequenced separately.

### Consequences
- DynamicDataStore remains the store; high-volume aggregation is mitigated (caching + predicate counts) but not eliminated — SQL migration still recommended (see todo.md).
- `ForwardedHeaders` is not yet configured, so the source IP is still derived from a spoofable header before anonymization (infra-dependent follow-up).
- Statistics can be stale up to `StatisticsCacheSeconds` (cache invalidated on purge).

---

## ADR-008: Platform Upgrade to .NET 10 / CMS 13 / Commerce 15 and Add-on Replacements
Date: 2026-11-11
Status: Accepted

### Context
Optimizely CMS 12 / Commerce 14 on .NET 8 is being upgraded to CMS 13 (13.6.0) / Commerce 15 (15.5.0) on .NET 10. Several add-ons have no CMS 13 build: EPiServer.Find.Cms / Find.Commerce / Commerce.FindSearchProvider, DoubleJay.Epi.ConfigurableColorPicker, DoubleJay.Epi.EnhancedPropertyList, Geta.Optimizely.DdsAdmin and EPiServer.Labs.ContentManager. Keeping a CMS 12 build would restore and then fail at runtime (`MissingMethodException`).

### Decision
- **Search & Navigation (Find):** remove all three packages, `FindCommerceOptions` and the unused `SearchService`/`ISearchService` (registered in DI, called nowhere). Optimizely Content Graph becomes the search/index path; Commerce uses its default search provider.
- **ConfigurableColorPicker:** `EventTuitionVariant.ColorPicker` becomes a `string` holding a `#RRGGBB` hex value with regex validation and the native HTML colour input as editor.
- **EnhancedPropertyList, DdsAdmin:** remove. Property lists use the standard CMS collection editor; the DDS admin screen is dropped.
- **Labs.ContentManager:** replaced by the supported `EPiServer.Cms.UI.ContentManager`.
- **JSON:** MVC/API serialisation moves to System.Text.Json (no Newtonsoft usage in code).
- **Dictionary properties:** `PropertyDictionaryMultiple` (obsolete) → `PropertyStringDictionaryMultiple` on FashionProduct.AvailableSizes/AvailableColors and EventQuestionProperty.PurchaseClaims, so Content Graph indexes them as string lists. No data migration (storage changes from LongString to Json).
- **Site definitions:** `ISiteSettingsManager.SpecificSiteSettings` now takes `IRoutableApplication` (CMS 13 Applications model).
- **Packages added because CMS 13 split them out:** `EPiServer.CMS.UI.AspNetIdentity`, `EPiServer.Cms.UI.VisitorGroups`, and `EPiServer.OptimizelyIdentity` (Commerce 15 loads it at startup even with Opti ID off).
- Optional CMS 13 features enabled: Opti ID, Content Graph, Content Manager/Opal.

### Options Considered
1. **Replace/remove add-ons without CMS 13 builds (chosen).**
2. **Port SearchService to Content Graph:** deferred; the service has no callers, and Find autocomplete statistics / best bets have no 1:1 Graph equivalent.
3. **Vendor add-on source (DoubleJay):** rejected; more code to own for one property.
4. **Wait for vendor CMS 13 builds:** rejected; blocks the upgrade indefinitely.

### Rationale
Each removed add-on has either no caller, a single trivial usage, or a supported first-party replacement, so the upgrade carries no runtime risk from CMS 12 binaries.

### Consequences
- Existing environments: stored values of AvailableSizes, AvailableColors and PurchaseClaims are not carried over and must be re-entered; stored `IColor` values on EventTuitionVariant need migrating to hex strings (new `DisplayColor` property); the Find index stops being updated; editors lose the DDS admin screen and the enhanced property-list editor.
- Geta.NotFoundHandler 8 upgrades its own schema (v2 → v3) at startup; the DB user needs ALTER on `NotFoundHandler.*`. 8.x also changes redirect behaviour (exact match, 410 for deleted URLs, no self-appending redirects).
- Tab names with spaces are no longer allowed; tab access rights set in Admin mode must be re-applied after deployment.
- DXP/hosting must run the .NET 10 runtime (≥ 10.0.13).
- **Forms 6.0.2 workaround:** `FormsAclJobRegistrationInitialization` registers Forms' `UpdateUploadFolderACLJob` row before Forms initializes (Forms NREs on databases where the row is missing — every fresh CMS 13 DB). Remove when Forms fixes it.
- `CookieConsentAuditPurgeJob` GUID changed (placeholder collided with Commerce 15's webhooks job and blocked all scheduled-job sync); its execution history does not carry over in existing environments.
- `EventTuitionVariant` hex colour property is `DisplayColor` (Commerce catalog properties are shared metafields by name; `FashionVariant.Color` already exists).

---

## Quick Reference

| ADR | Title | Status | Date |
|-----|-------|--------|------|
| 001 | Layered Architecture with Strict Dependency Flow | Accepted | [Retroactive] |
| 002 | Isolate All Optimizely NuGet Packages in Core Project | Accepted | [Retroactive] |
| 003 | Decorator Pattern for Cross-Cutting Concerns | Accepted | [Retroactive] |
| 004 | Mapster for Object Mapping | Accepted | [Retroactive] |
| 005 | Integrations Project Without Optimizely Dependencies | Accepted | [Retroactive] |
| 006 | Minimal API for Custom Endpoints | Accepted | [Retroactive] |
| 007 | Cookie Consent Capture Hardening | Accepted | 2026-05-30 |
| 008 | Platform Upgrade to .NET 10 / CMS 13 / Commerce 15 | Accepted | 2026-11-11 |
