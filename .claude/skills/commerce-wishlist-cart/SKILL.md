---
name: commerce-wishlist-cart
description: Build or change a named cart in Optimizely Commerce Connect (CMS 12) - a wishlist, saved-for-later, quote, or any second basket that is a cart under its own name rather than the shopping cart. Covers the cart-name constant, LoadCart vs LoadOrCreateCart, the customer key, the service and model builder, minimal API endpoints and member-only authorization, moving items between carts with their folded add-on lines and properties intact, pricing lens selection, and clean-slate deletion of an emptied cart. Use whenever asked to add a wishlist, save-for-later or comparable list, to move items between carts, or to debug a wishlist that is empty, mispriced, or losing event registrations.
---

# Building a named cart (wishlist)

In Optimizely Commerce a wishlist is not a separate entity - **it is a cart stored under a different name**. Everything follows from that: same `IOrderRepository`, same `ICart`, same line items, same shipment. Only the name and the rules differ.

This site has three named carts in `Globals.CartNames`: `ShoppingCart`, `WishList`, and `FreeRegistration` (an ephemeral single-registration cart for the $0 fast path). Read `.claude/rules/commerce.md` before starting.

## Step 1: Orient

```bash
ls src/Wisbar.Business/Features/Wishlist/
grep -n "CartNames" src/Wisbar.Optimizely.Core/Constants/Globals.cs
```

| Part | Location |
|---|---|
| Cart name constants | `Globals.CartNames` |
| Service + model builder | `Wisbar.Business/Features/Wishlist/Services/` |
| Models | `Wisbar.Models/Wishlist/` |
| Endpoints | `Wisbar.Web/Api/WishlistEndpoints.cs` |
| Page | `ContentTypes/Cms/Pages/WishlistPage.cs`, `Web/Controllers/WishlistPageController.cs` |
| Tests | `Wisbar.Tests/Features/Wishlist/WishlistServiceTests.cs` |

## Step 2: Name the cart, then load it correctly

```csharp
public static class CartNames
{
    public const string ShoppingCart = "ShoppingCart";
    public const string WishList = "WishList";
    public const string FreeRegistration = "FreeRegistration";
}
```

Add a constant; never inline the name. Then pick the right load:

| Call | Use when |
|---|---|
| `LoadOrCreateCart<ICart>(customerId, name)` | The caller intends to write, or an empty list is a valid answer (get, add, move-target) |
| `LoadCart<ICart>(customerId, name)` | A read where absence is meaningful. Returns **null** - handle it |

`IsInWishlist` uses `LoadCart` and treats null as "not in the list". `AddItemAsync` uses `LoadOrCreateCart` because saving an item must create the list. Getting this backwards either throws on first use or litters the database with empty carts.

## Step 3: The customer key

Every cart is keyed by `ICurrentIdentity.CustomerId` - the contact id for a signed-in member, the Commerce anonymous id otherwise. Take it from `ICurrentIdentity`, never from `CustomerContext.Current` directly.

**This is why the wishlist is member-only.** The endpoints use the member policy explicitly:

```csharp
// The member policy, not the default one: the default policy runs over the CMS scheme, so a
// signed-in editor would pass and get a wishlist keyed to a contact they don't own.
var group = builder.MapGroup("/api/wishlist").RequireAuthorization(Authentication.MemberPolicy);
```

Any new named cart that belongs to a person needs the same treatment. Note also that the anonymous cart merge on sign-in covers `ShoppingCart` only - a named cart built for anonymous visitors would not survive login unless you add that yourself.

## Step 4: Service shape

One service owning the cart's rules, returning a **built model**, not the cart:

```csharp
Task<WishlistModel> GetWishlistAsync(Guid customerId, string memberId);
bool                IsInWishlist(Guid customerId, string variantCode);
Task<WishlistModel> AddItemAsync(Guid customerId, string memberId, string variantCode);
Task<WishlistModel> RemoveItemAsync(Guid customerId, string memberId, string variantCode);
Task<(WishlistModel Wishlist, CartModel Cart)> MoveFromCartAsync(...);
Task<(WishlistModel Wishlist, CartModel Cart)> MoveToCartAsync(...);
```

Move operations return **both** models, because both changed and the page has to re-render both.

Add is idempotent by design - it checks `FindLineItem` first, so adding twice is a no-op rather than a quantity bump. A wishlist holds one of each thing.

## Step 5: The rule that makes this non-trivial - folded add-on lines

