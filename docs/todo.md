# Task Tracker

> Last Updated: 2026-11-11

## Current Phase
Phase: Active Development
Goal: Establish F3 documentation, address architectural concerns, continue feature development
Target: [TODO: Define target date]

## In Progress
- None currently

## Up Next
- [ ] **Provision Optimizely Graph and Opti ID credentials** `priority: high`
  - Graph: `Optimizely:ContentGraph:AppKey`/`Secret`/`SingleKey`; Opti ID: `EPiServer:OptimizelyIdentity:*`; Opal: `EPiServer:OpalChat:InstanceId`
  - user-secrets locally, DXP App Settings per environment; then run the Graph sync job and verify Opti ID role mapping (`MappedRoles`)
  - Invalid Graph keys stop the site from starting once Graph is enabled
- [ ] **Run the CMS 13 schema upgrade on a production DB copy** `priority: high`
  - Follow /docs/plans/cms13-database-upgrade.md (discovery query, ISchemaValidator cleanup, verification)
- [ ] **Rotate the EPiServer Find private key** `priority: high`
  - It was committed in appsettings.Development.json (removed 2026-11-11)
- [ ] **Build a Graph-based search service** `priority: medium`
  - Replaces the removed Find SearchService; reuse Features/Search models
- [ ] **Fix CI pipeline project paths** `priority: medium`
  - `pipelines/build-deploy-github.yml` points at `src/Optimizely.Web` / `src/Frontend.React` (do not exist; pre-existing)
- [ ] **Post-deploy admin tasks per environment** `priority: medium`
  - Re-apply tab access rights for "Aside content", "Site Settings", "Cookies Consent"; re-enter AvailableSizes/AvailableColors/PurchaseClaims; DB user needs ALTER on NotFoundHandler.* tables

- [ ] **Expand test coverage beyond Business project** `priority: medium`
  - Context: Tests project only references Business — no coverage for Web, Core, or Integrations
  - Add project references or create per-layer test projects

- [ ] **Implement actual API endpoint logic** `priority: medium`
  - Context: Current endpoints are stubs/demos (GET returns first page regardless of ID, POST echoes request)
  - See /docs/api.md for current endpoint documentation

- [ ] **Enable API authentication** `priority: medium`
  - Context: `.RequireAuthorization()` is commented out on endpoints
  - Define auth strategy and enable

## Backlog
- [ ] **Migrate cookie consent audit storage to a dedicated SQL table** `priority: medium`
  - Context: audit entries live in EPiServer DynamicDataStore; not built for high-volume querying/aggregation
  - Enables single-`DELETE` purge and indexed statistics; ADR-worthy + needs migration
  - See ADR-007 consequences and /docs/devlog.md Session 2

- [ ] **Verify DynamicDataStore predicate-count translation (consent stats)** `priority: medium`
  - Context: `GetStatistics` now counts via store queries; confirm they translate to SQL (not in-memory) at runtime

- [ ] **Configure ForwardedHeaders with known proxies** `priority: low`
  - Context: consent endpoint anonymizes IP but still reads spoofable `X-Forwarded-For`; needs DXP/Cloudflare topology

- [ ] **Consent capture: sendBeacon + drop jQuery dependency** `priority: low`
  - Context: consent POST is fire-and-forget before `location.reload()` (race); banner depends on global `$`
  - `navigator.sendBeacon` + vanilla-JS/React rewrite of `_CookiesConsentDialog.cshtml`

- [ ] **Consider splitting Optimizely.Core project** `priority: low`
  - Context: Core contains infrastructure AND feature code (search, shipping, gadgets, jobs)
  - Could split into Core (infrastructure) + features moved to Business
  - See ADR-002 consequences

- [ ] **Evaluate enabling Optimizely Content Delivery APIs** `priority: low`
  - Context: Content Delivery, Forms, Commerce, Definitions, and Management APIs are configured but commented out in Startup.cs
  - Decide which to enable for headless/hybrid scenarios

- [ ] **Wire PayPal integration into checkout flow** `priority: low`
  - Context: PayPal gateway code exists in Integrations but isn't connected to commerce checkout
  - Implement checkout page integration

- [ ] **Expand React frontend** `priority: low`
  - Context: Frontend is minimal (App.jsx, main.jsx only)
  - Define component architecture and implement key pages

- [ ] **Make API culture configurable** `priority: low`
  - Context: GET /api/subpage is hardcoded to Serbian culture (`sr`)
  - Accept culture as query parameter or from Accept-Language header

## Blocked
- None currently

## Completed (Recent)
- [x] Upgrade to .NET 10 / Optimizely CMS 13.6 / Commerce 15.5 (Phases 0–8, ADR-008; see devlog Session 3) - 2026-11-11
- [x] Cookies consent P0 — last-category enforcement bug + Secure/SameSite cookie - 2026-05-30
- [x] Cookies consent P1 — GDPR correctness (symmetric expiry, configured expiry, policy-version re-prompt, withdraw trigger, essential integrity) - 2026-05-30
- [x] Cookies consent P2 — security hardening (validation, rate limiting, IP anonymization, CSV injection) - 2026-05-30
- [x] Cookies consent P3 — performance (cached predicate statistics, bounded export) - 2026-05-30
- [x] Cookies consent P4 — accessibility (dialog semantics, focus, aria-expanded, describedby) - 2026-05-30
- [x] SPEC.md created (retroactive, 12 feature areas) - 2026-02-09
- [x] PLAN.md created (retroactive, 8 phases + forward plan) - 2026-02-09
- [x] F3 documentation system setup - 2026-02-09
- [x] Architecture analysis and documentation - 2026-02-09
- [x] Retroactive ADRs documented (6 decisions) - 2026-02-09
