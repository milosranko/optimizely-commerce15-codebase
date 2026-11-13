# IMIS Order Export API — Implementation Plan

> **Status:** Approved
> **Created:** 2026-03-10
> **Version:** 1.0

## Overview

API endpoint that exports Optimizely Commerce purchase orders to the IMIS system in XML format. Called every 5 minutes by IMIS. Secured via API key in request header.

**Endpoint:** `GET /api/orders/export?from={utcDateTime}&to={utcDateTime}`
**Response:** `application/xml`
**Auth:** `X-Api-Key` header

## Architecture

```
IMIS (caller, every 5 min)
  → GET /api/orders/export?from=...&to=...
  → X-Api-Key header
  → API Key Middleware (validates key)
  → Endpoint handler (validates params)
  → IOrderExportService (queries orders via IOrderSearchService)
  → OrderToImisXmlMapper (IPurchaseOrder → CommerceOrderXml)
  → XmlResponseHelper (serializes to XML string)
  → Response: application/xml
```

Fits into the existing layered architecture:
- **Commerce.Demo.Web** — endpoint, API key middleware, XML models, mapper
- **Commerce.Demo.Business** — `IOrderExportService` / `OrderExportService`

## XML Output Format

### Multiple Orders (wrapped)

```xml
<CommerceOrders>
  <CommerceOrder>
    ...
  </CommerceOrder>
  <CommerceOrder>
    ...
  </CommerceOrder>
</CommerceOrders>
```

### Single CommerceOrder Structure

```xml
<CommerceOrder>
  <Person
    BillingAddress1="123 Main Street USA"
    BillingAddress2="202"
    BillingAddress3=""
    BillingCity="Madison"
    BillingProvince="WI"
    BillingZip="53703"
    BillingCounty="DANE"
    BillingCountry="US"
    BillingPhone="941-364-8161"
    ShippingAddress1="123 Main Street USA"
    ShippingAddress2="202"
    ShippingAddress3=""
    ShippingCity="Madison"
    ShippingProvince="WI"
    ShippingZip="53703"
    ShippingCounty="DANE"
    ShippingCountry="US"
    ShippingPhone="941-364-8161"
    PreferredAddress="Shipping"
    EmailAddress="method2@wisbar.org"
    JobTitle=""
    Prefix=""
    FirstName="Scott"
    LastName="Auer"
    Suffix=""
    MemberId="1117621" />

  <Order
    PaymentTotalDollarAmount="3,024.15"
    PaymentAuthCode=""
    PaymentTransactionReference=""
    PaymentType=""
    Last4CC=""
    OrderStatus="NewOrder"
    OrderDate="2026-01-29T18:52:43.000+01:00"
    OrderNumber=""
    DiscountCode="WEB">

    <TaxInfo
      StateTax="41.95"
      CountyTax="4.20"
      StadiumTax="0.00"
      TaxTotal="46.15" />

    <ShippingInfo
      ShippingInstructions=""
      ShippingMethod="UPS"
      ShippingHandling="11.000"
      Discount=""
      DiscountCode="" />

    <OrderLineItems>
      <OrderLineItem
        ProductType="Product"
        ProductCode="AK0187"
        FormatDescription=""
        PricingRole="NM"
        Discount="0"
        Price="689.0000"
        Quantity="1"
        DiscountCode=""
        MemberId="1117621"
        AffiliateId="" />

      <OrderLineItem
        ProductType="Event"
        ProductCode="SA9255/TUITION1"
        FormatDescription=""
        PricingRole="NM"
        Discount="0"
        Price="500"
        Quantity="1"
        DiscountCode=""
        MemberId="1118685"
        AffiliateId="" />
    </OrderLineItems>

  </Order>
</CommerceOrder>
```

## Data Mapping

### Final Mapping Decisions

