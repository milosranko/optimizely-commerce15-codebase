# Plan: iMIS Customer Profiles → Optimizely Commerce Import

## Context

**Problem:** iMIS is the authoritative source for member/contact data (profiles, addresses, purchase claims, membership expiration). This data needs to be imported into Optimizely Commerce as `CustomerContact` records so that:
- Members can log in and see their profile
- Claim-based pricing resolves correctly at checkout (variant `alternateClaimsPricing` matched against customer `purchaseClaims`)
- Membership status (`upassEndDate`) drives access to gated content/products
- Corporate relationships (`corporateId`) are preserved

**Frequency:** Nightly scheduled import (runs as part of the iMIS Import Scheduled Job at 2 AM)

**Prerequisite:** iMIS API client + OAuth token handler must exist (Component 2 of the main iMIS integration plan)

---

## Actual iMIS Member Profile JSON Schema

```json
{
  "memberId": "1037342",
  "corporateId": "1759",
  "email": "jzajda@wisbar.org",
  "prefix": "Atty.",
  "firstName": "Joy",
  "lastName": "Muñoz",
  "phone": "1230211141",
  "company": "State Bar of Wisconsin",
  "title": "Database Administrator",
  "upassEndDate": "02/02/2027",
  "purchaseClaims": "ALLMEM,FASTCASE,FB1000_PRICE,FB2000,...",
  "addresses": [
    {
      "addressNum": "183097",
      "address1": "5302 Eastpark Blvd",
      "address2": "",
      "city": "Madison",
      "state": "WI",
      "zip": "53718-2101",
      "country": "USA",
      "county": "COLUMBIA",
      "isPreferredBillingAddress": true,
      "isPreferredDeliverableAddress": true
    }
  ]
}
```

**Key observations from sample data:**
- `memberId` is always present, unique — correlation key
- `corporateId` can be empty (individual members) or populated (org-affiliated)
- `email` always present
- `phone` format varies: `"1230211141"` vs `"(608) 250-6175"` — store as-is, no normalization
- `upassEndDate` format: `"MM/dd/yyyy"` or empty string — parse with `DateTime.TryParseExact`
- `purchaseClaims` is a comma-separated string of claim codes — store as-is on customer
- `addresses` is always an array (typically 1 entry); flags indicate billing/shipping preference
- `county` is present on addresses — maps to the custom `County` metafield already planned for order export

---

## Data Mapping: iMIS → Optimizely CustomerContact

| iMIS Field | Optimizely CustomerContact Property | Type | Notes |
|-----------|-------------------------------------|------|-------|
| `memberId` | `ImisMemberId` (custom) | string | **Correlation key** — used to find/update existing contacts |
| `corporateId` | `ImisCorporateId` (custom) | string | Parent org ID; empty = individual member |
| `email` | `Email` (built-in) | string | Primary email |
| `prefix` | `ImisPrefix` (custom) | string | e.g., "Atty.", "Ms.", "Mrs." |
| `firstName` | `FirstName` (built-in) | string | |
| `lastName` | `LastName` (built-in) | string | |
| `phone` | `ImisPhone` (custom) | string | Stored as-is (no format normalization) |
| `company` | `ImisCompany` (custom) | string | |
| `title` | `ImisJobTitle` (custom) | string | Job title at company |
| `upassEndDate` | `ImisUpassEndDate` (custom) | DateTime? | Membership expiration; null if empty |
| `purchaseClaims` | `ImisPurchaseClaims` (custom) | string | Comma-separated claim codes (stored raw) |
| — | `ContactGroup` (built-in enum) | enum | Set to "Member" or "Non-Member" based on claims logic |

### Address Mapping: iMIS → Optimizely CustomerAddress

| iMIS Field | Optimizely CustomerAddress Property | Notes |
|-----------|-------------------------------------|-------|
| `addressNum` | `Name` | Unique identifier for the address |
| `address1` | `Line1` | |
| `address2` | `Line2` | |
| `city` | `City` | |
| `state` | `State` | |
| `zip` | `PostalCode` | |
| `country` | `CountryCode` | "USA" → "US" (ISO 3166 conversion) |
| `county` | `County` (custom metafield) | Custom field — matches order export plan |
| `isPreferredBillingAddress` | `AddressType` = Billing | If true, mark as preferred billing |
| `isPreferredDeliverableAddress` | `AddressType` = Shipping | If true, mark as preferred shipping |

---

## Member vs Non-Member Classification

The `ContactGroup` enum (already created in `CommerceInitialization.cs` lines 47–55) classifies contacts:

| Condition | ContactGroup |
|-----------|-------------|
| `purchaseClaims` contains `ALLMEM` or `GOODMEM` or `STAFF` | **Member** |
| `upassEndDate` is in the future | **Member** |
| Otherwise | **Non-Member** |

This drives pricing: Member contacts get member pricing, Non-Member contacts get non-member pricing.

