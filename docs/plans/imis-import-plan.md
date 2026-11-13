# Plan: iMIS ↔ Optimizely Commerce Two-Way Integration

## Context

**Problem:** iMIS 20.x (On-Premise) is the authoritative source for events, products, and member/contact data, but this data needs to be available in Optimizely Commerce 14 for the e-commerce storefront. Conversely, orders and event registrations placed in Optimizely need to flow back to iMIS. Currently there is no integration between the two systems.

**Solution:** Build a scheduled (nightly) two-way sync engine:
- **Import (iMIS → Optimizely):** Events, Products, Members/Contacts
- **Export (Optimizely → iMIS):** Orders, Event Registrations

**iMIS API:** REST API with OAuth 2.0 Client Credentials flow. Single endpoint returns all `productFamilies` + `products` together. Separate endpoint for member profiles.

---

## Actual iMIS Data Models

### Member Profile
```json
{
  "memberId": "1037342",        // unique ID (correlation key)
  "corporateId": "1759",        // parent org (empty = individual)
  "email": "user@example.org",
  "prefix": "Atty.",
  "firstName": "Joy",
  "lastName": "Muñoz",
  "phone": "1230211141",
  "company": "State Bar of Wisconsin",
  "title": "Database Administrator",
  "upassEndDate": "02/02/2027", // membership expiration (can be empty)
  "purchaseClaims": "ALLMEM,FASTCASE,FB1000_PRICE,...",  // comma-separated claim codes
  "addresses": [{
    "addressNum": "183097",
    "address1": "5302 Eastpark Blvd",
    "city": "Madison", "state": "WI", "zip": "53718-2101",
    "country": "USA", "county": "COLUMBIA",
    "isPreferredBillingAddress": true,
    "isPreferredDeliverableAddress": true
  }]
}
```

### Product Family (parent container → Optimizely Product)
```json
{
  "id": "EventCA5559",                         // correlation key
  "name": "Joys 2nd Test Meeting With CCRB Credits",
  "description": "",
  "productLine": "CLE Seminars",               // → Optimizely Category
  "definition": "Event Family"                  // discriminator: "Event Family" | "Material Family"
}
```

### Product (purchasable item → Optimizely Variant)
```json
{
  "familyId": "EventCA3778",                    // links to parent family
  "SKU": "CA3778R14/TUITION",                   // correlation key
  "name": "Tuition",
  "format": "AddOnTuition",                     // variant type discriminator
  "memberPrice": 0.00,
  "nonMemberPrice": 0.00,
  "purchasingClaims": "ACTN,EMER,MemberPricing,NonMemberPricing,...",
  "alternateClaimsPricing": "ACTN:99.00,EMER:99.00,M:99.00,NM:149.00,...",
  "inventoryOnHand": 0,
  "maxPurchaseQuantity": 1,
  "isUpass": true,
  "isDeliverable": false,
  "isTaxable": false,
  "cleCredits": 0.00, "eprCredits": 0.00, "lpmCredits": 0.00,
  "locationName": "", "city": "",
  "startDateTime": "", "endDateTime": "",
  // ... plus ~20 more fields (see full JSON in conversation)
}
```

---

## Format → Variant Type Mapping

40+ format values grouped into 3 Optimizely variant types:

| Format Values | → Optimizely Variant | Condition |
|---------------|---------------------|-----------|
| `AddOnTuition` | `EventTuitionVariant` | family.definition = "Event Family" |
| `AddOnOther` | `EventAddonVariant` | family.definition = "Event Family" |
| `InPerson`, `Conference`, `Teleconference`, `Webcast`, `Meeting`, `Tour`, `OnDemand`, `Audio`, `DVD`, `CD`, `FlashDrive` | `EventVariant` | family.definition = "Event Family" |
| `PrintBook`, `Ebook`, `Booklet`, `Booklet25Pack`, `BooksUnBound`, `CourseMaterial`, `CourseMaterialWithCD`, `PDFFile`, `ElectronicFile`, `ReferenceCard` | `BookVariant` | family.productLine = "Books" or "Publications" |
| Everything else | `GenericVariant` (**NEW**) | All other combinations |

## Product Line → Category Mapping

