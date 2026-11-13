# Named cart (wishlist) implementation reference

Companion to `SKILL.md`. Verify details against the code before relying on them.

## File map

| Layer | File |
|---|---|
| Cart names | `Wisbar.Optimizely.Core/Constants/Globals.cs` -> `CartNames` |
| Service contract | `Business/Features/Wishlist/Interfaces/IWishlistService.cs` |
| Service | `Business/Features/Wishlist/Services/WishlistService.cs` |
| Model builder | `Business/Features/Wishlist/Services/WishlistModelBuilder.cs` |
| Models | `Wisbar.Models/Wishlist/` - `WishlistModel`, `WishlistItemModel`, `WishlistItemPrice`, `WishlistItemDate` |
| Endpoints | `Web/Api/WishlistEndpoints.cs`, `Web/Api/Requests/AddWishlistItemRequest.cs` |
| Page | `ContentTypes/Cms/Pages/WishlistPage.cs`, `Web/Controllers/WishlistPageController.cs`, `Web/Views/WishlistPage/Index.cshtml` |
| Tracking | `Web/Views/Shared/PagePartials/Wishlist/_WishlistTracking.cshtml` |
| Tests | `Wisbar.Tests/Features/Wishlist/WishlistServiceTests.cs` |

## Cart names

```csharp
public static class CartNames
{
    public const string ShoppingCart = "ShoppingCart";
    public const string WishList = "WishList";

    // Ephemeral, single-registration cart used only by the $0 event-registration fast path. The
    // event is placed as its own order directly from this cart so the member's ShoppingCart is
    // never swept into the order; the cart is deleted the moment the order is saved.
    public const string FreeRegistration = "FreeRegistration";
}
```

`FreeRegistration` is the pattern for a **transient** named cart: created for one operation, deleted immediately after. Worth copying when a flow needs an isolated basket that must not disturb the customer's real cart.

## Service contract

```csharp
public interface IWishlistService
{
    Task<WishlistModel> GetWishlistAsync(Guid customerId, string memberId);
    bool                IsInWishlist(Guid customerId, string variantCode);
    Task<WishlistModel> AddItemAsync(Guid customerId, string memberId, string variantCode);
    Task<WishlistModel> RemoveItemAsync(Guid customerId, string memberId, string variantCode);

    /// <summary>Removes item from the shopping cart and adds it to the wishlist.</summary>
    Task<(WishlistModel Wishlist, CartModel Cart)>
        MoveFromCartAsync(Guid customerId, string memberId, string variantCode);

    /// <summary>Removes item from the wishlist and adds it to the shopping cart.</summary>
    Task<(WishlistModel Wishlist, CartModel Cart)>
        MoveToCartAsync(Guid customerId, string memberId, string variantCode);
}
```

Dependencies: `IOrderRepository`, `IOrderGroupFactory`, `ICurrentMarket`, plus the two model builders.

## Load, add, check

```csharp
public Task<WishlistModel> GetWishlistAsync(Guid customerId, string memberId)
{
    var cart = _orderRepository.LoadOrCreateCart<ICart>(customerId, CartNames.WishList);
    return Task.FromResult(_wishlistBuilder.Build(cart, customerId, memberId));
}

public bool IsInWishlist(Guid customerId, string variantCode)
{
    var cart = _orderRepository.LoadCart<ICart>(customerId, CartNames.WishList);
    return cart is not null &&
           cart.GetAllLineItems()
               .Any(li => li.Code.Equals(variantCode, StringComparison.OrdinalIgnoreCase));
}

public Task<WishlistModel> AddItemAsync(Guid customerId, string memberId, string variantCode)
{
    var cart = _orderRepository.LoadOrCreateCart<ICart>(customerId, CartNames.WishList);

    // Idempotent: a wishlist holds one of each thing, so a second add is a no-op, not a quantity bump.
    if (cart.FindLineItem(variantCode) is null)
    {
        var lineItem = _orderGroupFactory.CreateLineItem(variantCode, cart);
        lineItem.Quantity = 1m;
        cart.AddLineItem(lineItem, _orderGroupFactory);
        _orderRepository.Save(cart);
    }

    return Task.FromResult(_wishlistBuilder.Build(cart, customerId, memberId));
}
```

