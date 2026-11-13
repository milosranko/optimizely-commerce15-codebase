# API Documentation

> Last Updated: 2026-02-09 [RETROACTIVE]
> Base URL: `/api`

## Overview

Custom API endpoints are implemented using ASP.NET Core Minimal API in `Commerce.Demo.Web\Api\Endpoints.cs`, mounted at `/api` via `endpoints.AddApiEndpoints()` in Startup.cs.

Optimizely's built-in content routing (`MapContent()`) handles CMS and Commerce URLs separately.

## Authentication

Currently: **No authentication** on custom API endpoints (`.RequireAuthorization()` is commented out).

[TODO: Define authentication strategy for API endpoints]

## Response Format

### List Response (ResponseList<T>)
```json
{
  "results": [
    { "name": "Page Name 1" },
    { "name": "Page Name 2" }
  ]
}
```

### Single Response (ResponseSingle<T>)
```json
{
  "result": {
    "name": "Page Name"
  }
}
```

## Custom Endpoints

### GET /api/subpage
**Purpose:** Get all child pages of the start page

**Query Parameters:** None

**Response:** `ResponseList<SubPage>`

| Field | Type | Description |
|-------|------|-------------|
| results | SubPage[] | Array of sub-page objects |
| results[].name | string | Page name |

**Notes:** Currently hardcoded to Serbian culture (`sr`). [TODO: Make culture configurable]

---

### GET /api/subpage/{id}
**Purpose:** Get a single child page of the start page

**Path Parameters:**

| Param | Type | Description |
|-------|------|-------------|
| id | int | Page identifier |

**Response:** `ResponseSingle<SubPage>`

**Error Responses:**
- `404 Not Found` — Page not found

**Notes:** Currently returns the first child page regardless of `id` parameter. [TODO: Implement actual ID-based lookup]

---

### POST /api/post
**Purpose:** Create a new resource (demo endpoint)

**Request Body:** `PostRequest`

| Field | Type | Description |
|-------|------|-------------|
| id | int/string | Resource identifier |

**Response:** `201 Created` with the request echoed back

**Notes:** Demo/stub endpoint — echoes back the request. [TODO: Implement actual resource creation]

---

### POST /api/consent/audit
**Purpose:** Record a visitor's cookie-consent decision to the audit trail (anonymous, fire-and-forget from the consent banner)

**Request Body:** `RecordConsentRequest`

| Field | Type | Description |
|-------|------|-------------|
| action | string | Required. Must parse to `ConsentAction` (`Accepted`/`Denied`/`Modified`) |
| categoriesSelected | string | Required. `false` (deny) or a pipe-delimited list of `SnippetUsage` categories |
| cookieName | string? | Optional. Consent cookie name (length-capped) |
| policyVersion | string? | Optional. Consent policy version (length-capped) |

**Responses:**
- `200 OK` — recorded (also returned on internal audit failure; logged, never thrown to the client)
- `400 Bad Request` — invalid action, unknown category, or oversized field
- `429 Too Many Requests` — per-IP rate limit exceeded (policy `consent-audit`: 10/min)

**Server-side handling:** client IP is anonymized before storage (last IPv4 octet / last 80 bits of IPv6 dropped); `User-Agent` and visitor id (authenticated name or anonymous-id cookie) are captured. Entries persist to the `CookieConsentAuditStore` DynamicDataStore.

**Notes:** No CORS is applied by design — the endpoint is same-origin, returns no sensitive data, and CORS does not block non-browser clients (handled by validation + rate limiting). See ADR-007.

## Optimizely Built-in APIs

### Content Delivery API (Commented Out)
The following Optimizely APIs are configured but currently disabled in Startup.cs:
- Content Delivery API (`AddContentDeliveryApi`)
- Content Delivery Forms API (`AddFormsApi`)
- Content Delivery Commerce API (`AddCommerceApi`)
- Content Definitions API (`AddContentDefinitionsApi`)
- Content Management API (`AddContentManagementApi`)

[TODO: Decide which Optimizely APIs to enable]

## Request/Response Models

### Location
- Requests: `Commerce.Demo.Web\Api\Requests\`
- Responses: `Commerce.Demo.Web\Api\Responses\`
- Base classes: `Commerce.Demo.Web\Api\Responses\Base\`

### Service Layer
- Interface: `IApiService` in `Commerce.Demo.Web\Api\Services\`
- Implementation: `ApiService` in `Commerce.Demo.Web\Api\Services\`