| IMIS Field | Source | Notes |
|-----------|--------|-------|
| `MemberId` (Person) | Custom property on Optimizely `CustomerContact` | Property: `ImisMemberId` |
| `MemberId` (LineItem) | Custom property on `ILineItem` | Can differ from Person for event attendee registrations |
| `PricingRole` | Custom property on `ILineItem` | Values: "NM", "ALLMEM", etc. |
| `ProductCode` | Custom property on `ILineItem` | External code from IMIS, not Optimizely SKU |
| `FormatDescription` | Custom property on `ILineItem` | e.g. "One-year subscription" |
| `County` | Custom property on `IOrderAddress` | New field for both billing and shipping |
| `ProductType` | Derived from variant's parent content type | `EventProduct` → "Event", else → "Product" |
| `AffiliateId` | Custom property on `ILineItem` | |
| `OrderStatus` | Always "NewOrder" for fresh exports | |
| `DiscountCode` | Custom property on `IPurchaseOrder` (order level) | |
| `PreferredAddress` | Custom property on `IPurchaseOrder` or default "Shipping" | |

### No Changes to Existing Content Types

All IMIS-specific data lives on order/commerce metafields. No modifications to `VariantBase`, `ProductBase`, or any content type classes.

### Custom Metafields Required

**On `ILineItem`:**
- `ImisProductCode` — external product code from IMIS
- `ImisPricingRole` — pricing role (NM, ALLMEM, etc.)
- `ImisFormatDescription` — format description
- `ImisMemberId` — attendee member ID (can differ from order customer for events)
- `ImisAffiliateId` — affiliate ID

**On `IOrderAddress`:**
- `County` — county name

**On `CustomerContact`:**
- `ImisMemberId` — IMIS member ID

**On `IPurchaseOrder` (order level):**
- `ImisDiscountCode` — discount code

## Component Breakdown

### Component A: Infrastructure (auth middleware, XML models, serializer)

#### A1: API Key Authentication Middleware
**Files:**
- `Commerce.Demo.Web/Api/Options/ImisApiOptions.cs`
- `Commerce.Demo.Web/Api/Middleware/ApiKeyEndpointFilter.cs`

```csharp
// ImisApiOptions.cs
public class ImisApiOptions
{
    public const string SectionName = "ApiSettings:Imis";
    public string ApiKey { get; set; } = string.Empty;
}
```

- Reads `X-Api-Key` header from request
- Compares against configured key from `appsettings.json` → `ApiSettings:Imis:ApiKey`
- Returns `401 Unauthorized` if missing/invalid
- Uses constant-time comparison to prevent timing attacks
- Implemented as `IEndpointFilter` for minimal API

**appsettings.json addition:**
```json
"ApiSettings": {
  "Imis": {
    "ApiKey": ""
  }
}
```

~60 lines, 2 files

#### A2: XML Models
**Files (all in `Commerce.Demo.Web/Api/Responses/Orders/`):**
- `CommerceOrdersXml.cs` — root wrapper for multiple orders
- `CommerceOrderXml.cs` — single order with Person + Order
- `PersonXml.cs` — billing/shipping address attributes, name, MemberId
- `OrderXml.cs` — payment info, status, date, nested TaxInfo/ShippingInfo/LineItems
- `TaxInfoXml.cs` — state/county/stadium tax
- `ShippingInfoXml.cs` — method, handling, discount
- `OrderLineItemsXml.cs` — container for line items
- `OrderLineItemXml.cs` — product type, code, price, quantity, MemberId

All properties as `[XmlAttribute]`, child elements as `[XmlElement]`.

~150 lines, 8 files

#### A3: XML Serialization Helper
**File:** `Commerce.Demo.Web/Api/Helpers/XmlResponseHelper.cs`

```csharp
public static class XmlResponseHelper
{
    public static string Serialize<T>(T obj)
    {
        var serializer = new XmlSerializer(typeof(T));
        var ns = new XmlSerializerNamespaces();
        ns.Add("", ""); // suppress xmlns
        using var writer = new StringWriter();
        serializer.Serialize(writer, obj, ns);
        return writer.ToString();
    }
}
```

~20 lines, 1 file

**Component A Total: 11 files, ~230 lines**

---

### Component B: Business logic, mapper, endpoint wiring

#### B1: Order Export Service (Business Layer)
**Files (in `Commerce.Demo.Business/Features/OrderExport/`):**
- `IOrderExportService.cs`
- `OrderExportService.cs`

