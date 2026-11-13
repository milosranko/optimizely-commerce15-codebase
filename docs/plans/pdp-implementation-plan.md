# Product Details Page (PDP) - Implementation Plan

## Context

The Optimizely Commerce Demo currently has a minimal Product Detail Page: `FashionProductController` returns a `ProductViewModel<FashionProduct>` and the Razor view (`Views/FashionProduct/Index.cshtml`) only displays the product name, code, and banner text. There is no variant selection, pricing, tabbed content, cart integration, related products, or SEO metadata rendering on product pages.

The goal is to build a comprehensive, extensible PDP that supports multiple product types (fashion, books, subscriptions), each with domain-specific tabbed content, variant-aware pricing (including member pricing via Optimizely Commerce), add-to-cart, related products, social sharing, SEO, and extension points for future reviews/wishlist.

**Key Decisions (confirmed with user):**
- Create new content types per product domain (BookProduct, SubscriptionProduct)
- Use Optimizely Commerce built-in `IPriceService` + `IPromotionEngine` for all pricing
- Server-rendered Razor views + vanilla JS enhancement (not React SPA)
- Core PDP features + extension point stubs for reviews/wishlist

---

## Component Breakdown (13 Components)

### Phase 1: Content Type Foundation (ContentTypes Layer)

#### Component 1: BookProduct & SubscriptionProduct Content Types
**Goal:** Create new product content types with domain-specific properties.

**Files:**
| Action | File |
|--------|------|
| CREATE | `ContentTypes/Commerce/Products/BookProduct.cs` |
| CREATE | `ContentTypes/Commerce/Products/SubscriptionProduct.cs` |
| MODIFY | `Core/Constants/Globals.cs` |

**Details:**
- `BookProduct` extends `ProductBase`: Description, LongDescription, Authors (XhtmlString), SummaryOfContents (XhtmlString), ISBN, Publisher, PublicationDate, Edition, PageCount, Format, BulkPricingInfo, LicenseAgreement, ProductTeaser
- `SubscriptionProduct` extends `ProductBase`: Description, LongDescription, SubscriptionTerms, RenewalInfo, AccessDetails, ProductTeaser
- Add to `Globals.MetaClassNames`: `Book_Product`, `Subscription_Product`, `Book_Variant`, `Subscription_Variant`
- Add to `Globals.GroupNames`: `Publications`
- Add to `Globals.Tabs`: `ProductDetails`
- Follow exact pattern of `FashionProduct.cs` (attributes, searchable, culture-specific)

---

#### Component 2: BookVariant, SubscriptionVariant & IProductDescription Interface
**Goal:** Create variant types and a shared interface for product description content.

**Files:**
| Action | File |
|--------|------|
| CREATE | `ContentTypes/Commerce/Variants/BookVariant.cs` |
| CREATE | `ContentTypes/Commerce/Variants/SubscriptionVariant.cs` |
| CREATE | `ContentTypes/Commerce/Interfaces/IProductDescription.cs` |

**Details:**
- `BookVariant` extends `VariantBase`: Format (Hardcover/Paperback/eBook/Audio), Description
- `SubscriptionVariant` extends `VariantBase`: Duration (Monthly/Annual), AccessLevel, Description
- `IProductDescription` interface: `XhtmlString Description { get; }`, `XhtmlString LongDescription { get; }` - implemented by FashionProduct, BookProduct, SubscriptionProduct
- Follow `FashionVariant.cs` pattern exactly

---

#### Component 3: ProductBase CustomTabs, PublicationsCategory & ProductTabBlock
**Goal:** Add editor-managed custom tabs to all products and create the publications category.

**Files:**
| Action | File |
|--------|------|
| MODIFY | `ContentTypes/Commerce/Products/Base/ProductBase.cs` |
| CREATE | `ContentTypes/Commerce/Categories/PublicationsCategory.cs` |
| CREATE | `ContentTypes/Commerce/Blocks/ProductTabBlock.cs` |
| CREATE | `ContentTypes/Commerce/Properties/ProductTabMetaDataProperty.cs` |