| iMIS productLine | → Optimizely Category |
|-----------------|----------------------|
| `CLE Seminars` | `EventCategory` (exists) |
| `Business Meetings` | `EventCategory` (exists) |
| `Books` | `BookCategory` (exists) |
| `Publications` | `BookCategory` (exists) |
| `Online Forms`, `Law Office Videos`, `Public Resources`, `Donations`, `Services` | `GenericCategory` (**NEW**) |

---

## Architecture Decision: Where Code Lives

Following the existing layered architecture (`Web → Business → ContentTypes → Core`):

| Concern | Project | Rationale |
|---------|---------|-----------|
| iMIS HTTP client, DTOs, OAuth token handler | `Commerce.Demo.Integrations` | No Optimizely deps — matches PayPal pattern |
| New content types (GenericProduct, GenericVariant, GenericCategory) | `Commerce.Demo.Optimizely.ContentTypes` | All content types live here |
| Sync services, mappers, scheduled jobs | `Commerce.Demo.Business` | Needs both iMIS client AND Optimizely APIs |
| DI wiring, configuration | `Commerce.Demo.Web` (Startup.cs) | Entry point for service registration |

---

## Source of Truth Per Entity

| Entity | Source of Truth | Direction |
|--------|----------------|-----------|
| Product Families & Products (catalog) | iMIS | iMIS → Optimizely |
| Members/Contacts + purchaseClaims | iMIS | iMIS → Optimizely |
| Claim-based pricing (alternateClaimsPricing) | iMIS | iMIS → Optimizely |
| CMS-authored content (rich descriptions, images) | Optimizely | Never overwritten |
| Orders/Purchases | Optimizely | Optimizely → iMIS |
| Event Registrations | Optimizely | Optimizely → iMIS |

---

## Implementation Components (7 total)

### Component 1: New Content Types (ContentTypes project)

**New files in `Commerce.Demo.Optimizely.ContentTypes/Commerce/`:**

```
Products/GenericProduct.cs        - Generic product for non-Event, non-Book products
Variants/GenericVariant.cs        - Generic variant with Format property
Categories/GenericCategory.cs     - Category for Donations, Forms, Videos, etc.
```

**`GenericProduct`** (extends `ProductBase`):
- `Format` (string) — iMIS format value
- `ProductLine` (string) — iMIS productLine
- `iMISFamilyId` (string) — correlation key back to iMIS

**`GenericVariant`** (extends `VariantBase`):
- `Format` (string) — e.g., "WLF25", "Services", "UltimatePass"
- `MemberPrice` (decimal)
- `NonMemberPrice` (decimal)
- `AlternateClaimsPricing` (string) — raw claim:price pairs
- `PurchasingClaims` (string) — who can purchase
- `MaxPurchaseQuantity` (int)
- `iMISSKU` (string) — correlation key

**`GenericCategory`** (extends `CategoryBase`):
- Allows `GenericProduct` + `GenericCategory` as children

**Files to modify:**
- `src/Commerce.Demo.Optimizely.Core/Constants/Globals.cs` — add GroupNames/MetaClassNames for Generic types
- `src/Commerce.Demo.Optimizely.ContentTypes/Commerce/Categories/EventCategory.cs` — verify AvailableContentTypes
- `src/Commerce.Demo.Optimizely.ContentTypes/Commerce/Categories/BookCategory.cs` — verify AvailableContentTypes

**Patterns to follow:**
- `EventProduct.cs` → model for `GenericProduct.cs`
- `EventVariant.cs` → model for `GenericVariant.cs`
- `BookCategory.cs` → model for `GenericCategory.cs`

### Component 2: iMIS API Client (Integrations project)

**New files in `Commerce.Demo.Integrations/iMIS/`:**

```
Client/
  IiMISApiClient.cs           - Interface: GetProducts(), GetProfiles(), CreateOrder()
  iMISApiClient.cs             - HttpClient impl with JSON deserialization
  iMISTokenHandler.cs          - DelegatingHandler for OAuth 2.0 token caching
Models/
  iMISOptions.cs               - Config POCO (BaseUrl, TokenEndpoint, ClientId, ClientSecret)
  DTOs/
    iMISProfileDto.cs          - Member profile (memberId, purchaseClaims, addresses)
    iMISAddressDto.cs          - Address sub-object
    iMISProductFamilyDto.cs    - Product family (id, name, productLine, definition)
    iMISProductDto.cs          - Product (SKU, format, prices, claims, credits, dates)
    iMISProductCatalogDto.cs   - Root response: { productFamilies[], products[] }
    iMISOrderDto.cs            - Order request (for export)
Results/
  iMISOperationResult.cs       - Generic result<T> with IsSuccess, ErrorMessage
```

