# Product Recommendations architecture reference

Companion to `SKILL.md`. Verify details against the code before relying on them.

## Request lifecycle

```
Request
  │
  ├─ Controller action runs          ← ViewData["epi_Recommendations"] NOT yet populated
  │    (view-model builders run here - they cannot see recommendations)
  │
  ├─ [CommerceTracking] filter        ← tracks the event, receives widget groups,
  │                                     writes them into ViewData
  │
  └─ View renders                     ← view components read ViewData and resolve cards
       ├─ SameLineRecommendationsViewComponent  (product page carousel)
       ├─ PurchaseViewComponent                 (add-to-cart modal)
       └─ ProductRecommendationsBlockComponent  (editor-placed block)
```

The single most important consequence:

```csharp
/// Same-line "Discover something else" carousel. Runs during view rendering, so the
/// [CommerceTracking] filter has already written the recommendation groups into ViewData -
/// unlike the controller action body, which executes before the filter populates them.
```

And from `ProductViewModel`:

```csharp
/// <remarks>
/// The modal's recommendations are deliberately not a property here: the [CommerceTracking] filter
/// writes the widget groups into ViewData after the action, so PurchaseViewComponent resolves them.
/// </remarks>
```

## The ViewData key

```csharp
public static class RecommendationConstants
{
    /// ViewData key under which the [CommerceTracking] result processor stores the
    /// recommendation groups. Read by the recommendation block and same-line carousel.
    public const string ViewDataKey = "epi_Recommendations";
}
```

Standard read in a component:

```csharp
var groups = ViewData[RecommendationConstants.ViewDataKey] as IEnumerable<RecommendationGroup> ?? [];
```

Always null-coalesce to an empty collection - an untracked action leaves the key absent.

## Surfaces

```csharp
/// Which surface is asking, and so which Site Settings widget answers and how many cards it wants.
/// Each has its own widget so the modal recommends something other than the carousel behind it.
public enum RecommendationSurface
{
    ProductPageCarousel,   // "Discover something else" on the product page
    AddToCartModal         // "You may also like" in the add-to-cart modal
}
```

## Resolver and builder

Two layers, deliberately split:

```csharp
// ProductRecommendationsResolver - knows about surfaces and Site Settings.
// "Resolves a Site Settings recommendation widget into product cards. Shared by the product-page
//  carousel and the add-to-cart modal so the widget lookup, the mock-code override and the
//  'nothing configured' rule live in one place. Groups stay a parameter: they come from ViewData,
//  which only the caller can reach."
Task<IReadOnlyList<RecommendationItemViewModel>> ResolveAsync(
    RecommendationSurface surface,
    IEnumerable<RecommendationGroup> groups,
    string? excludeCode = null);

// RecommendationItemsBuilder - knows nothing about surfaces; takes an explicit widget key.
// Used directly by the block, which carries its own widget key as a block property.
Task<IReadOnlyList<RecommendationItemViewModel>> BuildAsync(
    IEnumerable<RecommendationGroup> groups,
    string? widgetKey,
    int maxItems,
    string? mockProductCodes = null,
    string? excludeCode = null);
```

Use the **resolver** for a surface configured in Site Settings; use the **builder** when the caller owns its widget key. Cards are resolved through `IProductSearchService` with `ICacheService` in front.

Test seam worth knowing:

```csharp
// ReferenceConverter is sealed and unmockable, so the live path resolves codes through an
// injectable delegate that defaults to the converter under production DI.
internal RecommendationItemsBuilder(
    ICacheService cacheService, IProductSearchService searchService, Func<ContentReference, string> resolveCode)
```

## Registration