```csharp
public interface IOrderExportService
{
    Task<IReadOnlyList<IPurchaseOrder>> GetOrdersSinceAsync(DateTime fromUtc, DateTime? toUtc);
}
```

- Uses `IOrderSearchService.FindPurchaseOrders()` with date filter
- Returns raw `IPurchaseOrder` list
- Logs: query date range, result count, execution duration

~70 lines, 2 files

#### B2: Order-to-XML Mapper
**File:** `Commerce.Demo.Web/Api/Mappers/OrderToImisXmlMapper.cs`

Maps `IPurchaseOrder` → `CommerceOrderXml`:
- `IPurchaseOrder.GetFirstForm()` → shipments, payments, line items
- `IOrderAddress` → `PersonXml` billing/shipping attributes
- `ILineItem` custom properties → `OrderLineItemXml`
- `IPayment` → Order payment attributes
- Tax/shipping from order form totals
- `CustomerContact` → Person `MemberId`
- ProductType derived from variant parent content type

~100 lines, 1 file

#### B3: Endpoint + DI Wiring
**Modified files:**
- `Commerce.Demo.Web/Api/Endpoints.cs` — new `/api/orders` route group
- `Commerce.Demo.Web/Startup.cs` — DI registration

Endpoint:
```
GET /api/orders/export?from={utcDateTime}&to={utcDateTime}
```

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `from` | DateTime (UTC, ISO 8601) | Yes | Start of date range |
| `to` | DateTime (UTC, ISO 8601) | No | End of date range (defaults to UtcNow) |

| Header | Required | Description |
|--------|----------|-------------|
| `X-Api-Key` | Yes | IMIS integration API key |

| Response | Description |
|----------|-------------|
| 200 | `application/xml` — `<CommerceOrders>` with matching orders |
| 400 | Invalid/missing `from` parameter |
| 401 | Missing or invalid API key |
| 500 | Server error (via ExceptionFilter) |

Handler flow:
1. Validate `from` query param (required, valid UTC)
2. Parse optional `to` query param
3. Call `IOrderExportService.GetOrdersSinceAsync(from, to)`
4. Map each `IPurchaseOrder` → `CommerceOrderXml` via mapper
5. Wrap in `CommerceOrdersXml`
6. Serialize via `XmlResponseHelper`
7. Return `Results.Content(xml, "application/xml")`

DI registration:
```csharp
services.Configure<ImisApiOptions>(configuration.GetSection(ImisApiOptions.SectionName));
services.AddScoped<IOrderExportService, OrderExportService>();
```

~50 lines added across 2 files

**Component B Total: 5 files, ~220 lines**

## Implementation Order

1. **Component A** — Infrastructure (auth, XML models, serializer)
2. **Component B** — Business logic, mapper, endpoint

Each delivered separately in the Build ↔ Guide cycle.

## Security Considerations

- API key stored in `appsettings.json` / environment variables (never hardcoded)
- Constant-time string comparison for API key validation
- Input validation on `from`/`to` params (reject non-UTC, reject `from` > `to`)
- No PII in logs (log order numbers and counts only, not customer data)
- Rate limiting consideration for future (not in scope for v1)
- ExceptionFilter already handles `/api` errors with structured responses

## Testing Plan

- **Unit tests** for `OrderExportService` — mock `IOrderSearchService`, verify date filtering
- **Unit tests** for `ApiKeyEndpointFilter` — valid key, invalid key, missing key
- **Unit tests** for `OrderToImisXmlMapper` — verify XML output matches IMIS schema
- **Unit tests** for `XmlResponseHelper` — verify clean XML without namespaces
- **Integration test** (manual) — call endpoint against running instance

## Performance Notes

- Called every 5 minutes by IMIS
- Date-filtered queries should be efficient (narrow time window)
- `XmlSerializer` instances should be cached (static) to avoid repeated reflection
- No caching of results needed (each call fetches fresh data for the time window)

## Totals

| Component | Files | Lines |
|-----------|-------|-------|
| A: Infrastructure | 11 | ~230 |
| B: Business + Endpoint | 5 | ~220 |
| **Total** | **16** | **~450** |
