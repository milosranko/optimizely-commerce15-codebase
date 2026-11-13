# iMIS Order Export API — Manager Report

## Executive Summary

Build a **REST API endpoint** that exposes Optimizely Commerce purchase orders to iMIS in XML format. iMIS calls this endpoint every 5 minutes to pull new/updated orders, enabling near-real-time order synchronization without Optimizely needing to push data.

| Aspect | Detail |
|--------|--------|
| **Endpoint** | `GET /api/orders/export?from={utcDateTime}&to={utcDateTime}` |
| **Response Format** | XML (`application/xml`) |
| **Authentication** | API key via `X-Api-Key` header |
| **Polling Frequency** | Every 5 minutes (driven by iMIS) |
| **Direction** | Optimizely → iMIS (pull-based) |

---

## Scope & Components

| # | Component | Description | Files | Estimate |
|---|-----------|-------------|-------|----------|
| A1 | **API Key Auth Middleware** | Endpoint filter with constant-time key comparison, config via appsettings | 2 | **1–2 hours** |
| A2 | **XML Models** | 8 model classes matching iMIS expected schema (Person, Order, LineItems, Tax, Shipping) | 8 | **2–3 hours** |
| A3 | **XML Serialization Helper** | Clean XML output without namespaces, cached serializer | 1 | **0.5–1 hour** |
| B1 | **Order Export Service** | Business layer — queries orders by date range via `IOrderSearchService` | 2 | **2–3 hours** |
| B2 | **Order-to-XML Mapper** | Maps `IPurchaseOrder` → XML models (addresses, payments, line items, tax) | 1 | **3–4 hours** |
| B3 | **Endpoint + DI Wiring** | Minimal API endpoint registration, config binding, service registration | 2 | **1–2 hours** |

---

## Effort Summary

| Category | Estimate |
|----------|----------|
| **Development** (16 files, ~450 lines) | **16–24 hours** (~2–3 dev days) |
| **Unit Tests** (auth filter, mapper, service, serializer) | **4–8 hours** (~0.5–1 dev day) |
| **Integration Testing** (manual, against running instance) | **2–3 hours** |
| **Code Review & QA** | **2–3 hours** |
| | |
| **Total Estimated Effort** | **24–40 hours (~3–5 dev days)** |

---

## What Gets Exported

Each order sent to iMIS includes:

- **Person** — billing/shipping addresses, name, email, phone, iMIS Member ID
- **Order** — payment total, auth code, transaction reference, status ("NewOrder"), date, discount code
- **Tax Info** — state tax, county tax, stadium tax, total
- **Shipping Info** — method, handling charges, instructions
- **Line Items** — product code, product type (Event/Product), pricing role, price, quantity, attendee Member ID, affiliate ID

---

## Custom Metafields Required

New commerce metafields needed to store iMIS-specific data on orders:

| On Entity | Field | Purpose |
|-----------|-------|---------|
| `ILineItem` | `ImisProductCode` | External product code from iMIS |
| `ILineItem` | `ImisPricingRole` | Pricing role (NM, ALLMEM, etc.) |
| `ILineItem` | `ImisFormatDescription` | Format description |
| `ILineItem` | `ImisMemberId` | Attendee member ID (can differ from order customer) |
| `ILineItem` | `ImisAffiliateId` | Affiliate ID |
| `IOrderAddress` | `County` | County name |
| `CustomerContact` | `ImisMemberId` | iMIS member ID |
| `IPurchaseOrder` | `ImisDiscountCode` | Discount code |

**Note:** No changes to existing content types (products, variants, categories). All iMIS-specific data lives on order/commerce metafields only.

---

## Key Technical Decisions

1. **Pull-based architecture** — iMIS calls Optimizely every 5 minutes; no push/webhook infrastructure needed
2. **API key authentication** — simple, sufficient for server-to-server calls; constant-time comparison prevents timing attacks
3. **XML response format** — matches iMIS's expected schema; no additional transformation on their side
4. **Minimal API endpoint** — follows existing `Endpoints.cs` pattern in the project
5. **No result caching** — each call fetches fresh data for the narrow time window; keeps implementation simple

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Metafield data missing on orders | High | Validate during checkout flow; log warnings for missing fields |
| Large order volumes in 5-min window | Low | Date-filtered queries are narrow; XML serializer instances cached |
| API key exposure | Medium | Stored in environment variables in production, never in source control |
| XML schema mismatch with iMIS expectations | High | XML models built directly from iMIS-provided sample; validate during integration testing |
| Endpoint availability during deployments | Low | Standard deployment windows; iMIS retries on next 5-min cycle |

## Dependencies & Prerequisites

- iMIS team to provide and configure their API key
- Agreement on XML schema (sample provided and approved)
- Custom metafields must be populated during the checkout/order creation flow
- `ImisMemberId` on `CustomerContact` must be set during member import (Component 4 of main integration plan)

## Security

- API key validated via constant-time comparison (prevents timing attacks)
- Input validation on `from`/`to` parameters (reject non-UTC, reject `from` > `to`)
- No PII in logs (order numbers and counts only)
- Rate limiting deferred to v2 (not in scope)

---

*Estimates assume a single senior .NET developer familiar with Optimizely Commerce order APIs. This is a focused, well-scoped component with low uncertainty — estimates should be reliable.*