```csharp
// Startup.ConfigureServices
.AddScoped<IRecommendationItemsBuilder, RecommendationItemsBuilder>()
.AddScoped<IProductRecommendationsResolver, ProductRecommendationsResolver>()

// Replaces the personalization tracking factory registered by EPiServer.Tracking.Commerce
// (same Singleton lifetime, last registration wins).
services.AddSingleton<TrackingDataFactory, WisbarTrackingDataFactory>();

// Adds the variant's Format category name to the Product Recommendations catalog feed.
// Decorate (not a plain registration) because Optimizely's DefaultEntryAttributeService is
// internal - AddCommerce() has already registered it, and Decorate keeps its lifetime.
services.Decorate<IEntryAttributeService, WisbarEntryAttributeService>();

// Development only
.Configure<TrackingOptions>(o => { o.TrackingEnabled = false; });
```

## Site Settings

Tab `Globals.Tabs.ProductRecommendations` on `SiteSettingsPage`:

| Property | Order | Notes |
|---|---|---|
| `ProductRecommendationsWidgetKey` | 300 | Product-page carousel. Empty hides it |
| `AddToCartRecommendationsWidgetKey` | 305 | Add-to-cart modal. **Use a different widget from the carousel**, or the modal repeats the cards behind it |
| `ProductRecommendationsMockProductCodes` | 310 | Comma- or newline-separated catalog codes, for dev/preview. Overrides both widgets. **Ignored in Production** |

Block properties on `ProductRecommendationsBlock`: `WidgetKey`, `MaxItems` (default 10), `MockProductCodes`.

## Block component - fail open, but log

```csharp
protected override async Task<IViewComponentResult> InvokeComponentAsync(ProductRecommendationsBlock block)
{
    var model = new ProductRecommendationsBlockViewModel(block);
    var groups = ViewData[RecommendationConstants.ViewDataKey] as IEnumerable<RecommendationGroup> ?? [];

    model.RecommendationItems = await _itemsBuilder.BuildAsync(
        groups, block.WidgetKey, block.MaxItems, block.MockProductCodes);

    // The view drops the whole section when this is empty, so without a line here an editor's
    // block simply is not in the page source and there is nothing left to diagnose from.
    // The heading names it the way the editor sees it in the content area.
    if (model.RecommendationItems.Count == 0)
    {
        var surface = string.IsNullOrWhiteSpace(block.Heading)
            ? "Product Recommendations block"
            : $"Product Recommendations block \"{block.Heading}\"";

        RecommendationItemsBuilder.LogNothingResolved(_logger, surface, block.WidgetKey, groups);
    }

    return View(model);
}
```

Copy this shape for any new surface: render nothing, log once, name the surface as the editor sees it.

## Tracking data factory

```csharp
/// Points Optimizely's basket, checkout and wishlist tracking at the carts this site actually uses.
/// <remarks>
/// The stock factory looks for Cart.DefaultName ("Default") owned by
/// CustomerContext.Current.CurrentContactId. Wisbar stores carts under Globals.CartNames keyed by
/// ICurrentIdentity.CustomerId - the contact id for a signed-in member, the Commerce anonymous id
/// otherwise - so the stock lookup never finds the cart, and basket/checkout tracking is built from
/// nothing: CreateCartTrackingData returns null, which the [CommerceTracking] action filter treats as
/// "skip the rest of the pipeline", and the client-side tracking proxy answers 400. Overriding the
/// two lookup seams is what makes the tracking request carry the visitor's actual line items.
/// </remarks>
public class WisbarTrackingDataFactory : TrackingDataFactory
```

**Diagnostic:** basket/checkout tracking silent, or the tracking proxy returning 400, points here.

## Catalog feed attributes

```csharp
/// Adds the variant's Format category name to the Optimizely Product Recommendations catalog feed.
/// <remarks>
/// Format is a ContentReference, which Optimizely maps to a ShortString metafield, so the stock feed
/// already ships it - as the bare content id, which no merchandising rule can use. This decorator
/// adds a second attribute carrying the category's operational Name ("OnDemand") and leaves the raw
/// one alone.
/// <para>Attributes are produced for the product-level item and for every nested variation element,
/// so the type check below is what confines the new attribute to variations, where Format lives.</para>
/// </remarks>
public sealed class WisbarEntryAttributeService : IEntryAttributeService
{
    public IDictionary<string, string> GetAttributes(EntryContentBase entryContent, string scope)
    {
        var attributes = _inner.GetAttributes(entryContent, scope);
        var formatName = ResolveFormatName(entryContent);
        if (string.IsNullOrWhiteSpace(formatName)) return attributes;
        // ... add, then return
    }
}
```