**Details:**
- Add `CustomTabs` property to `ProductBase` (IList<ProductTabBlock>) - uses PropertyList<> pattern from EventScheduleMetaDataProperty
- `ProductTabBlock` POCO: TabTitle, TabContent (XhtmlString), SortOrder
- `ProductTabMetaDataProperty` extends `PropertyList<ProductTabBlock>` with `[PropertyDefinitionTypePlugIn]`
- `PublicationsCategory` extends `CategoryBase`: accepts BookProduct, SubscriptionProduct, PublicationsCategory

---

### Phase 2: Business Logic (Business Layer)

#### Component 4: Product Pricing Service
**Goal:** Wrap Optimizely Commerce `IPriceService` + `IPromotionEngine` for PDP pricing.

**Files:**
| Action | File |
|--------|------|
| CREATE | `Business/Features/Products/Interfaces/IProductPricingService.cs` |
| CREATE | `Business/Features/Products/Services/ProductPricingService.cs` |
| CREATE | `Business/Features/Products/Models/VariantPriceModel.cs` |

**Details:**
- `GetVariantPrices(variants, marketId, currency)` - returns pricing for all variants
- `GetVariantPrice(variantCode, marketId, currency)` - single variant (for AJAX endpoint)
- Uses `IPriceService.GetCatalogEntryPrices()` with price filters for list/member prices
- Uses `IPromotionEngine.Evaluate()` for promotional discounts
- `VariantPriceModel`: Code, ListPrice, SalePrice?, MemberPrice?, DiscountedMemberPrice?, PromotionName?, IsAvailable
- Structured logging (INFO: price lookups, WARN: missing prices, ERROR: service failures)
- Graceful error handling - returns empty/default models rather than throwing

**Reuse:** `VariantsViewModel` already has matching price fields (PlacedPrice, DiscountedPrice, MemberPrice, DiscountedMemberPrice) - the service feeds these existing models.

---

#### Component 5: Product Detail Service
**Goal:** Orchestrating service that assembles complete PDP data: product, variants, pricing, tabs, related products.

**Files:**
| Action | File |
|--------|------|
| CREATE | `Business/Features/Products/Interfaces/IProductDetailService.cs` |
| CREATE | `Business/Features/Products/Services/ProductDetailService.cs` |
| CREATE | `Business/Features/Products/Models/ProductDetailModel.cs` |
| CREATE | `Business/Features/Products/Models/ProductTabModel.cs` |
| CREATE | `Business/Features/Products/Models/RelatedProductModel.cs` |

**Details:**
- `GetProductDetail(ProductBase product, string language)` - builds full PDP model
- Tab resolution via switch expression on product type:
  - Common: Description tab (via `IProductDescription`)
  - FashionProduct: Sizing tab
  - BookProduct: Authors, Summary of Contents, Details (ISBN etc.), License Agreement, Bulk Pricing
  - SubscriptionProduct: Terms, Renewal Info, Access Details
  - All types: CustomTabs from `ProductBase.CustomTabs`
- Loads variants via `IContentLoader.GetChildren<VariationContent>()`
- Gets related products via `IRelationRepository` (product associations)
- `ProductTabModel`: Key, DisplayName, HtmlContent (pre-rendered), SortOrder, IsActive
- `RelatedProductModel`: Name, Code, ImageUrl, Url, Price
- Depends on: Component 4 (IProductPricingService)

---

#### Component 6: Cart Service (Add-to-Cart)
**Goal:** Service for adding items to the Optimizely Commerce cart.

**Files:**
| Action | File |
|--------|------|
| CREATE | `Business/Features/Cart/Interfaces/ICartService.cs` |
| CREATE | `Business/Features/Cart/Services/CartService.cs` |
| CREATE | `Business/Features/Cart/Models/AddToCartResult.cs` |