---

## Architecture: Where Code Lives

| Concern | Project | Rationale |
|---------|---------|-----------|
| `iMISProfileDto`, `iMISAddressDto` | `Commerce.Demo.Integrations` | DTOs for API response — no Optimizely deps |
| `IiMISCustomerSyncService`, mapper | `Commerce.Demo.Business` | Needs `CustomerContact` API (Mediachase.Commerce.Customers) |
| Custom metafield registration | `Commerce.Demo.Web` (`CommerceInitialization.cs`) | Extends existing MetaDataPlus init |
| DI wiring, configuration | `Commerce.Demo.Web` (`Startup.cs`) | Entry point |

---

## Implementation Components (4 total)

### Component 1: iMIS Profile DTOs (Integrations project)

**New files in `Commerce.Demo.Integrations/iMIS/Models/DTOs/`:**

```
iMISProfileDto.cs      - Member profile (sealed record)
iMISAddressDto.cs      - Address sub-object (sealed record)
```

**`iMISProfileDto`** (follows `PayPalPayerInfo` sealed record pattern):
```csharp
public sealed record iMISProfileDto
{
    public string MemberId { get; init; } = string.Empty;
    public string? CorporateId { get; init; }
    public string? Email { get; init; }
    public string? Prefix { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Phone { get; init; }
    public string? Company { get; init; }
    public string? Title { get; init; }
    public string? UpassEndDate { get; init; }
    public string? PurchaseClaims { get; init; }
    public IReadOnlyList<iMISAddressDto>? Addresses { get; init; }
}
```

**`iMISAddressDto`:**
```csharp
public sealed record iMISAddressDto
{
    public string? AddressNum { get; init; }
    public string? Address1 { get; init; }
    public string? Address2 { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Zip { get; init; }
    public string? Country { get; init; }
    public string? County { get; init; }
    public bool IsPreferredBillingAddress { get; init; }
    public bool IsPreferredDeliverableAddress { get; init; }
}
```

~50 lines, 2 files

**Files to modify:**
- `Commerce.Demo.Integrations/iMIS/Client/IiMISApiClient.cs` — add `GetProfilesAsync()` method to interface
- `Commerce.Demo.Integrations/iMIS/Client/iMISApiClient.cs` — implement the method

### Component 2: Custom MetaFields on CustomerContact (Web project)

**File to modify:** `Commerce.Demo.Web/Infrastructure/CommerceInitialization.cs`

Add custom metafields to the `Contact` metaclass so `CustomerContact` can store iMIS-specific data:

| MetaField Name | MetaDataType | Length | Purpose |
|---------------|-------------|--------|---------|
| `ImisMemberId` | `ShortString` | 20 | Correlation key |
| `ImisCorporateId` | `ShortString` | 20 | Parent org |
| `ImisPrefix` | `ShortString` | 20 | Name prefix |
| `ImisPhone` | `ShortString` | 30 | Phone (varied format) |
| `ImisCompany` | `ShortString` | 200 | Company name |
| `ImisJobTitle` | `ShortString` | 200 | Job title |
| `ImisUpassEndDate` | `DateTime` | — | Membership expiration |
| `ImisPurchaseClaims` | `LongString` | — | Comma-separated claims (can be 500+ chars) |

**Pattern** — follows existing `PurchaseClaims` DictionaryMultiValue creation on lines 34–44:
```csharp
var contactMetaClass = MetaClass.Load(MetaDataContext.Instance, "Contact");
AddMetaFieldIfMissing(contactMetaClass, "ImisMemberId", MetaDataType.ShortString, 20);
AddMetaFieldIfMissing(contactMetaClass, "ImisCorporateId", MetaDataType.ShortString, 20);
// ... etc.
```

**Also add:** Index on `ImisMemberId` for fast lookup during sync.

~60 lines added to existing file

### Component 3: Customer Sync Service & Mapper (Business project)

**New files in `Commerce.Demo.Business/Features/iMISSync/`:**

```
Interfaces/
  IiMISCustomerSyncService.cs
Services/
  iMISCustomerSyncService.cs
Mappings/
  iMISToOptimizelyCustomerMapper.cs
```

**`IiMISCustomerSyncService`:**
```csharp
public interface IiMISCustomerSyncService
{
    Task<SyncResult> SyncCustomersAsync(CancellationToken cancellationToken = default);
}
```

**`iMISCustomerSyncService`** flow:
1. Call `IiMISApiClient.GetProfilesAsync()` → get all member profiles
2. For each profile:
   a. Search for existing `CustomerContact` by `ImisMemberId` metafield
   b. If not found → create new `CustomerContact`
   c. If found → update existing
   d. Map profile fields via `iMISToOptimizelyCustomerMapper`
   e. Sync addresses (upsert by `addressNum`, delete addresses no longer in iMIS)
   f. Set `ContactGroup` to Member or Non-Member
   g. Save contact
