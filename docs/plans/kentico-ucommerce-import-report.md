# Kentico uCommerce → Optimizely Commerce Product Enrichment — Manager Report

## Executive Summary

Enrich Optimizely Commerce product catalog with **rich content data** from Kentico uCommerce. Products are initially created as skeletons by the iMIS import (SKUs, prices, claims). This import adds the missing layer: descriptions, images, SEO metadata, event schedules, addon relationships, and registration questions.

| Aspect | Detail |
|--------|--------|
| **Source** | JSON export files from Kentico uCommerce (`products.json`, `categories.json`) + media files on disk |
| **Target** | Existing Optimizely Commerce products/categories (created by iMIS sync) |
| **Correlation Key** | SKU / product code (shared across iMIS, uCommerce, and Optimizely) |
| **Frequency** | Recurring (nightly at 4 AM, after iMIS import completes at 2–3 AM) |
| **Prerequisite** | Products must already exist in Optimizely via iMIS import |

---

## What This Import Adds

| Data Type | Examples |
|-----------|---------|
| **Rich Descriptions** | HTML product descriptions, long descriptions, marketing teasers, sizing guides |
| **Product Images & Files** | Primary images, galleries, PDF brochures |
| **SEO Metadata** | Meta title, description, keywords, robots directives |
| **OpenGraph / Social** | Social share titles, descriptions, preview images |
| **Event Schedules** | Session times, headings, descriptions, speaker names |
| **Registration Questions** | Dietary requirements, accessibility needs (with type & required flag) |
| **Addon Relationships** | Related addons, conflicting addons, dependent addons (resolved by SKU) |
| **Category Content** | Headings, intro text, SEO for category pages |

---

## Scope & Components

| # | Component | Description | Files | Estimate |
|---|-----------|-------------|-------|----------|
| 1 | **Import File DTOs & Reader** | JSON deserialization models (8 DTOs) + file reader service | 12 | **4–6 hours** |
| 2 | **Media Import Service** | Upload images/files to Optimizely media library, folder structure, deduplication | 2 | **4–6 hours** |
| 3 | **Product & Category Enrichment** | Map DTOs → Optimizely content properties (descriptions, SEO, schedules, addon relations) | 5 | **8–12 hours** |
| 4 | **Scheduled Job** | Nightly job at 4 AM, categories first → products second, stop/resume support | 1 | **2–3 hours** |
| 5 | **Configuration & DI Wiring** | appsettings, Startup.cs registration | 2 | **1–2 hours** |

---

## Effort Summary

| Category | Estimate |
|----------|----------|
| **Development** (~22 files) | **19–29 hours** (~3–4 dev days) |
| **Unit Tests** (mock file reader, content repo, blob factory) | **6–8 hours** (~1 dev day) |
| **Integration Testing** (sample JSON + test images → verify in CMS) | **4–6 hours** |
| **Code Review & QA** | **3–4 hours** |
| | |
| **Total Estimated Effort** | **32–47 hours (~4–6 dev days)** |

---

## Nightly Import Sequence (Full Picture)

```
2:00 AM  →  iMIS Import     (creates product skeletons: families, SKUs, prices, members)
3:00 AM  →  iMIS Export     (pushes orders/registrations back to iMIS)
4:00 AM  →  uCommerce Import (enriches products with descriptions, images, SEO, schedules)
```

Products must exist before uCommerce enrichment runs — the 4 AM timing ensures this.

---

## Key Technical Decisions

1. **Null-skip pattern** — only non-null fields from JSON overwrite Optimizely content; prevents accidental blanking of manually edited CMS content
2. **Media deduplication** — checks if image with same filename exists before uploading; updates metadata only if binary already present
3. **SKU as universal correlation key** — same SKU joins data across iMIS, uCommerce, and Optimizely (no additional ID mapping needed)
4. **Per-product isolation** — failure on one product doesn't stop the batch; each enrichment is independent
5. **Addon resolution by SKU** — addon/conflict/dependent relationships expressed as SKU strings in JSON, resolved to Optimizely `ContentReference` at import time

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Products not yet created by iMIS when enrichment runs | High | Scheduled 2 hours after iMIS import; missing SKUs logged and skipped |
| Malformed HTML in descriptions | Medium | Trusted internal data; malformed HTML logged as warning, not blocked |
| Missing image files on disk | Medium | Per-image isolation — missing files logged, other fields still imported |
| Unresolved addon SKUs | Low | Logged and skipped; re-resolved on next import run |
| Large media volume slowing import | Medium | Deduplication avoids redundant uploads; batch processing |
| JSON schema changes from uCommerce export tool | Medium | DTOs validated on read; malformed files abort with clear error |

## Dependencies & Prerequisites

- **iMIS import must run first** — products must exist in Optimizely before enrichment
- **uCommerce export tool** (external, outside this project) must generate `products.json` and `categories.json` matching the agreed schema
- **Media files** must be placed at the configured `MediaSourcePath` before import runs
- **Existing content types** — EventProduct, EventAddonVariant, BookProduct, etc. must support the target properties (EventSchedule, Questions, addon lists)

## Deliverables

- 12 DTO / reader files in Integrations project
- 8 service / mapper files in Business project
- 1 scheduled job (configurable via CMS Admin)
- Unit test coverage for enrichment mapping, media import, and file reader
- Configuration documentation for import paths

---

*Estimates assume a single senior .NET developer familiar with Optimizely Commerce content APIs. The uCommerce export tool (generating the JSON files) is outside this project's scope and must be delivered separately.*