**DTOs match the actual JSON** from iMIS (not generic iMIS Party API):
- `iMISProfileDto` — matches the member JSON with `memberId`, `corporateId`, `purchaseClaims` (comma-separated string), `upassEndDate`, `addresses[]`
- `iMISProductFamilyDto` — matches `productFamilies[]` with `id`, `name`, `productLine`, `definition`
- `iMISProductDto` — matches `products[]` with all 40+ fields: `SKU`, `format`, `alternateClaimsPricing`, credit fields, location fields, etc.

**Key patterns:**
- Follow `PayPalOptions.cs` for `iMISOptions` validation
- Extend `StartupExtensions.AddHttpClients()` with iMIS client registration + `AddStandardResilienceHandler()`
- `iMISTokenHandler` as `DelegatingHandler` — POST /token, cache token with SemaphoreSlim, refresh before expiry

**Files to modify:**
- `src/Commerce.Demo.Integrations/Startup/StartupExtensions.cs`
- `src/Commerce.Demo.Integrations/Commerce.Demo.Integrations.csproj` — no new packages needed

### Component 3: Sync State Infrastructure (Business project)

**New files in `Commerce.Demo.Business/Features/iMISSync/`:**

```
Models/
  SyncState.cs           - Per-entity: LastSuccessfulSync, LastStatus, CheckpointId
  SyncResult.cs          - Per-run: Created/Updated/Skipped/Failed counts
  SyncError.cs           - Individual error (EntityId, Message, IsRetryable)
Interfaces/
  ISyncStateRepository.cs
Services/
  SyncStateRepository.cs - Optimizely DDS-backed implementation
```

### Component 4: Import Services & Mapping (Business project)

**New files:**

```
iMISSync/
  Mappings/
    FormatToVariantTypeMap.cs              - Static map: format string → variant type + category type
    iMISToOptimizelyCatalogMapper.cs       - Creates/updates Optimizely content from iMIS DTOs
    iMISToOptimizelyCustomerMapper.cs      - Creates/updates CustomerContact from profiles
  Interfaces/
    IiMISCatalogSyncService.cs             - Unified: imports both families + products
    IiMISCustomerSyncService.cs            - Imports member profiles
  Services/
    iMISCatalogSyncService.cs
    iMISCustomerSyncService.cs
```

**Catalog import flow:**
1. Call iMIS API → get `{ productFamilies[], products[] }`
2. Group products by `familyId`
3. For each family:
   a. Determine category by `productLine` → find/create appropriate Category
   b. Determine product type by `definition`:
      - "Event Family" → `EventProduct`
      - "Material Family" + productLine "Books"/"Publications" → `BookProduct`
      - Everything else → `GenericProduct`
   c. Upsert product (search by `Code = family.id`, create or update)
   d. For each product in that family:
      - Determine variant type by `format` (see mapping table above)
      - Upsert variant (search by `Code = product.SKU`)
      - Set pricing: `memberPrice`, `nonMemberPrice`, `alternateClaimsPricing`
      - Set credit fields, dates, location, flags

**Customer import flow:**
1. Call iMIS API → get member profiles array
2. For each profile:
   - Find/create `CustomerContact` by `memberId` (stored as external ID)
   - Map: firstName, lastName, email, company, phone, addresses
   - Store `purchaseClaims` as comma-separated string on customer properties
   - Store `upassEndDate` for membership expiration tracking

**Correlation keys:**
- Product Family → `Code = family.id` (e.g., "EventCA5559")
- Product/Variant → `Code = product.SKU` (e.g., "CA3778R14/TUITION")
- Customer → `memberId` stored on CustomerContact

### Component 5: Export Services & Mapping (Business project)

**New files:**

```
iMISSync/
  Mappings/
    OptimizelyToIMISMapper.cs     - Maps orders → iMIS DTOs
  Interfaces/
    IiMISOrderExportService.cs
  Services/
    iMISOrderExportService.cs     - Exports orders + event registrations
```