3. Return `SyncResult` with created/updated/skipped/failed counts

**`iMISToOptimizelyCustomerMapper`** — static mapper class:
- `MapProfile(iMISProfileDto dto, CustomerContact contact)` — maps all profile fields
- `MapAddress(iMISAddressDto dto, CustomerAddress address)` — maps address fields
- `DetermineContactGroup(iMISProfileDto dto)` → "Member" or "Non-Member"
- `ParseUpassEndDate(string? dateStr)` → `DateTime?` (uses `MM/dd/yyyy` format)
- `NormalizeCountryCode(string? country)` → ISO 3166 (e.g., "USA" → "US")

**CustomerContact lookup strategy:**
```csharp
// Search by custom metafield
var contacts = CustomerContact.List(0, 1,
    new FilterElement[]
    {
        new FilterElement("ImisMemberId", FilterElementType.Equal, memberId)
    },
    null);
```

~200 lines, 3 files

### Component 4: DI Wiring & Integration with Import Job

**Files to modify:**

- `Commerce.Demo.Web/Startup.cs` — register `IiMISCustomerSyncService`:
```csharp
services.AddScoped<IiMISCustomerSyncService, iMISCustomerSyncService>();
```

- `Commerce.Demo.Business/ScheduledJobs/iMISImportScheduledJob.cs` (from main plan) — add customer sync as Phase 2:
```
Phase 1: Catalog sync (families + products)
Phase 2: Customer sync (profiles + addresses)  ← THIS
```

~10 lines across 2 files

---

## Error Handling

- **Missing required fields:** If `memberId` is empty → log ERROR, skip profile
- **Invalid email:** Log WARNING, still import (email not strictly required for contact creation)
- **Date parse failure:** `upassEndDate` in unexpected format → log WARNING, set `ImisUpassEndDate = null`
- **Address sync failure:** Per-address isolation — one bad address doesn't block other addresses or the profile
- **Per-profile isolation:** Each profile is independent; one failure doesn't stop the batch
- **Duplicate memberId:** Should not happen (iMIS enforces uniqueness), but if found → update first match, log WARNING

---

## Security Considerations

- **No PII in INFO logs:** Log `memberId` and counts only; email/name only at DEBUG level
- **purchaseClaims stored as-is:** No interpretation at import time; resolved at runtime by pricing service
- **No password sync:** Members authenticate via separate SSO/login mechanism, not imported passwords

---

## Edge Cases

| Scenario | Handling |
|----------|---------|
| Member with no addresses | Create contact without addresses; log INFO |
| Member with multiple addresses | Import all; flag preferred billing/shipping per `isPreferred*` booleans |
| `corporateId` empty | Set `ImisCorporateId = null`; treated as individual member |
| `upassEndDate` empty | Set `ImisUpassEndDate = null`; classified as Non-Member unless claims say otherwise |
| `purchaseClaims` very long (500+ chars) | Stored in `LongString` metafield (no length limit) |
| Phone format varies | Stored as-is; no normalization |
| Unicode in names (e.g., "Muñoz") | Handled natively by .NET strings + SQL Server nvarchar |
| Member removed from iMIS | **Not deleted** from Optimizely — orphan cleanup is a separate concern |

---

## Critical Files Reference

| File | Role |
|------|------|
| `src/Commerce.Demo.Web/Infrastructure/CommerceInitialization.cs` | Add custom metafields to Contact metaclass (lines 34–55 pattern) |
| `src/Commerce.Demo.Integrations/PaymentProviders/PayPal/Models/PayPalPayerInfo.cs` | Pattern for sealed record DTOs |
| `src/Commerce.Demo.Integrations/Startup/StartupExtensions.cs` | Extend with iMIS client (if not already done) |
| `src/Commerce.Demo.Web/Startup.cs` | DI wiring (line 178-188 `#region Other Services`) |
| `src/Commerce.Demo.Optimizely.Core/Constants/Globals.cs` | Constants reference |

---

## Verification Plan

1. **Build:** `dotnet build src\Commerce.Demo.sln -c Release`
2. **Unit tests:** Mock `IiMISApiClient` → test sync service with sample JSON (3 profiles above)
3. **Unit tests:** Test mapper — profile mapping, address mapping, date parsing, country code normalization, contact group classification
4. **Unit tests:** Test edge cases — empty addresses, empty upassEndDate, long purchaseClaims
5. **Integration test:** Configure against real iMIS → trigger import from CMS Admin → verify:
   - CustomerContacts created with correct `ImisMemberId`
   - Addresses populated with correct billing/shipping flags
   - `ContactGroup` set to Member or Non-Member correctly
   - `ImisPurchaseClaims` stored as full comma-separated string
6. **Tests:** `dotnet test src\Commerce.Demo.Tests\Commerce.Demo.Tests.csproj`