**Details:**
- `AddToCart(variantCode, quantity, contactId)` - loads or creates cart, adds line item, validates, saves
- `CanAddToCart(variantCode)` - checks availability
- Uses `IOrderRepository`, `IOrderGroupFactory`, `ICurrentMarket`, `ILineItemValidator`
- Returns `AddToCartResult` (Success, ErrorMessage, CartItemCount)
- Auth gating is at the controller/endpoint level, not in the service
- Logging: INFO for add-to-cart, WARN for out-of-stock, ERROR for order repository failures
- Input validation: quantity > 0, non-empty variant code

---

### Phase 3: Web Layer (Controllers, API, ViewModels)

#### Component 7: Enhanced View Models
**Goal:** Extend ProductViewModel with tabs, social sharing, auth state; create AJAX response models.

**Files:**
| Action | File |
|--------|------|
| MODIFY | `Web/Models/Commerce/ProductViewModel.cs` |
| CREATE | `Web/Models/Commerce/ProductTabViewModel.cs` |
| CREATE | `Web/Models/Commerce/VariantSelectionResponse.cs` |
| CREATE | `Web/Models/Commerce/SocialShareViewModel.cs` |

**Details:**
- Add to `ProductViewModel<T>`: Tabs (IEnumerable<ProductTabViewModel>), SocialShare, DefaultVariantCode, IsAuthenticated, LoginUrl, ShowReviews (bool, default false), ShowWishlist (bool, default false)
- `ProductTabViewModel`: Key, DisplayName, HtmlContent, SortOrder, IsActive
- `VariantSelectionResponse`: Code, FormattedListPrice, FormattedSalePrice?, FormattedMemberPrice?, PromotionName?, IsAvailable, ImageUrl?
- `SocialShareViewModel`: PageUrl, PageTitle, Description?, ImageUrl?

---

#### Component 8: Product Controllers & Layout Pipeline Fix
**Goal:** Create controllers for new product types, refactor FashionProductController, fix PageContextActionFilter to populate Layout for product pages.

**Files:**
| Action | File |
|--------|------|
| MODIFY | `Web/Controllers/FashionProductController.cs` |
| CREATE | `Web/Controllers/BookProductController.cs` |
| CREATE | `Web/Controllers/SubscriptionProductController.cs` |
| MODIFY | `Web/Filters/PageContextActionFilter.cs` |
| MODIFY | `Web/Filters/PageViewContextFactory.cs` |

**Details:**
- **Critical infrastructure fix:** `PageContextActionFilter.OnResultExecuting` currently checks `is IPageViewModel<PageBase>` only. Products use `ProductBase` (not `PageBase`). Add a second check: `is IPageViewModel<ProductBase>` and call a new `CreateLayoutForProduct(ProductBase)` method.
- `PageViewContextFactory.CreateLayoutForProduct(ProductBase)` - builds `LayoutModel` from `ProductBase.SeoBlock` and `ProductBase.OpenGraphBlock` (different property paths than `PageBaseSeo`). Creates MetaData from SeoBlock.MetaDescription/MetaNoRobots, OpenGraph from OpenGraphBlock, Header, Footer.
- All product controllers use `IProductDetailService` to build view model, then return shared view `~/Views/Product/Index.cshtml`
- All controllers inject `IProductDetailService`, `ILogger<T>`
- FashionProductController refactored to use the same pattern

**Existing patterns reused:**
- `PageViewContextFactory` (line 24: `CreateLayout`) - follow same structure
- `CommerceControllerBase<T>` - all controllers inherit from this
- `ProductViewModel<T>` constructor pattern

---

#### Component 9: PDP API Endpoints (Variant Price + Add-to-Cart)
**Goal:** Minimal API endpoints for AJAX variant price lookup and add-to-cart.

**Files:**
| Action | File |
|--------|------|
| CREATE | `Web/Api/ProductEndpoints.cs` |
| MODIFY | `Web/Api/Endpoints.cs` |
| CREATE | `Web/Api/Requests/AddToCartRequest.cs` |