**Export logic:**
- Query orders with `Modified > lastExportTimestamp`
- Track exported order IDs in `SyncState` to prevent duplicates
- Map `IPurchaseOrder` → `iMISOrderDto`
- POST to iMIS; handle 409 (duplicate) gracefully

### Component 6: Scheduled Jobs (Business project)

**New files:**

```
ScheduledJobs/
  iMISImportScheduledJob.cs      - Nightly at 2 AM
  iMISExportScheduledJob.cs      - Nightly at 3 AM
```

**Two separate jobs** for independent monitoring/scheduling:
- **Import:** CatalogSyncService → CustomerSyncService. Supports `Stop()` between phases.
- **Export:** OrderExportService. Same pattern.
- Both use `[ScheduledPlugIn(DefaultEnabled = false)]`
- Dev note: Scheduler disabled in dev mode (Startup.cs line 80). Trigger manually from CMS Admin.

### Component 7: Configuration & DI Wiring (Web project)

**Files to modify:**

- `src/Commerce.Demo.Web/appsettings.json` — add `iMIS` section (no secrets)
- `src/Commerce.Demo.Web/Startup.cs` — register in `#region Other Services`

```json
{
  "iMIS": {
    "BaseUrl": "https://[host]/api",
    "TokenEndpoint": "https://[host]/token",
    "ClientId": "",
    "ClientSecret": ""
  }
}
```

Secrets via environment variables in production: `iMIS__ClientId`, `iMIS__ClientSecret`

---

## Claim-Based Pricing Strategy

The `alternateClaimsPricing` field (e.g., `"ACTN:99.00,M:99.00,NM:149.00,UPASS:0.00"`) combined with member `purchaseClaims` drives pricing:

1. **Import:** Store `alternateClaimsPricing` as a string property on each variant
2. **Runtime pricing:** A custom `IPriceService` or price resolver checks the current customer's `purchaseClaims` against the variant's `alternateClaimsPricing` to determine the applicable price
3. **Existing infrastructure:** `CommerceInitialization.cs` already creates MetaFields for claims like `FASTCASE`, `FB1000_PRICE`, `FB2000` — the import must align with this existing metadata

---

## Error Handling & Resilience

- **Per-entity isolation:** Failure on one product/member does not stop the batch
- **Retry via Polly:** `AddStandardResilienceHandler()` on HttpClient
- **Checkpoint recovery:** SyncState stores last successful timestamp; interrupted runs resume
- **No PII in logs:** Never log ClientSecret, tokens; emails only at DEBUG level

---

## Critical Files Reference

| File | Role |
|------|------|
| `src/Commerce.Demo.Integrations/Startup/StartupExtensions.cs` | Extend with `AddIMISClient()` |
| `src/Commerce.Demo.Integrations/PaymentProviders/PayPal/PayPalOptions.cs` | Pattern for iMISOptions |
| `src/Commerce.Demo.Web/Startup.cs` | DI wiring (line 178-188 `#region Other Services`) |
| `src/Commerce.Demo.Web/appsettings.json` | Add iMIS config section |
| `src/Commerce.Demo.Optimizely.Core/Constants/Globals.cs` | Add Generic group/metaclass names |
| `src/Commerce.Demo.Optimizely.ContentTypes/Commerce/Products/EventProduct.cs` | Pattern for GenericProduct |
| `src/Commerce.Demo.Optimizely.ContentTypes/Commerce/Variants/EventVariant.cs` | Pattern for GenericVariant |
| `src/Commerce.Demo.Optimizely.ContentTypes/Commerce/Categories/BookCategory.cs` | Pattern for GenericCategory |
| `src/Commerce.Demo.Web/Infrastructure/CommerceInitialization.cs` | Existing PurchaseClaims MetaFields |

---

## Verification Plan

1. **Build:** `dotnet build src\Commerce.Demo.sln -c Release`
2. **Unit tests:** Mock HttpMessageHandler → test iMIS client + token handler
3. **Unit tests:** Mock IiMISApiClient + IContentRepository → test sync services + mappers
4. **Integration:** Configure against real iMIS, trigger import job from CMS Admin → verify catalog content
5. **Tests:** `dotnet test src\Commerce.Demo.Tests\Commerce.Demo.Tests.csproj`
