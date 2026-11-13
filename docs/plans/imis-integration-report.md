# iMIS ↔ Optimizely Commerce Integration — Manager Report

## Executive Summary

Build a **nightly two-way sync** between iMIS 20.x (on-premise) and Optimizely Commerce 14 to unify product catalog, member data, and order processing across both systems.

| Direction | What Syncs | Frequency |
|-----------|-----------|-----------|
| **iMIS → Optimizely** | Events, Products, Members/Contacts, Pricing | Nightly (2 AM) |
| **Optimizely → iMIS** | Orders, Event Registrations | Nightly (3 AM) |

---

## Scope & Components

| # | Component | Description | Estimate |
|---|-----------|-------------|----------|
| 1 | **New Content Types** | 3 new Optimizely types (GenericProduct, GenericVariant, GenericCategory) + constants | **3–4 hours** |
| 2 | **iMIS API Client** | HTTP client, OAuth 2.0 token handler, DTOs (12+ files) | **6–8 hours** |
| 3 | **Sync State Infrastructure** | Tracking per-entity sync state, checkpoint/resume, error tracking (DDS-backed) | **3–4 hours** |
| 4 | **Catalog & Customer Import** | Mapping engine (40+ format values → 5 variant types), upsert logic, claim-based pricing | **10–14 hours** |
| 5 | **Order/Registration Export** | Map Optimizely orders → iMIS DTOs, duplicate detection, POST to iMIS | **6–8 hours** |
| 6 | **Scheduled Jobs** | Two independent jobs (import + export) with stop/resume support | **2–3 hours** |
| 7 | **Configuration & DI Wiring** | appsettings, Startup.cs registration, environment variable support | **1–2 hours** |

---

## Effort Summary

| Category | Estimate |
|----------|----------|
| **Development** | **31–43 hours** (~4–6 dev days) |
| **Unit Tests** (mock HTTP + mock content repo) | **8–12 hours** (~1–2 dev days) |
| **Integration Testing** (against real iMIS) | **6–10 hours** (~1–1.5 dev days) |
| **Code Review & QA** | **4–6 hours** |
| **Documentation & Handoff** | **2–3 hours** |
| | |
| **Total Estimated Effort** | **51–74 hours (~7–10 dev days)** |

---

## Key Technical Decisions

1. **New `Commerce.Demo.Integrations` project** for iMIS client — keeps Optimizely dependencies isolated (follows existing PayPal pattern)
2. **Format-based variant routing** — 40+ iMIS format values mapped to 5 Optimizely variant types via a static mapping table
3. **Claim-based pricing** — stored as raw strings on variants, resolved at runtime against member claims
4. **Optimizely DDS** for sync state — no additional database infrastructure required
5. **Two separate scheduled jobs** — import and export run independently for easier monitoring and failure isolation

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| iMIS API availability/rate limits | Medium | Polly retry policies via `AddStandardResilienceHandler()` |
| Data mapping mismatches (40+ formats) | High | Static mapping table with fallback to GenericVariant; log unknowns |
| Large catalog volume on first sync | Medium | Checkpoint recovery — interrupted runs resume from last success |
| Credential management | Low | Secrets via environment variables, never in config files |
| Schema drift in iMIS responses | Medium | DTOs match actual JSON; log deserialization warnings |

## Dependencies & Prerequisites

- iMIS API credentials (ClientId / ClientSecret) — **needed before integration testing**
- iMIS API endpoint URLs and documentation for order creation endpoint
- Access to iMIS test environment
- Existing Optimizely Commerce content types already in place (EventProduct, BookProduct, etc.)

## Deliverables

- ~30 new files across 4 projects
- 2 scheduled jobs (configurable via CMS Admin)
- Full unit test coverage for mapping logic and sync services
- Integration test suite for iMIS API client
- Configuration documentation for deployment

---

*Estimates assume a single senior .NET developer familiar with Optimizely Commerce. Add ~20–30% buffer for unknowns in the iMIS API behavior during integration testing.*