**Details:**
- `GET /api/products/{code}/variant-price` - public, returns VariantSelectionResponse
- `POST /api/products/add-to-cart` - requires auth, accepts AddToCartRequest (VariantCode, Quantity)
- Follow existing pattern in `Endpoints.cs` (MapGroup, ResponseSingle<T>, ResponseBase)
- Input validation: variant code non-empty, quantity >= 1
- Security: add-to-cart requires `.RequireAuthorization()`
- Error responses use existing `ResponseBase.Errors`
- Register via `endpoints.AddProductEndpoints()` in `Endpoints.cs`

---

#### Component 10: DI Registration
**Goal:** Wire up all new services in Startup.cs.

**Files:**
| Action | File |
|--------|------|
| MODIFY | `Web/Startup.cs` |

**Details:**
- Add to `#region Other Services`:
  - `AddScoped<IProductPricingService, ProductPricingService>()`
  - `AddScoped<IProductDetailService, ProductDetailService>()`
  - `AddScoped<ICartService, CartService>()`
- Add to `UseEndpoints`: `endpoints.AddProductEndpoints()`
- Minimal component - just DI wiring
- Depends on all service components being complete (4, 5, 6, 9)

---

### Phase 4: Views & Frontend

#### Component 11: Shared PDP Razor View & Product Layout Fix
**Goal:** Create the main PDP view that all product types share, and fix `_ProductLayout.cshtml` to render metadata/header/footer.

**Files:**
| Action | File |
|--------|------|
| CREATE | `Web/Views/Product/Index.cshtml` |
| MODIFY | `Web/Views/Shared/Layouts/_ProductLayout.cshtml` |
| CREATE | `Web/Views/Product/_VariantSelector.cshtml` |

**Details:**
- **Fix `_ProductLayout.cshtml`:** Currently bypasses all sections (just `@RenderBody()`). Needs to be upgraded like `_Layout.cshtml` - render Metadata, Header, Footer, Snippets sections from `Model.Layout`. Uses `IPageViewModel<ProductBase>` instead of `IPageViewModel<PageBasePublic>`.
- **`Views/Product/Index.cshtml`:** Main PDP view using `@model ProductViewModel<ProductBase>`:
  - Product header (name, brand)
  - Image gallery area (product images from CommerceMedia)
  - Variant selector partial (_VariantSelector)
  - Tabbed content sections (iterate Model.Tabs, ARIA-compliant tab markup)
  - Social sharing partial
  - Related products carousel
  - Extension point stubs (reviews/wishlist divs, hidden by default)
- **`_VariantSelector.cshtml`:** Variant dropdown/radio buttons, pricing display (list price, sale price, member price), quantity input, add-to-cart button with `data-*` attributes for JS, login prompt for anonymous users

**Existing patterns reused:**
- `_Layout.cshtml` section rendering pattern (Metadata, HeadSnippets, Header, Footer, etc.)
- `HtmlHelpers.EditableField()` for editable fields in edit mode

---

#### Component 12: Partial Views (Social Share, Related Products)
**Goal:** Create remaining partial views for the PDP.

**Files:**
| Action | File |
|--------|------|
| CREATE | `Web/Views/Product/_SocialShare.cshtml` |
| CREATE | `Web/Views/Product/_RelatedProducts.cshtml` |

**Details:**
- **`_SocialShare.cshtml`:** Share buttons for Facebook, Twitter/X, LinkedIn, Email. Uses `SocialShareViewModel` with properly encoded URLs. `target="_blank" rel="noopener noreferrer"` for security. No JavaScript dependencies - uses platform share URLs.
- **`_RelatedProducts.cshtml`:** Horizontal scrollable carousel of related products. Each item shows image (lazy loaded), name, price, link. Uses `IEnumerable<VariantsViewModel>` model (existing type).

---

#### Component 13: PDP JavaScript Enhancement
**Goal:** Vanilla JS for dynamic PDP interactions: tab switching, variant selection AJAX, add-to-cart, carousel.

**Files:**
| Action | File |
|--------|------|
| CREATE | `Web/wwwroot/js/pdp.js` |
| MODIFY | `Web/Views/Product/Index.cshtml` (add script reference) |

