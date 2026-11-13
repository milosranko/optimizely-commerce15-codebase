# Wishlist Feature - Implementation Plan

## Context

The project needs product wishlist functionality. Currently, `Globals.CartNames.WishList = "WishList"` is defined but nothing is implemented. The wishlist is listed as "Out of Scope" in SPEC.md — this plan will first update SPEC.md and PLAN.md to bring it into scope per F3 protocol, then implement the feature.

**Key design insight:** Optimizely Commerce treats wishlists as named carts via `IOrderRepository`. A wishlist is simply an `ICart` loaded with the name `"WishList"` instead of `"ShoppingCart"`. This means we get persistence, line item management, and market awareness for free — no custom database tables needed.

**Scope:** Backend API only (no frontend). Authenticated users only. Variant-level items (specific SKUs).

---

## Component Breakdown

### Component 1: Update Foundation Docs (SPEC.md + PLAN.md)

Add wishlist feature to SPEC.md under Core Features and update PLAN.md with implementation tasks.

**Files:**
- `SPEC.md` — Move wishlist from "Out of Scope" to Core Features, add user stories + acceptance criteria
- `PLAN.md` — Add wishlist implementation phase

---

### Component 2: Wishlist Business Service

Create the wishlist service in the Business layer following existing feature folder patterns.

**Files to create:**
- `src/Commerce.Demo.Business/Features/Wishlist/Interfaces/IWishlistService.cs`
- `src/Commerce.Demo.Business/Features/Wishlist/Models/WishlistItemModel.cs`

**Files to modify:**
- None (interface + model only, no dependencies on Optimizely Commerce)

**IWishlistService interface:**
```csharp
public interface IWishlistService
{
    IEnumerable<WishlistItemModel> GetItems(Guid contactId, string marketId);
    WishlistItemModel? AddItem(Guid contactId, string variantCode, string marketId);
    bool RemoveItem(Guid contactId, string variantCode, string marketId);
    bool ContainsItem(Guid contactId, string variantCode, string marketId);
    void Clear(Guid contactId, string marketId);
    int GetItemCount(Guid contactId, string marketId);
}
```

**WishlistItemModel:**
```csharp
public class WishlistItemModel
{
    public string Code { get; set; }        // Variant SKU code
    public string DisplayName { get; set; }
    public decimal? Price { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime DateAdded { get; set; }
}
```

---

### Component 3: Wishlist Service Implementation

Implement the service in the Core layer (where Optimizely Commerce dependencies live) following the existing SearchService pattern (interface in Business, implementation in Core).

**Files to create:**
- `src/Commerce.Demo.Optimizely.Core/Features/Wishlist/Services/WishlistService.cs`

**Implementation approach:**
- Use `IOrderRepository.LoadOrCreateCart<ICart>(contactId, CartNames.WishList, marketId)` for load/create
- Use `IOrderRepository.LoadCart<ICart>(contactId, CartNames.WishList, marketId)` for read operations
- Add line items via `cart.GetFirstShipment().LineItems.Add()`
- Remove by filtering on `lineItem.Code`
- Map line items to `WishlistItemModel` using `ReferenceConverter` + `IContentLoader` to resolve variant details
- Use `IPriceService` for current pricing
- Quantity is always 1 for wishlist items (no quantity management)

**Key patterns to reuse:**
- `LineItemExtensions.GetVariant()` from `Commerce.Demo.Optimizely.Core/Extensions/LineItemExtensions.cs` — resolves variant content from line item code
- `IOrderRepository` pattern from `AnonymousCartMergingMiddleware` — cart loading/saving
- `Globals.CartNames.WishList` constant already defined

---

### Component 4: API Endpoints

Create wishlist API endpoints following the existing Minimal API pattern.

**Files to create:**
- `src/Commerce.Demo.Web/Api/WishlistEndpoints.cs`
- `src/Commerce.Demo.Web/Api/Responses/WishlistItemResponse.cs`
- `src/Commerce.Demo.Web/Api/Requests/WishlistRequest.cs`

**File to modify:**
- `src/Commerce.Demo.Web/Startup.cs` — Register `IWishlistService` and mount wishlist endpoints

**Endpoints:**

| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| GET | `/api/wishlist` | Get all wishlist items | Required |
| POST | `/api/wishlist` | Add variant to wishlist | Required |
| DELETE | `/api/wishlist/{variantCode}` | Remove item from wishlist | Required |
| GET | `/api/wishlist/count` | Get wishlist item count | Required |
| GET | `/api/wishlist/{variantCode}` | Check if item is in wishlist | Required |
| DELETE | `/api/wishlist` | Clear entire wishlist | Required |

All endpoints require `.RequireAuthorization()`.

**User identification:** Extract user identity from `HttpContext.User` via `CustomerContext.CurrentContactId` or equivalent Optimizely Commerce identity mapping.

**Response format:** Uses existing `ResponseList<T>`, `ResponseSingle<T>`, `ResponseBase` patterns.

---

### Component 5: DI Registration + Wiring

**File to modify:**
- `src/Commerce.Demo.Web/Startup.cs` — Add to `#region Other Services`:
  ```csharp
  .AddScoped<IWishlistService, WishlistService>()
  ```
- `src/Commerce.Demo.Web/Startup.cs` — Add in `UseEndpoints`:
  ```csharp
  endpoints.AddWishlistEndpoints();
  ```

---

### Component 6: Unit Tests

**Files to create:**
- `src/Commerce.Demo.Tests/Features/Wishlist/WishlistServiceTests.cs`
- `src/Commerce.Demo.Tests/Features/Wishlist/WishlistEndpointsTests.cs`

**Test coverage:**
- Add item to empty wishlist → item appears
- Add duplicate item → no duplicates (idempotent)
- Remove existing item → item removed
- Remove non-existent item → returns false, no error
- Get items from empty wishlist → empty list
- Get item count → correct count
- Clear wishlist → empty
- Contains item check → true/false

---

### Component 7: Documentation Updates

**Files to update:**
- `docs/api.md` — Add wishlist endpoint documentation
- `docs/architecture.md` — Add wishlist service to component inventory
- `docs/devlog.md` — Session entry for wishlist implementation
- `docs/todo.md` — Mark wishlist tasks complete

---

## Implementation Order

1. **Component 1** — Foundation docs (SPEC.md + PLAN.md)
2. **Component 2** — Interface + model (Business layer)
3. **Component 3** — Service implementation (Core layer)
4. **Component 4** — API endpoints (Web layer)
5. **Component 5** — DI wiring (Startup.cs)
6. **Component 6** — Unit tests
7. **Component 7** — Documentation

Components 2-5 follow the strict dependency flow: Business → Core → Web.

---

## Verification

- `dotnet build src\Commerce.Demo.sln` — Solution compiles
- `dotnet test src\Commerce.Demo.Tests\Commerce.Demo.Tests.csproj` — All tests pass
- Manual verification: API endpoints return correct responses for add/remove/get/count/clear operations