Adding a feed attribute: call the inner service, add on top, never replace what it returned, and confine the addition to the entry type the data actually lives on.

## Client-side tracking partials

| Partial | Fires on |
|---|---|
| `ProductDetailPage/_RecommendationTracking.cshtml` | Click-through attribution from `?recommendationId=` |
| `Cart/_CartTracking.cshtml` | `cart:updated` event |
| `Checkout/_CheckoutTracking.cshtml` | Checkout page |
| `OrderConfirmation/_OrderTracking.cshtml` | Order confirmation, once per order |
| `Search/_SearchResultTracking.cshtml` | Search results |
| `Wishlist/_WishlistTracking.cshtml` | Wishlist |

All guard first:

```js
if (typeof TrackingDataFactory === 'undefined' || typeof epiRecommendations === 'undefined') return;
```

### Click-through attribution

```js
var recId = parseInt('@Context.Request.Query["recommendationId"]', 10);
if (isNaN(recId) || recId <= 0) return;
var data = TrackingDataFactory.createProductTrackingData(@Json.Serialize(Model.Code), @Json.Serialize(Model.DisplayName ?? Model.Name));
data.SkipRecommendations = true;   // attributing a click must not also request a fresh set
data.recommendationId = recId;
epiRecommendations.track(data);
```

### Basket

```js
window.addEventListener("cart:updated", function (e) {
    // The payload carries no line items - the tracking proxy rebuilds the basket from the cart on
    // the server, and answers 400 when there is none. An empty cart has nothing to report.
    var cart = (e.detail || {}).cart;
    if (!cart || !(cart.items || []).length) return;
    epiRecommendations.track(TrackingDataFactory.createBasketTrackingData());
});
```

### Order, guarded against re-entry

```js
// The confirmation link is re-enterable, so a reload would re-send the conversion. A storage
// failure (Safari private mode) leaves tracked false - double counting beats never counting.
var tracked = false;
try {
    tracked = sessionStorage.getItem("wisbar:order-tracked:" + orderId) === "1";
    sessionStorage.setItem("wisbar:order-tracked:" + orderId, "1");
} catch (e) {
    tracked = false;
}
if (tracked) return;
```

## Tracked actions

```csharp
[CommerceTracking(TrackingType.Product)]
public async Task<IActionResult> Index(BookProduct currentPage)
```

Present on `BookProductController`, `EventProductController`, `MeetingProductController`, `GenericProductController`, and `StartPageController`. A page without it receives no groups.

## Adding a new surface

1. Decide the widget: a new Site Settings property (shared surface) or a block property (editor-placed).
2. Write a **view component** - never a controller path - reading `ViewData[RecommendationConstants.ViewDataKey]`.
3. Call the resolver (Site Settings widget) or the builder (own widget key), passing `excludeCode` for the product being viewed.
4. Render nothing when empty, and log through `LogNothingResolved` with the surface name.
5. Confirm the hosting page's action carries `[CommerceTracking]`.
6. Test with mock codes, plus a test for the empty-groups path.

## Tests

`Wisbar.Tests/Web/`: `ProductRecommendationsResolverTests`, `RecommendationItemsBuilderTests`, `RecommendationCardDataTests`, `SameLineRecommendationsViewComponentTests`, `PurchaseViewComponentTests`.

Highest-value cases: empty groups renders nothing; empty widget key hides the surface; mock codes override live groups; `excludeCode` removes the current product; `maxItems` is honoured.