Code comparisons are `OrdinalIgnoreCase` throughout - catalog codes are case-insensitive here.

## Remove, with folded add-ons

```csharp
public Task<WishlistModel> RemoveItemAsync(Guid customerId, string memberId, string variantCode)
{
    var cart = _orderRepository.LoadCart<ICart>(customerId, CartNames.WishList)
               ?? throw new ArgumentException($"Wishlist not found for customer '{customerId}'.");
    var shipment = cart.GetFirstShipment();
    var lineItem = cart.FindLineItem(variantCode)
                   ?? throw new ArgumentException($"Item '{variantCode}' not found in wishlist.", nameof(variantCode));

    shipment!.LineItems.Remove(lineItem);

    // A saved registration is one item to the customer: its add-on lines have no row of their
    // own, so leaving them behind would strand lines nothing can ever reach or remove.
    foreach (var addOn in FoldedAddOns(cart, variantCode))
        shipment.LineItems.Remove(addOn);

    _orderRepository.Save(cart);

    return Task.FromResult(_wishlistBuilder.Build(cart, customerId, memberId));
}
```

`ArgumentException` is the service's "bad request" signal; the endpoint maps it to a 400.

## Move from cart to wishlist

The most involved operation. Four concerns in order: gather the whole unit, replace a stale registration, copy lines across, clean up the source.

```csharp
var market = _currentMarket.GetCurrentMarket();
var wishlist = _orderRepository.LoadOrCreateCart<ICart>(customerId, CartNames.WishList);
var shopping = _orderRepository.LoadCart<ICart>(customerId, CartNames.ShoppingCart)
               ?? throw new ArgumentException("Shopping cart not found.", nameof(customerId));

var source = shopping.FindLineItem(variantCode)
             ?? throw new ArgumentException($"Item '{variantCode}' not found in cart.", nameof(variantCode));

// An event line is only the $0 container - its tuition and extras are separate add-on lines
// folded onto it. Move the whole registration, or the add-ons stay behind in the cart with
// their parent gone: still hidden (they keep pointing at the departed event) yet still priced
// into the total, and unreachable by any UI. CopyProperties carries ParentEventCode and the
// registration answers across, so the saved registration stays whole.
var lines = new List<ILineItem> { source };
lines.AddRange(FoldedAddOns(shopping, variantCode));

// Saving a registration replaces whatever copy the wishlist already holds, rather than
// letting the per-line guard below merge into it. The existing copy can be a bare event line
// saved by the PDP heart (no container flag, no add-ons, no answers) or a registration with a
// different tuition, and merging leaves a mixed unit: the stale event line kept, the incoming
// add-ons landing beside it. The cart's copy is the newer and complete one - and the one the
// customer is looking at when they save.
if (source.IsEventRegistration() && wishlist.FindLineItem(variantCode) is not null)
{
    var stale = new List<ILineItem> { wishlist.FindLineItem(variantCode)! };
    stale.AddRange(FoldedAddOns(wishlist, variantCode));
    foreach (var line in stale)
        wishlist.GetFirstShipment()!.LineItems.Remove(line);
}

foreach (var line in lines)
{
    if (wishlist.FindLineItem(line.Code) is null)
    {
        var dest = _orderGroupFactory.CreateLineItem(line.Code, wishlist);
        dest.Quantity = line.Quantity;
        CopyProperties(line, dest);
        wishlist.AddLineItem(dest, _orderGroupFactory);
    }

    shopping.GetFirstShipment()!.LineItems.Remove(line);
}

_orderRepository.Save(wishlist);

// Moving the last visible item out leaves the shopping cart empty (or only hidden event
// add-ons orphaned by their removed parent) - a full reset instead of a saved empty record,
// matching the same clean-slate behavior CartService gives the other removal paths.
var wishlistModel = _wishlistBuilder.Build(wishlist, customerId, memberId);
var ctx = new CartContext(customerId, memberId, market);
CartModel cartModel;
if (shopping.IsVisiblyEmpty())
{
    _orderRepository.Delete(shopping.OrderLink);
    cartModel = _cartBuilder.BuildEmpty(market);
}
// else: save and build normally
```

`MoveToCartAsync` mirrors this in the other direction, with a `cartKeepsItsOwn` guard so a line already in the cart is not overwritten by the saved copy.

## The two helpers

