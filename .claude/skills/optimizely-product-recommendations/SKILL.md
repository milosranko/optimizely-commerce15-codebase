---
name: optimizely-product-recommendations
description: Work on Optimizely Product Recommendations (Peerius) in an Optimizely Commerce Connect (CMS 12) site - the [CommerceTracking] filter and ViewData handoff, tracking data factory, catalog feed attributes, recommendation widgets configured in Site Settings, the resolver/builder that turns widget groups into product cards, the recommendation block and same-line carousel, mock codes for local development, and the client-side tracking partials for product, basket, checkout, order, search and wishlist. Use whenever asked to add or change a recommendations surface, debug empty or wrong recommendations, wire tracking on a new page, or extend the data sent to the recommendations engine.
---

# Working on Product Recommendations

Optimizely Product Recommendations (Peerius) works in two halves that are easy to confuse:

- **Tracking out** - the site tells the engine what the visitor is doing (viewed a product, changed a basket, checked out, ordered, searched).
- **Recommendations in** - the engine answers with widget groups, which this site turns into product cards.

Both halves ride on the same `[CommerceTracking]` request. Most bugs here come from one specific fact about *when* the answer arrives, covered in Step 2.

## Step 1: Orient

```bash
ls src/Wisbar.Web/Infrastructure/Recommendations/
ls src/Wisbar.Web/Views/Shared/PagePartials/ProductDetailPage/
grep -rn "CommerceTracking" --include=*.cs src/
```

| Part | Location |
|---|---|
| Resolver, builder, constants, card data | `Wisbar.Web/Infrastructure/Recommendations/` |
| Tracking data factory | `Wisbar.Optimizely.Core/Personalization/Tracking/WisbarTrackingDataFactory.cs` |
| Catalog feed attributes | `Wisbar.Business/Features/Personalization/WisbarEntryAttributeService.cs` |
| Block | `ContentTypes/Cms/Blocks/ProductRecommendationsBlock.cs` + `Web/Components/ProductRecommendationsBlockComponent.cs` |
| Same-line carousel | `Web/Components/SameLineRecommendationsViewComponent.cs` |
| Add-to-cart modal | `Web/Components/PurchaseViewComponent.cs` |
| Widget configuration | `SiteSettingsPage`, tab `Globals.Tabs.ProductRecommendations` |
| Client tracking partials | `Views/Shared/PagePartials/{Cart,Checkout,OrderConfirmation,Search,Wishlist,ProductDetailPage}/` |
| Tests | `Wisbar.Tests/Web/*Recommendation*`, `PurchaseViewComponentTests` |

## Step 2: The timing rule that explains most bugs

> **`[CommerceTracking]` writes the recommendation groups into `ViewData["epi_Recommendations"]` *after* the controller action body has run.**

So a controller action, or a view-model builder called from it, **cannot** see recommendations - it will always find an empty list. Recommendations must be resolved **during view rendering**, which is why every surface here is a view component:

- `SameLineRecommendationsViewComponent` - product page carousel
- `PurchaseViewComponent` - add-to-cart modal
- `ProductRecommendationsBlockComponent` - the editor-placed block

`ProductViewModel` documents this deliberately: the modal's recommendations are *not* a property on it, precisely because the builder runs too early.

If you are adding a new surface, it is a view component reading `ViewData[RecommendationConstants.ViewDataKey]`. Do not try to move that lookup into a controller.

## Step 3: The action must be tracked

A page only receives groups if its action carries the filter:

```csharp
[CommerceTracking(TrackingType.Product)]
public async Task<IActionResult> Index(BookProduct currentPage)
```

Product controllers (Book, Event, Meeting, Generic) and the start page use it. A new page that wants recommendations needs the attribute with the right `TrackingType`. No attribute, no groups, and the surface silently renders nothing.

## Step 4: Widgets are configured by editors, not in code

Site Settings, tab `Globals.Tabs.ProductRecommendations`:

| Setting | Purpose |
|---|---|
| `ProductRecommendationsWidgetKey` | The "Discover something else" product-page carousel. Empty hides it |
| `AddToCartRecommendationsWidgetKey` | "You may also like" in the add-to-cart modal. Empty hides it |
| `ProductRecommendationsMockProductCodes` | Dev/preview override - fixed catalog codes instead of live recommendations. **Ignored in Production** |