An event registration is not one line. The event line is a $0 container, with tuition and extras as separate add-on lines carrying `ParentEventCode`. **Any operation on the parent must carry its add-ons**, or you strand lines that are hidden (they point at a departed parent), still priced into the total, and unreachable by any UI.

```csharp
private static List<ILineItem> FoldedAddOns(ICart cart, string eventVariantCode) =>
    cart.GetFirstShipment()?.LineItems
        .Where(li => !li.Code.Equals(eventVariantCode, StringComparison.OrdinalIgnoreCase)
                     && string.Equals(li.GetParentEventCode(), eventVariantCode, StringComparison.OrdinalIgnoreCase))
        .ToList() ?? [];
```

Remove, move-from-cart and move-to-cart all call it. A new operation on a named cart must too.

**Moving is a copy, not a transfer** - a line item belongs to its cart. Create a new line on the destination and copy across, including the whole `Properties` bag, or `ParentEventCode` and the registration answers are lost:

```csharp
private static void CopyProperties(ILineItem source, ILineItem dest)
{
    dest.DisplayName = source.DisplayName;
    dest.PlacedPrice = source.PlacedPrice;
    dest.IsGift = source.IsGift;
    dest.IsInventoryAllocated = source.IsInventoryAllocated;
    foreach (string key in source.Properties.Keys)
        dest.Properties[key] = source.Properties[key];
}
```

**Registrations replace, they do not merge.** Saving a registration that already exists in the wishlist removes the stale copy first - the existing one may be a bare event line saved from the product page heart (no container flag, no add-ons, no answers), and merging would leave a mixed unit. The cart's copy is newer and complete, and is the one the customer is looking at.

## Step 6: Pricing lens

The model builder takes a **keyed** price service:

```csharp
// The product page's pricing lens (profile claims only). The unkeyed service is the cart-aware one,
// which would price the wishlist off whatever is in the cart and leave the two pages disagreeing
// about the same product.
[FromKeyedServices(WisbarConstants.ServiceKeys.CatalogPricing)] IPriceService priceService
```

A saved list is a catalog view, not a basket view. Resolve prices through `IPriceService` and inventory through `IInventoryService` - never from the database, per the commerce rules.

## Step 7: Clean-slate deletion

When a move empties the source cart, delete it rather than saving an empty record:

```csharp
if (shopping.IsVisiblyEmpty())
{
    _orderRepository.Delete(shopping.OrderLink);
    cartModel = _cartBuilder.BuildEmpty(market);
}
```

`IsVisiblyEmpty` covers the case where only hidden add-ons orphaned by a removed parent remain. This matches the other removal paths in `CartService` - keep the behavior consistent or the cart badge and the cart page disagree.

## Step 8: Endpoints

Minimal API group, member-policy authorized, returning the built model in the site's response envelope:

```csharp
group.MapGet("/",                       GetWishlist);
group.MapPost("/items",                 AddItem);
group.MapDelete("/items/{variantCode}", RemoveItem);
group.MapPost("/items/move-from-cart",  MoveFromCart);
group.MapPost("/items/move-to-cart",    MoveToCart);
```

Catch `ArgumentException` from the service and return `BadRequest` with the message; let anything else surface. Register the group in `Startup` alongside the other API endpoints.

## Step 9: Verify

```bash
dotnet build src/Wisbar.sln
dotnet test src/Wisbar.Tests/Wisbar.Tests.csproj
```

Report which of these you actually checked:

- Add is idempotent - adding the same item twice leaves one line.
- Moving an event registration carries its add-on lines both ways, with `ParentEventCode` and answers intact.
- Re-saving an existing registration replaces rather than merges.
- Emptying the source cart deletes it, and the badge and cart page agree.
- Prices match the product page, not the cart's pricing lens.
- Endpoints reject an anonymous caller **and** a signed-in CMS editor.

## Checklist

- [ ] Cart name added to `Globals.CartNames`, never inlined
- [ ] `LoadOrCreateCart` for write/get paths, `LoadCart` with a null check for reads
- [ ] Keyed by `ICurrentIdentity.CustomerId`, not `CustomerContext.Current`
- [ ] Endpoints use the member policy, not the default policy
- [ ] Service returns built models; move operations return both
- [ ] Add is idempotent
- [ ] Every operation carries folded add-on lines
- [ ] Moves copy the full `Properties` bag to a new line item
- [ ] Registrations replace rather than merge
- [ ] Catalog pricing lens for a saved list
- [ ] Emptied source cart deleted, not saved empty
- [ ] `IOrderRepository` throughout; no `CartHelper` or `OrderContext`
- [ ] Tests added; suite green

Details and code: `references/implementation.md`.