```csharp
private static List<ILineItem> FoldedAddOns(ICart cart, string eventVariantCode) =>
    cart.GetFirstShipment()?.LineItems
        .Where(li => !li.Code.Equals(eventVariantCode, StringComparison.OrdinalIgnoreCase)
                     && string.Equals(li.GetParentEventCode(), eventVariantCode, StringComparison.OrdinalIgnoreCase))
        .ToList() ?? [];

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

`GetParentEventCode()` and `IsEventRegistration()` are `LineItemExtensions` wrappers over the line's `Properties` bag - see the `commerce-metadata` skill for why those keys are prefixed and which survive order placement.

Copying the whole `Properties` bag is deliberate: it is the only thing carrying `ParentEventCode`, the registration answers, and the pricing flags.

## Model builder and pricing

```csharp
public WishlistModelBuilder(
    ICartCatalogResolver catalogResolver,
    // The product page's pricing lens (profile claims only). The unkeyed service is the
    // cart-aware one, which would price the wishlist off whatever is in the cart and leave the
    // two pages disagreeing about the same product.
    [FromKeyedServices(WisbarConstants.ServiceKeys.CatalogPricing)] IPriceService priceService,
    ICurrentMarket currentMarket,
    IRelationRepository relationRepository,
    IContentLoader contentLoader)
```

A saved list is a catalog view. Use the catalog pricing lens; the cart-aware one is for the basket.

```csharp
public record WishlistModel
{
    public int Count { get; init; }
    public IReadOnlyList<WishlistItemModel> Items { get; init; } = [];
}
```

`Count` is on the model so the header badge does not have to walk `Items`.

## Endpoints

```csharp
public static IEndpointRouteBuilder AddWishlistEndpoints(this IEndpointRouteBuilder builder)
{
    // The member policy, not the default one: the default policy runs over the CMS scheme, so a
    // signed-in editor would pass and get a wishlist keyed to a contact they don't own.
    var group = builder.MapGroup("/api/wishlist").RequireAuthorization(Authentication.MemberPolicy);

    group.MapGet("/",                        GetWishlist);
    group.MapPost("/items",                  AddItem);
    group.MapDelete("/items/{variantCode}",  RemoveItem);
    group.MapPost("/items/move-from-cart",   MoveFromCart);
    group.MapPost("/items/move-to-cart",     MoveToCart);

    return builder;
}
```

Handler shape - identity from `ICurrentIdentity`, `ArgumentException` to 400:

```csharp
private static async Task<IResult> AddItem(
    [FromBody] AddWishlistItemRequest request,
    ICurrentIdentity identity,
    IWishlistService service)
{
    var (customerId, memberId) = (identity.CustomerId, identity.MemberId);
    try
    {
        var wishlist = await service.AddItemAsync(customerId, memberId, request.VariantCode);
        return Results.Ok(new ResponseSingle<WishlistModel> { Result = wishlist });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new ResponseSingle<WishlistModel> { Errors = [ex.Message] });
    }
}
```

Move endpoints return both models:

```csharp
public record WishlistTransferResult
{
    public required WishlistModel Wishlist { get; init; }
    public required CartModel     Cart     { get; init; }
}
```

## Identity

```csharp
/// The key every cart, wishlist and order is stored under: the contact id for a member, otherwise
/// [the Commerce anonymous id]
Guid CustomerId { get; }
```

Always `ICurrentIdentity`. `User.Identity.IsAuthenticated` is true for a signed-in CMS editor, which is exactly the case the member policy exists to exclude.

The anonymous cart merge on sign-in (`AnonymousCartMergingMiddleware`) handles `CartNames.ShoppingCart` only. A named cart intended to survive an anonymous-to-member transition needs its own merge step.

## Adding a different named cart

1. Add the constant to `Globals.CartNames`.
2. Decide its lifetime: durable (like `WishList`) or transient and deleted after one operation (like `FreeRegistration`).
3. Write a service owning its rules, returning built models.
4. Choose the pricing lens - catalog for a saved list, cart-aware for a basket.
5. Carry folded add-ons in every operation that touches an event line.
6. Authorize on the member policy if it belongs to a person.
7. Decide the empty behavior - delete for a clean slate, or keep.
8. Test: idempotent add, add-on carrying, replace-not-merge, empty cleanup.