**Each surface needs its own widget.** If the modal reuses the carousel's widget it repeats the cards already on the page behind it - the setting's own description says so.

The `ProductRecommendationsBlock` is the exception: it carries its own `WidgetKey`, `MaxItems` (default 10) and `MockProductCodes` as block properties, so an editor can place several with different widgets.

Empty widget key is a valid configuration meaning "hide this surface" - never treat it as an error.

## Step 5: Fail open, but leave a trace

Every surface renders nothing when it resolves nothing. That is correct behavior - a recommendations outage must not break a product page.

The cost is that a missing block is invisible in the page source with nothing to diagnose. `ProductRecommendationsBlockComponent` handles this by logging through `RecommendationItemsBuilder.LogNothingResolved` with the surface name, widget key and groups when the result is empty, naming the block the way the editor sees it. **Keep that pattern on any new surface** - silent-and-unlogged is the failure mode that wastes the most time here.

## Step 6: Extending what the engine knows

**Tracking data** - `WisbarTrackingDataFactory` replaces the stock factory (same singleton lifetime, last registration wins). It exists because the stock one looks for a cart named `Default` owned by `CustomerContext.Current.CurrentContactId`, while this site keys carts by `Globals.CartNames` and `ICurrentIdentity.CustomerId`. Without the override, `CreateCartTrackingData` returns null, `[CommerceTracking]` treats null as "skip the pipeline", and the client tracking proxy answers 400. If basket or checkout tracking goes quiet, check this first.

**Catalog feed** - `WisbarEntryAttributeService` decorates `IEntryAttributeService` to add the variant's Format category *name* to the feed, because the stock feed ships the raw content id, which no merchandising rule can use. It is registered with `Decorate<>` rather than a plain registration because Optimizely's `DefaultEntryAttributeService` is internal. Attributes are produced for the product item *and* every nested variation, so the type check confining the attribute to variations matters. Add feed attributes here, following that shape.

## Step 7: Client-side tracking partials

One partial per surface, all defensive in the same way:

```js
if (typeof TrackingDataFactory === 'undefined' || typeof epiRecommendations === 'undefined') return;
```

Points worth preserving:

- **Basket tracking** fires on the `cart:updated` event and bails on an empty cart, because the proxy rebuilds the basket server-side and answers 400 when there is none.
- **Order tracking** guards against double counting with a `sessionStorage` key, since the confirmation link is re-enterable - and deliberately falls back to *tracking* if storage throws (Safari private mode), because double counting beats never counting.
- **Recommendation click-through** reads `recommendationId` from the query string and sets `SkipRecommendations = true`, so attributing a click does not also request a fresh set.

`TrackingOptions.TrackingEnabled` is set false in Development, so no local traffic reaches the engine.

## Step 8: Verify

```bash
dotnet build src/Wisbar.sln
dotnet test src/Wisbar.Tests/Wisbar.Tests.csproj
```

Locally there is no live feed, so use `ProductRecommendationsMockProductCodes` (or the block's own `MockProductCodes`) to exercise rendering. Report which of these you actually checked:

- Cards render from mock codes, with correct layout at each surface.
- An empty widget key hides the surface cleanly and logs a diagnosable line.
- The excluded code (the product being viewed) does not appear in its own carousel.
- New surfaces resolve during view rendering, not in a controller.
- Tracking partials no-op cleanly when the engine scripts are absent.

Say plainly that live recommendation quality and feed content were not verified against the real engine - that needs a deployed environment with tracking enabled.

## Checklist

- [ ] New surfaces are view components reading `ViewData[RecommendationConstants.ViewDataKey]`
- [ ] The page's action carries `[CommerceTracking]` with the right `TrackingType`
- [ ] Widget keys come from Site Settings (or block properties), never hardcoded
- [ ] Distinct widget per surface, so the modal does not repeat the carousel
- [ ] Fail open, and log when nothing resolves, naming the surface
- [ ] Current product excluded from its own recommendations
- [ ] Feed attribute changes go through `WisbarEntryAttributeService` and stay confined to the right entry type
- [ ] Tracking partials guard on the globals and avoid double counting
- [ ] Tests added or updated; suite green
- [ ] Report distinguishes what was mock-tested from what needs the live engine

Details and code: `references/architecture.md`.