**Details:**
- IIFE module, vanilla JS (no framework dependencies)
- **Tab switching:** ARIA-compliant, toggles `hidden` attribute and `aria-selected`
- **Variant selection:** On change, `fetch('/api/products/{code}/variant-price')`, updates price display, availability, image
- **Add-to-cart:** Checks `data-authenticated` attribute; if false, shows login prompt/redirect. If true, `POST /api/products/add-to-cart` with CSRF token from meta tag
- **Related products carousel:** Horizontal scroll with prev/next buttons
- **Login prompt:** Modal or redirect to `data-login-url`
- Progressive enhancement: page works without JS (shows first tab, static pricing), JS enhances interaction

---

## Implementation Order & Dependencies

```
Phase 1: Content Types
  C1: BookProduct, SubscriptionProduct, Globals
  C2: BookVariant, SubscriptionVariant, IProductDescription (depends on C1)
  C3: ProductBase CustomTabs, PublicationsCategory, ProductTabBlock (depends on C1)

Phase 2: Business Logic (can start C4 in parallel with C3)
  C4: ProductPricingService
  C5: ProductDetailService (depends on C4)
  C6: CartService (parallel with C5)

Phase 3: Web Layer
  C7: Enhanced ViewModels (depends on C5 models)
  C8: Controllers + Layout Fix (depends on C5, C7)
  C9: API Endpoints (depends on C4, C6)
  C10: DI Registration (depends on C4, C5, C6, C9)

Phase 4: Views
  C11: Main PDP View + ProductLayout fix (depends on C7, C8)
  C12: Partial Views (depends on C11)
  C13: JavaScript Enhancement (depends on C9, C11)
```

---

## Verification Plan

After each component, run:
```bash
dotnet build src\Commerce.Demo.sln -c Release
dotnet test src\Commerce.Demo.Tests\Commerce.Demo.Tests.csproj
```

After all components complete:
1. Build succeeds with no errors
2. Run the application (`dotnet run` from Web project)
3. Navigate to a product page in the browser - verify tabs, pricing, layout renders
4. Open Optimizely admin - verify new content types appear (BookProduct, SubscriptionProduct, etc.)
5. Create a BookProduct with custom tabs in admin - verify they render
6. Test variant selection AJAX - price updates without page reload
7. Test add-to-cart for authenticated user - verify cart count updates
8. Test add-to-cart for anonymous user - verify login prompt appears
9. View page source - verify SEO metadata (title, meta description, OpenGraph tags) render
10. Test social share links open correct URLs

---

## Critical Files Reference

| File | Why It Matters |
|------|---------------|
| `ContentTypes/Commerce/Products/Base/ProductBase.cs` | Must add CustomTabs property; all products inherit from this |
| `ContentTypes/Commerce/Products/FashionProduct.cs` | Pattern to follow for new product types |
| `ContentTypes/Commerce/Properties/EventScheduleMetaDataProperty.cs` | Pattern for PropertyList<> custom properties |
| `Web/Filters/PageContextActionFilter.cs` | Must fix to populate Layout for product pages |
| `Web/Filters/PageViewContextFactory.cs` | Must add CreateLayoutForProduct() for product SEO/metadata |
| `Web/Models/Commerce/ProductViewModel.cs` | Must extend with tabs, auth, social sharing |
| `Web/Models/Commerce/VariantsViewModel.cs` | Existing pricing fields to reuse |
| `Web/Controllers/FashionProductController.cs` | Must refactor to use ProductDetailService |
| `Web/Views/Shared/Layouts/_ProductLayout.cshtml` | Must fix to render header/footer/metadata |
| `Web/Views/Shared/Layouts/_Layout.cshtml` | Reference pattern for product layout fix |
| `Web/Api/Endpoints.cs` | Must register product endpoints |
| `Web/Startup.cs` | Must register new services in DI |
| `Core/Constants/Globals.cs` | Must add new MetaClassNames, GroupNames, Tabs |
