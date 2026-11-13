# Pro EPiServer Commerce — Reference Notes

> **Source:** Pro EPiServer Commerce (Apress, c. 2018), targeting Episerver Commerce 9–12 (pre-Optimizely rebrand).
>
> **Status:** Historical reference for an **Optimizely Commerce 14 / .NET 8** project (Commerce.Demo.Optimizely). Many APIs referenced here have been renamed, restructured, or removed in current Optimizely Commerce. Treat all namespace and class names as historical reference — verify against the current Optimizely Commerce 14 SDK before using. For current/modern guidance, prefer [pro-optimizely-commerce-cloud.md](pro-optimizely-commerce-cloud.md). See "Migration Notes for Current Optimizely 14 Codebases" at the end of this document.
>
> The text was extracted from `proepiservercommerce.pdf` (15,452 lines of plain text) and condensed into this reference. The original ~570-page book covers far more detail; this file captures the architecture, type names, and patterns most useful for understanding inherited or legacy Commerce code.

---

## Table of Contents

**Part 1 — Catalog, Pricing, Inventory, Orders, Events**
- [Chapter 0 — A Brief History](#chapter-0--a-brief-history)
- [Chapter 0.5 — Framework Overview](#chapter-05--framework-overview)
- [Chapter 1 — How the Catalog Is Structured](#chapter-1--how-the-catalog-is-structured)
- [Chapter 2 — Catalog Content Modeling](#chapter-2--catalog-content-modeling)
- [Chapter 3 — Associations, Relations, and Assets](#chapter-3--associations-relations-and-assets)
- [Chapter 3.1 — Markets](#chapter-31--markets)
- [Chapter 3.2 — Application-Level Settings](#chapter-32--application-level-settings)
- [Chapter 4 — Prices](#chapter-4--prices)
- [Chapter 5 — Inventories](#chapter-5--inventories)
- [Chapter 6 — The Order System (Legacy)](#chapter-6--the-order-system-legacy)
- [Chapter 7 — New Order System](#chapter-7--new-order-system)
- [Chapter 8 — Taxes](#chapter-8--taxes)
- [Chapter 9 — Content Events](#chapter-9--content-events)
- [Chapter 9 (cont.) — CatalogEventBroadcaster](#chapter-9-cont--catalogeventbroadcaster)
- [Chapter 10 — Inventories, Prices, and Orders Events](#chapter-10--inventories-prices-and-orders-events)

**Part 2 — Search, Promotions, Business Foundation, Extensions**
- [Chapter 11 — Search](#chapter-11--search)
- [Chapter 12 — The Old Promotion System](#chapter-12--the-old-promotion-system)
- [Chapter 13 — The New Promotion System](#chapter-13--the-new-promotion-system)
- [Chapter 14 — Business Foundation Overview](#chapter-14--business-foundation-overview)
- [Extending the System — Framework Way](#extending-the-system--framework-way)
- [The Unusual Ways (Workflows, Commerce Manager, Payment Providers)](#the-unusual-ways-workflows-commerce-manager-payment-providers)
- [ProfileModule and CustomerContact Events](#profilemodule-and-customercontact-events)
- [Migration Steps](#migration-steps)
- [Caching](#caching)
- [Routing](#routing)
- [Appendix — Performance and Upgrade Guidance](#appendix--performance-and-upgrade-guidance)

**Bridge to Modern Optimizely**
- [Migration Notes for Current Optimizely 14 Codebases](#migration-notes-for-current-optimizely-14-codebases)

---

## Chapter 0 — A Brief History

Episerver Commerce originated from the acquisition of **Mediachase eCF** in April 2012. Prior to that, Commerce
was integrated as a white-brand product with Commerce Manager as a separate back-end. The first major
post-acquisition release — **Commerce 7.5** (shipped with Episerver 7.5) — introduced `CatalogContentProvider`,
unifying catalog and CMS content under `IContentRepository`. Commerce 8 focused on cloud compatibility
(Azure/AWS) and removed Common Framework dependencies. **Commerce 9** re-architected catalog versioning
storage (replacing DDS with native eCF tables), delivering 2–10× performance gains and removing the old
DDS bottleneck. After 7.5, Episerver moved to a continuous NuGet release cycle with semantic versioning.

**Key NuGet packages (historical):**

- `EPiServer.Commerce` — main front-end package
- `EPiServer.Commerce.Core` — catalog, promotion, pricing core (no UI)
- `EPiServer.Commerce.UI` — Catalog UI, Marketing UI gadgets
- `EPiServer.CommerceManager` — Commerce Manager back-end
- `EPiServer.Find.Commerce` — Find integration (optional)
- `EPiServer.ServiceApi.Commerce` — REST catalog API (optional)

---

## Chapter 0.5 — Framework Overview

Episerver Commerce is built on top of Episerver Framework and CMS. The two pillars that enable its
pluggable architecture are **dependency injection** (historically StructureMap, wrapped by `ServiceLocator`)
and **initialization modules** (`IInitializationModule` / `IConfigurableModule`).

The "last registration wins" rule means custom implementations registered after the default ones take
precedence. `ModuleDependency` controls module ordering.

**Key namespaces / types:**

- `EPiServer.ServiceLocation.ServiceLocator` — resolves registered services
- `EPiServer.Framework.Initialization.IInitializationModule` — runs at startup
- `EPiServer.Framework.Initialization.IConfigurableModule` — DI registration at startup
- `EPiServer.Framework.Initialization.InitializationEngine` — passed to module lifecycle methods
- `EPiServer.ServiceLocation.ServiceConfigurationAttribute` — attribute-based service registration

```csharp
// Registering a custom IPriceService implementation via IConfigurableModule
[InitializableModule]
[ModuleDependency(typeof(EPiServer.Commerce.Initialization.CommerceInitialization))]
public class CustomInitialization : IConfigurableModule
{
    public void ConfigureContainer(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IPriceService>(
            locator => new MyCustomPriceService());
    }
    public void Initialize(InitializationEngine context) { }
    public void Uninitialize(InitializationEngine context) { }
}
```

---

## Chapter 1 — How the Catalog Is Structured

The catalog is a tree: **Catalog → Nodes (categories) → Entries**. A node can be linked to multiple parent
nodes (via `CatalogNodeRelation`); entries can belong to multiple nodes (via `NodeEntryRelation`). Commerce 11
added `IsPrimary` to node-entry relations, fixing sort-order drag-and-drop.

**Entry types:**

| Type | Sellable | Own prices/inventory |
|------|----------|----------------------|
| Product | No (placeholder) | No |
| Variation / SKU | Yes | Yes |
| Package | Yes | Yes (fixed bundle) |
| Bundle | No own price | No (items sold individually) |

**Entry-to-entry relation types:** `ProductVariation`, `PackageEntry`, `BundleEntry` (stored in `CatalogEntryRelation`).
Cross-sell / up-sell links are **associations** (stored in `CatalogEntryAssociation`).

The **two systems**: The legacy `ICatalogSystem` API works with DataSet DTOs; the preferred
`IContentRepository` / `IContentLoader` API works with POCO content objects. Prefer the content API
for new code — it supports versioning, has better caching, and aligns with CMS patterns.

**`ReferenceConverter`** bridges integer IDs and `ContentReference`. It encodes `CatalogContentType` in
the two most-significant bits of the content ID. Commerce 9 made `WorkId` unique system-wide.

**Key namespaces / types:**

- `Mediachase.Commerce.Catalog.ICatalogSystem` — legacy DTO-based catalog API
- `EPiServer.IContentRepository` / `EPiServer.IContentLoader` — preferred content API
- `EPiServer.Commerce.Catalog.ReferenceConverter` — ID ↔ `ContentReference` bridge
- `EPiServer.Commerce.Catalog.ContentTypes.CatalogContentType` — catalog, node, entry
- `EPiServer.Core.ContentReference` — universal content identity

```csharp
// Convert an entry code to a ContentReference (Commerce 10+)
var contentLink = _referenceConverter.GetContentLink(entryCode,
    CatalogContentType.CatalogEntry, 0);
```

---

## Chapter 2 — Catalog Content Modeling

The **MetaData system** (`Mediachase.MetaDataPlus`) provides the extensible schema layer. A `MetaClass`
is a template (e.g., "FashionProduct"); a `MetaField` is one attribute; a `MetaObject` is an instance.
In Commerce 9, property storage moved from per-metaclass tables to a single `CatalogContentProperty`
table (one row per property per content per language), matching the CMS `tblContentProperty` pattern.

Strongly typed content types inherit from the base classes below and are decorated with
`[CatalogContentType(GUID = "...", MetaClassName = "...")]`. Properties must be `virtual`, must match
the metafield name (case-insensitive), and must use a consistent type across all content types that
share the same metafield.

**Dictionary types** (`DictionarySingleValue`, `DictionaryMultiValue`) require `[BackingType]`:

```csharp
[BackingType(typeof(PropertyDictionarySingle))]
public virtual string Color { get; set; }

[BackingType(typeof(PropertyDictionaryMultiple))]
public virtual IEnumerable<string> Colors { get; set; }
```

`CatalogContentScannerExtension` auto-syncs content types to metaclasses on site startup.

**Key namespaces / types:**

- `Mediachase.MetaDataPlus.Configurator.MetaClass` — metaclass definition
- `Mediachase.MetaDataPlus.Configurator.MetaField` — metafield definition
- `Mediachase.MetaDataPlus.MetaDataType` — enum of supported field types
- `EPiServer.Commerce.Catalog.ContentTypes.CatalogContentBase` — root base class
- `EPiServer.Commerce.Catalog.ContentTypes.NodeContent` — category/node
- `EPiServer.Commerce.Catalog.ContentTypes.ProductContent` — product
- `EPiServer.Commerce.Catalog.ContentTypes.VariationContent` — SKU / variant
- `EPiServer.Commerce.Catalog.ContentTypes.PackageContent` — package
- `EPiServer.Commerce.Catalog.ContentTypes.BundleContent` — bundle
- `EPiServer.Commerce.Catalog.ContentTypes.EntryContentBase` — base for all entries
- `EPiServer.Commerce.Catalog.DataAnnotations.CatalogContentTypeAttribute` — maps class to metaclass
- `EPiServer.Commerce.Catalog.DataAnnotations.BackingTypeAttribute` — specifies PropertyData backing type
- `EPiServer.Commerce.SpecializedProperties.CommerceImage` — base for catalog image assets
- `EPiServer.Commerce.SpecializedProperties.CommerceMediaCollection` — asset list on entries/nodes

---

## Chapter 3 — Associations, Relations, and Assets

Relations and associations are managed by `IRelationRepository`, `IAssociationRepository` (and the
now-removed `ILinksRepository` from Commerce 10 and earlier). Commerce 11 replaced `Source`/`Target`
naming with `Parent`/`Child` and introduced `NodeEntryRelation.IsPrimary`.

**Association groups** (CrossSell, Upsell, etc.) are registered via:

```csharp
var repo = context.Locate.Advanced
    .GetInstance<GroupDefinitionRepository<AssociationGroupDefinition>>();
repo.Add(new AssociationGroupDefinition { Name = "CrossSell" });
```

**Assets** are `CommerceMedia` objects in a `CommerceMediaCollection` on `IAssetContainer`
(implemented by `EntryContentBase` and `NodeContent`). Each `CommerceMedia` has `AssetLink`
(a `ContentReference` to a CMS media item), `AssetType`, `GroupName`, and `SortOrder`.
Image types should inherit `CommerceImage` to provide the `LargeThumbnail` (256×256) blob
used as the Catalog UI content header.

**Versioning** improved dramatically in Commerce 9: `WorkId` became globally unique; draft versions
are stored in `ecfVersionProperty` (same schema as `CatalogContentProperty`). Key settings:
`DisableVersionSync` (keep only the published version) and `UIMaxVersions` (default 20).

**Key namespaces / types:**

- `EPiServer.Commerce.Catalog.Linking.IRelationRepository` — node/entry relations
- `EPiServer.Commerce.Catalog.Linking.IAssociationRepository` — entry associations
- `EPiServer.Commerce.Catalog.Linking.ProductVariation` — product→variant relation
- `EPiServer.Commerce.Catalog.Linking.PackageEntry` — package→entry relation
- `EPiServer.Commerce.Catalog.Linking.BundleEntry` — bundle→entry relation
- `EPiServer.Commerce.Catalog.Linking.NodeRelation` — node→node / node→entry relation
- `EPiServer.Commerce.Catalog.Linking.NodeEntryRelation` — node-entry with IsPrimary (Commerce 11.2+)
- `EPiServer.Commerce.SpecializedProperties.IAssetContainer` — interface on entries/nodes for assets
- `EPiServer.Commerce.SpecializedProperties.CommerceMedia` — single asset descriptor
- `EPiServer.Core.IContentVersionRepository` — query content versions
- `Mediachase.Commerce.Catalog.ImportExport.CatalogImportExport` — catalog XML import/export
- `Mediachase.Commerce.Catalog.ImportExport.ImportJob` — async import wrapper

---

## Chapter 3.1 — Markets

A **Market** is the unit of geographic/commercial segmentation. Each order, price, and shipment method
is tied to a market. Markets were fully introduced in Commerce 7.5. The special `MarketId.Default`
("DEFAULT") is the fallback when multi-market is not required.

`ICurrentMarket.GetCurrentMarket()` determines the active market per request; the default
implementations (QuickSilver `CurrentMarket`, Commerce Sample `MarketStorage`) use cookies.
A market has `DefaultLanguage`, `DefaultCurrency`, `Languages`, `Currencies`, and `Countries`.
Commerce 12 added `PricesIncludeTax` to `IMarket`.

**Key namespaces / types:**

- `Mediachase.Commerce.IMarket` — market contract
- `Mediachase.Commerce.MarketId` — struct identifying a market ("US", "EUROPE", etc.)
- `Mediachase.Commerce.IMarketService` — CRUD for markets
- `Mediachase.Commerce.ICurrentMarket` — resolves current market per request
- `Mediachase.Commerce.Currency` — currency value type

```csharp
public interface ICurrentMarket
{
    IMarket GetCurrentMarket();
    void SetCurrentMarket(MarketId marketId);
}
```

---

## Chapter 3.2 — Application-Level Settings

Three objects hold site-wide Commerce settings (a historical artifact from the Mediachase era):

- `Mediachase.Commerce.Core.SiteContext.Current` — `CurrentCurrency` (cookie-based per request)
- `Mediachase.Commerce.AppContext.Current` — `ApplicationId` (Guid, obsoleted in Commerce 11), `AzureCompatible` (obsoleted), `IsEncryptionEnabled`
- `EPiServer.Commerce.Core.IApplicationContext` — `HasContentModelTypes`, `DisableVersionSync`

In Commerce 11+ `ApplicationId` was removed; `CatalogKey` uses only the entry code.

---

## Chapter 4 — Prices

Prices are **decoupled from entries** (since Commerce R3). A price (`IPriceValue`) is keyed by
`CatalogKey` (entry code + application ID, or entry code alone in Commerce 11+), `MarketId`,
`CustomerPricing` (AllCustomers / UserName / PriceGroup), `Currency`, `ValidFrom`/`ValidUntil`,
and `MinQuantity`.

**Two price interfaces:**

- `IPriceService` — optimised for read (returns lowest applicable price; auto-deduplicates overlapping
  ranges on save). Use for displaying prices to customers.
- `IPriceDetailService` — optimised for write (saves as-is, individual `PriceValueId`). Use for
  editing prices. The two services stay in sync via `ReplicatePriceDetailChanges` / `ReplicatePriceServiceChanges`.

**Key namespaces / types:**

- `Mediachase.Commerce.Pricing.IPriceValue` — price value read interface
- `Mediachase.Commerce.Pricing.IPriceService` — batch price query/save
- `Mediachase.Commerce.Pricing.IPriceDetailValue` — price value with `PriceValueId`
- `Mediachase.Commerce.Pricing.IPriceDetailService` — individual price CRUD
- `Mediachase.Commerce.Pricing.PriceValue` — mutable implementation of `IPriceValue`
- `Mediachase.Commerce.Pricing.PriceDetailValue` — mutable implementation of `IPriceDetailValue`
- `Mediachase.Commerce.Pricing.CustomerPricing` — price segmentation (AllCustomers / UserName / PriceGroup)
- `Mediachase.Commerce.Pricing.PriceFilter` — filter by currency, customer pricing, quantity
- `Mediachase.Commerce.CatalogKey` — entry + application ID key (application ID obsoleted in Commerce 11)
- `Mediachase.Commerce.Money` — amount + currency value type

```csharp
// Load the default price for a variant in the current market
var price = _priceService.GetDefaultPrice(
    currentMarket.MarketId,
    DateTime.UtcNow,
    new CatalogKey(entryContent.Code),
    currentMarket.DefaultCurrency);
```

---

## Chapter 5 — Inventories

Inventories are decoupled from entries and designed to be replaced by external systems (ERP).
There are two API generations:

- **`IWarehouseInventoryService`** (legacy, obsoleted in Commerce 9.24) — uses `IWarehouseInventory`
  with `InStockQuantity`, `ReservedQuantity`, `AllowBackorder`, etc. Values are **set** directly
  (race-condition risk under concurrent load).
- **`IInventoryService`** (preferred since Commerce 8.7) — uses `InventoryRecord` with
  `PurchaseAvailableQuantity`, `PreorderAvailableQuantity`, `BackorderAvailableQuantity`, and
  separate `Requested` quantities. Changes are made via **requests** (`InventoryRequest` /
  `InventoryRequestItem`), which the database applies atomically (no race conditions).

The request/response cycle:

1. On cart checkout — create `InventoryRequestItem` with `RequestType = Purchase | Preorder | Backorder`
2. Send via `IInventoryService.Request(request)` → receive `InventoryResponse`
3. Store returned `OperationKey` on `Shipment.OperationKeysMap`
4. On shipment complete → send `Complete` request; on cancel → send `Cancel` request using stored keys
5. Call `shipment.ClearOperationKeys()` after final request

Commerce 10.7 dramatically improved inventory caching. `QueryByEntry` is the most cache-friendly
and commonly used read method.

**Key namespaces / types:**

- `Mediachase.Commerce.Inventory.IWarehouseRepository` — warehouse CRUD
- `Mediachase.Commerce.Inventory.IWarehouse` — warehouse contract
- `Mediachase.Commerce.Inventory.IWarehouseInventoryService` — legacy inventory API
- `Mediachase.Commerce.Inventory.IWarehouseInventory` — legacy inventory record
- `Mediachase.Commerce.Inventory.IInventoryService` — current inventory API
- `Mediachase.Commerce.Inventory.InventoryRecord` — current inventory data class
- `Mediachase.Commerce.Inventory.InventoryRequest` — request wrapper
- `Mediachase.Commerce.Inventory.InventoryRequestItem` — per-line-item request
- `Mediachase.Commerce.Inventory.InventoryRequestType` — Purchase / Preorder / Backorder / Complete / Cancel / Split
- `Mediachase.Commerce.Inventory.InventoryResponse` — result from `IInventoryService.Request`
- `Mediachase.Commerce.Inventory.OperationKeySerializer` — serializes/deserializes `OperationKey` strings

---

## Chapter 6 — The Order System (Legacy)

The legacy order system is based on the MetaDataPlus system. `OrderGroup` is the abstract base for
`Cart`, `PaymentPlan`, and `PurchaseOrder`. Each `OrderGroup` has:

- `OrderFormCollection` — typically one `OrderForm` per group (multiple for B2B)
- `OrderAddressCollection` — shipping + billing addresses
- Each `OrderForm` owns `LineItemCollection`, `ShipmentCollection`, `PaymentCollection`
- `Shipment.LineItemIndexQuantityMap` — fragile index-based mapping (use `PurchaseOrderManager` methods to mutate)

Workflow names: `CartValidate` → `CartPrepare` → `CartCheckout`. Run via
`OrderGroupWorkflowManager.RunWorkflow(cart, workflowName)`.

Order status progression: `InProgress` → `OnHold` / `Completed` / `Cancelled`.
Shipment status: `AwaitingInventory` → `Released` → `Packing` → `Shipped`.
Managed via `OrderStatusManager` static methods.

**`CartHelper`** was the GOD class for cart operations — obsoleted in Commerce 12. Replace with new
abstraction APIs (`IOrderRepository`, `IOrderGroupFactory`).

**Key namespaces / types:**

- `Mediachase.Commerce.Orders.OrderGroup` — abstract base
- `Mediachase.Commerce.Orders.Cart` — shopping cart (`DefaultName`, `WishListName`, `ExchangeName`)
- `Mediachase.Commerce.Orders.PaymentPlan` — subscription/recurring order
- `Mediachase.Commerce.Orders.PurchaseOrder` — completed order
- `Mediachase.Commerce.Orders.OrderForm` — form within an order group
- `Mediachase.Commerce.Orders.Shipment` — shipment within an order form
- `Mediachase.Commerce.Orders.LineItem` — single line in a shipment
- `Mediachase.Commerce.Orders.OrderAddress` — address on an order group
- `Mediachase.Commerce.Orders.Managers.OrderGroupWorkflowManager` — runs activity workflows
- `Mediachase.Commerce.Orders.Managers.OrderStatusManager` — status transitions
- `Mediachase.Commerce.Orders.Managers.PurchaseOrderManager` — safe lineitem/shipment mutations
- `Mediachase.Commerce.Orders.Managers.OrderNotesManager` — add audit notes
- `Mediachase.Commerce.Website.CartHelper` — legacy helper (obsoleted Commerce 12)

---

## Chapter 7 — New Order System

The new order system introduces **interface abstractions** over the concrete MetaClass types,
enabling custom storage backends and proper unit-testability. `IOrderRepository` replaces
`CartHelper`; `IOrderGroupFactory` replaces direct `new` instantiation.

Key improvements over the legacy system:
- All objects created in-memory; database write only on explicit `IOrderRepository.Save(cart)`
- `IOrderGroupFactory.BuilderFor` / `IOrderGroupBuilder` allow custom creation logic
- Commerce 10.2 introduced **serialized carts** (entire cart serialised to one DB row), delivering
  up to 10× save performance for large/concurrent carts; enabled via `ecf.app.config` feature flag
- Workflow-free checkout: `cart.ApplyDiscounts()`, `cart.ValidateOrRemoveLineItems(...)`,
  `cart.UpdateInventoryOrRemoveLineItems(...)`, `cart.ProcessPayments()` replace workflow calls
  (requires new promotion system)

**Key namespaces / types:**

- `EPiServer.Commerce.Order.IOrderGroup` — abstraction over `OrderGroup`
- `EPiServer.Commerce.Order.ICart` — abstraction over `Cart`
- `EPiServer.Commerce.Order.IPurchaseOrder` — abstraction over `PurchaseOrder`
- `EPiServer.Commerce.Order.IPaymentPlan` — abstraction over `PaymentPlan`
- `EPiServer.Commerce.Order.IOrderForm` — abstraction over `OrderForm`
- `EPiServer.Commerce.Order.IShipment` — abstraction over `Shipment`
- `EPiServer.Commerce.Order.ILineItem` — abstraction over `LineItem`
- `EPiServer.Commerce.Order.IOrderAddress` — abstraction over `OrderAddress`
- `EPiServer.Commerce.Order.IPayment` — abstraction over `Payment`
- `EPiServer.Commerce.Order.IOrderRepository` — load/save/delete order groups
- `EPiServer.Commerce.Order.IOrderGroupFactory` — create in-memory order objects
- `EPiServer.Commerce.Order.IOrderGroupBuilder` — custom factory for specific order types
- `EPiServer.Commerce.Order.OrderReference` — lightweight order identity (id + type)
- `EPiServer.Commerce.Order.IExtendedProperties` — property bag (replaces MetaObject access)

```csharp
// Load or create a cart using the new abstraction API
var cart = _orderRepository.LoadOrCreateCart<ICart>(
    currentContact.ContactId, Cart.DefaultName, _currentMarket);
var lineItem = _orderGroupFactory.CreateLineItem(variantCode);
cart.AddLineItem(lineItem, _orderGroupFactory);
lineItem.Quantity = 1;
var orderLink = _orderRepository.Save(cart);
```

---

## Chapter 8 — Taxes

Tax configuration in Commerce Manager is hierarchical: **Jurisdiction** (specific geographic rule,
e.g., state/country) → **Jurisdiction Group** (grouping of jurisdictions) → **Tax** (rate applied per
Tax Category per Jurisdiction Group). Tax categories are assigned to variations in the Pricing tab.

Two calculation approaches:
- **Workflow-based (legacy):** `CartPrepare` workflow calculates taxes at checkout
- **Calculator-based (new):** `IOrderGroupTotalsCalculator.GetTotals(cart)` returns `SubTotal`,
  `Total`, `ShippingTotal`, `TaxTotal` without running a workflow

Commerce 12 added `IMarket.PricesIncludeTax` to natively support tax-inclusive pricing per market.

**Key namespaces / types:**

- `Mediachase.Commerce.Orders.Managers.TaxManager` — tax data DTO access
- `EPiServer.Commerce.Order.Calculator.ITaxCalculator` — low-level tax calculation interface
- `EPiServer.Commerce.Order.Calculator.IOrderGroupTotalsCalculator` — full order totals including tax
- `Mediachase.Commerce.Orders.Managers.JurisdictionManager` — jurisdiction CRUD

---

## Chapter 9 — Content Events

Catalog content events fire at two levels:

1. **`EPiServer.IContentEvents`** — fires when changes go through `IContentRepository`
   (Catalog UI, API calls). Subscribe to `MovingContent`, `SavingContent`, `DeletingContent`, etc.
   Use `e.Content is CatalogContentBase` to filter catalog events.

2. **`CatalogEventListenerBase`** — fires when changes go through the lower-level `ICatalogSystem`
   (e.g., legacy ServiceAPI before v5.0). Override `EntryUpdated`, `NodeUpdated`, etc.
   Register implementation with `[ServiceConfiguration(typeof(CatalogEventListenerBase))]`.

For **remote/cross-site events** (e.g., a separate ServiceAPI instance notifying the front-end site),
use `CatalogEventBroadcaster`. It broadcasts via Episerver Framework UDP events under
`CatalogEventBroadcaster.CommerceProductUpdated` GUID. Enable remote events in `web.config`
(UDP multicast endpoint `soap.udp://239.255.255.19:5000/RemoteEventService`).

**Key namespaces / types:**

- `EPiServer.IContentEvents` — CMS-level content event hub
- `EPiServer.ContentEventArgs` — event arguments for content changes
- `EPiServer.Commerce.Catalog.ContentTypes.CatalogContentBase` — base for all catalog content
- `Mediachase.Commerce.Catalog.Events.CatalogEventListenerBase` — low-level catalog event base class
- `Mediachase.Commerce.Catalog.Events.CatalogEventBroadcaster` — remote event broadcaster
- `Mediachase.Commerce.Catalog.Events.CatalogContentUpdateEventArgs` — deserialized remote event payload
- `EPiServer.Events.Event` — Episerver Framework UDP event mechanism

---

## Chapter 9 (cont.) — CatalogEventBroadcaster

The `CatalogEventBroadcaster` provides a distributed event bus for catalog changes. Listeners
subscribe through `Event.Get(CatalogEventBroadcaster.CommerceProductUpdated)` and attach
handlers in an `IInitializationModule`. Events carry serialised `CatalogContentUpdateEventArgs`
(byte array via `BinaryFormatter`) containing lists of affected IDs. Seventeen event-type string
constants are defined (e.g. `CatalogUpdatedEventType`, `CatalogEntryDeletedEventType`). From
Commerce 11.8, the `…Updating`/`…Deleting` variants were removed. Always unsubscribe in
`Uninitialize` to avoid memory leaks.

**Key types:**
- `EPiServer.Commerce.Catalog.Provider.CatalogEventBroadcaster`
- `EPiServer.Commerce.Catalog.Provider.CatalogContentUpdateEventArgs`
- `EPiServer.Framework.Cache.Event` (EPiServer Framework)
- `EPiServer.Framework.Cache.EventNotificationEventArgs`
- `EPiServer.Commerce.Catalog.Events.CatalogEventListenerBase`

```csharp
// Subscribe in IInitializationModule.Initialize
var catalogEvent = Event.Get(CatalogEventBroadcaster.CommerceProductUpdated);
catalogEvent.Raised += CatalogEventUpdated;

private void CatalogEventUpdated(object sender, EventNotificationEventArgs e)
{
    var args = (CatalogContentUpdateEventArgs)DeSerialize((byte[])e.Param);
    // check args.EventType against CatalogContentUpdateEventArgs constants
}
```

---

## Chapter 10 — Inventories, Prices, and Orders Events

### Prices and Inventories

Price and inventory events were added in Commerce 8.15/8.16 via `CatalogKeyEventBroadcaster`.
The default implementations of `IPriceService` and `IInventoryService` fire these events
automatically. If you provide custom implementations, you must fire them yourself:

```csharp
var args = new PriceUpdateEventArgs(affectedCatalogKeys)
{
    ApplicationHasContentModelTypes = _applicationContext.HasContentModelTypes
};
_broadcaster.OnPriceUpdated(this, args);
```

Listen by registering on `Event.Get(CatalogKeyEventBroadcaster.CatalogKeyEventGuid)`. The
deserialized `CatalogKeyEventArgs` can be cast to `PriceUpdateEventArgs` or
`InventoryUpdateEventArgs` to determine the event type. Always de-register event handlers
in `IInitializationModule.Uninitialize` to avoid memory leaks.

**Note:** `IPriceDetailService` changes do **not** automatically trigger price events unless the
underlying `IPriceService` is writable and the changes replicate through to it.

**Key types:**
- `EPiServer.Commerce.Catalog.Events.CatalogKeyEventBroadcaster` / `EPiServer.Commerce.Catalog.Provider.CatalogKeyEventBroadcaster` — fires price/inventory events
- `EPiServer.Commerce.Catalog.Provider.PriceUpdateEventArgs`
- `EPiServer.Commerce.Catalog.Provider.InventoryUpdateEventArgs`
- `EPiServer.Commerce.Catalog.Provider.CatalogKeyEventArgs`
- `Mediachase.Commerce.Catalog.CatalogKey`
- `EPiServer.Events.Event` — Episerver Framework event; use `Event.Get(guid)` to subscribe
- `EPiServer.Events.EventNotificationEventArgs` — carrier for serialized event payload (`Param` is `byte[]`)

### Order Events — Old Way

`Mediachase.Commerce.Orders.OrderContext` exposes `OrderGroupUpdated` and `OrderGroupDeleted`
events (static, local-only — not distributed across servers).

### Order Events — New Way (`IOrderRepositoryCallback`)

Register a class implementing `IOrderRepositoryCallback` with `[ServiceConfiguration]`. The
default `IOrderRepository` calls it on create/update/delete. Methods receive only an
`OrderReference`; the order must be reloaded if its data is needed. Note that Commerce Manager
(at book time) still used the old concrete APIs, so `IOrderRepositoryCallback` was not called for
CM-originated operations.

**Key types:**
- `EPiServer.Commerce.Order.IOrderRepositoryCallback`
- `EPiServer.Commerce.Order.OrderReference`
- `Mediachase.Commerce.Orders.OrderContext`
- `Mediachase.Commerce.Orders.OrderGroupEventArgs`

```csharp
[ServiceConfiguration(typeof(IOrderRepositoryCallback))]
public class MyOrderCallback : IOrderRepositoryCallback
{
    public void OnUpdated(OrderReference orderReference) { /* react to update */ }
    public void OnCreating(Guid customerId, string name) { }
    public void OnCreated(OrderReference orderReference) { }
    public void OnUpdating(OrderReference orderReference) { }
    public void OnDeleting(OrderReference orderReference) { }
    public void OnDeleted(OrderReference orderReference) { }
}
```

---

## Chapter 11 — Search

### SearchProviders (Index-Based)

Commerce ships three providers: `LuceneSearchProvider` (default, file-based),
`Solr35SearchProvider` (hosted Solr), and `FindSearchProvider` (EPiServer Find, separate index
from Find Commerce). Configured in `Configs\Mediachase.Search.config`. Indexing is handled by
`BaseCatalogIndexBuilder` / `CatalogIndexBuilder` which implement `ISearchIndexBuilder`.
Automatic incremental indexing uses `ChangeNotificationManager` polling a queue every ~5 s;
the processor can become "Closed" and requires a site restart.

**Search API usage (`CatalogEntrySearchCriteria`):**

```csharp
var criteria = new CatalogEntrySearchCriteria();
criteria.SearchPhrase = "keyword";
criteria.CatalogNames.Add("Consumer");
criteria.ClassTypes.Add("Product");
// Add facet filters
foreach (SearchFilter filter in SearchFilterHelper.Current.SearchConfig.SearchFilters)
    criteria.Add(filter);
var manager = new SearchManager(AppContext.Current.ApplicationName);
ISearchResults results = manager.Search(criteria);
var ids = results.GetKeyFieldValues<int>();
```

**Key types:**
- `Mediachase.Search.SearchManager`
- `Mediachase.Search.CatalogEntrySearchCriteria`
- `Mediachase.Search.AbstractSearchCriteria`
- `Mediachase.Search.SearchProvider` (abstract base)
- `Mediachase.Search.ISearchIndexBuilder`
- `Mediachase.Search.BaseCatalogIndexBuilder`
- `Mediachase.Search.SearchFilterHelper`
- `Mediachase.Search.Filters.SearchFilter`

### SQL-Based Search API (CatalogSearchParameters)

Use for back-end batch operations only; never for customer-facing pages. Wraps SQL
WHERE/meta-WHERE clauses.

**Key types:**
- `Mediachase.Commerce.Catalog.CatalogSearchOptions`
- `Mediachase.Commerce.Catalog.CatalogSearchParameters`
- `Mediachase.Commerce.Catalog.ICatalogSystem` (method: `FindItemsDto`)
- `Mediachase.Commerce.Core.SearchOptions`
- `Mediachase.Commerce.Core.SearchParameters`

### Find Commerce

`EPiServer.Commerce.Find` (separate NuGet, own versioning). Uses `SearchClient.Instance` and
strongly-typed lambda queries. Provides `FilterPriceAvailableForCurrentUser()`,
`FilterForVisitor()`, `ApplyBestBets()`, `Track()`, and extension methods such as
`IOrderGroup.ApplyDiscounts()`. Convention-based indexing via
`SearchClient.Instance.Conventions.ForInstancesOf<T>()`. Separate index from `FindSearchProvider`.

**Key types:**
- `EPiServer.Find.SearchClient` (singleton `Instance`)
- `EPiServer.Commerce.Find.FindCommerceInitializationModule`
- `EPiServer.Commerce.Find.CatalogContentEventListener`
- `EPiServer.Commerce.Find.ContentEventIndexer`
- `EPiServer.Commerce.Core.IStockPlacement` (inventory indexing interface)
- `EPiServer.Commerce.Catalog.ContentTypes.VariationContent`
- `EPiServer.Commerce.Catalog.ContentTypes.ProductContent`

### Order Search

Uses `Mediachase.Commerce.Orders.OrderContext` with `OrderSearchParameters` /
`OrderSearchOptions` (same base classes as catalog search). Methods:
`FindCarts`, `FindPaymentPlans`, `FindPurchaseOrders`, `FindPurchaseOrdersByStatus`.
Filter on `[OrderGroup].<field>` via `SqlWhereClause` or on metaclass fields via
`SqlMetaWhereClause` (`Meta.<FieldName>`). `FindPurchaseOrdersByStatus` does a clustered index
scan and should be used sparingly.

---

## Chapter 12 — The Old Promotion System

The legacy promotion engine (marked obsolete; will be removed in a future version) uses
CodeDom XML expressions evaluated by `System.Workflow.Activities.Rules.RuleEngine`. Campaigns
group promotions; promotions carry conditions and rewards. Promotions are applied by calling the
`CartValidate` / `CartPrepare` workflows which invoke `CalculateDiscountsActivity`. Coupon codes
must be added to `MarketingContext` before the workflow runs and persisted across requests (cookie
or session). Redemption is recorded by passing `UsageStatus = PromotionUsageStatus.Used` to
`CartCheckout` workflow. The engine is slow at scale (tested problematic with 500+ active
promotions) and handles concurrency poorly.

**Key types:**
- `Mediachase.Commerce.Marketing.MarketingContext`
- `Mediachase.Commerce.Marketing.PromotionHelper`
- `Mediachase.Commerce.Marketing.PromotionContext`
- `Mediachase.Commerce.Marketing.PromotionFilter`
- `Mediachase.Commerce.Marketing.PromotionEntriesSet`
- `Mediachase.Commerce.Marketing.PromotionEntry`
- `Mediachase.Commerce.Marketing.PromotionReward`
- `Mediachase.Commerce.Orders.Managers.OrderGroupWorkflowManager`

---

## Chapter 13 — The New Promotion System

Introduced as stable in Commerce 9.19; enabled by default in new installations from that version.
Campaigns (`SalesCampaign`) and promotions (`PromotionData`) are stored as CMS content under
`SalesCampaignFolder.CampaignRoot`. Editing is done in the Marketing UI (Dojo-based). Supports
export/import. Does not support localisation (at book time).

### Core Abstractions

`IPromotionEngine.Run(IOrderGroup)` evaluates all active promotions in priority order and returns
`IEnumerable<RewardDescription>`. Use `IOrderGroup.ApplyDiscounts()` as the convenient
extension. Processors are resolved by reflection from `PromotionProcessorBase<TPromotion>`.

**Key types:**
- `EPiServer.Commerce.Marketing.IPromotionEngine`
- `EPiServer.Commerce.Marketing.PromotionData` (abstract, extends `BasicContent`)
- `EPiServer.Commerce.Marketing.SalesCampaign`
- `EPiServer.Commerce.Marketing.SalesCampaignFolder`
- `EPiServer.Commerce.Marketing.EntryPromotion` / `OrderPromotion` / `ShippingPromotion`
- `EPiServer.Commerce.Marketing.PromotionProcessorBase<T>`
- `EPiServer.Commerce.Marketing.OrderPromotionProcessorBase<T>`
- `EPiServer.Commerce.Marketing.EntryPromotionProcessorBase<T>`
- `EPiServer.Commerce.Marketing.ShippingPromotionProcessorBase<T>`
- `EPiServer.Commerce.Marketing.PromotionProcessorContext`
- `EPiServer.Commerce.Marketing.RewardDescription`
- `EPiServer.Commerce.Marketing.RedemptionDescription`
- `EPiServer.Commerce.Marketing.FulfillmentStatus` (flags enum)
- `EPiServer.Commerce.Marketing.CatalogItemSelection`
- `EPiServer.Commerce.Marketing.CollectionTargetEvaluator`
- `EPiServer.Commerce.Marketing.PromotionEngineSettings`
- `EPiServer.Commerce.Marketing.ICouponFilter`
- `EPiServer.Commerce.Marketing.IEntryFilter`
- `EPiServer.Commerce.Marketing.EntryFilterSettings`
- `EPiServer.Commerce.Marketing.PromotionTypeHandler`

### Custom Promotion Example (Order-level)

```csharp
[ContentType(GUID = "7660BDC2-6F65-4D22-8FDB-812EE7F4074D",
             GroupName = "orderpromotion", Order = 100)]
[PromotionSettings(FixedRedemptionsPerOrder = 1)]
public class SpendAmountGetDiscount : OrderPromotion
{
    public virtual IList<Money> Amounts { get; set; }
    public virtual decimal Discount { get; set; }
    public virtual decimal AlmostFulfilledThreshold { get; set; }
}

public class SpendAmountGetDiscountProcessor
    : OrderPromotionProcessorBase<SpendAmountGetDiscount>
{
    protected override RewardDescription Evaluate(
        SpendAmountGetDiscount promotionData, PromotionProcessorContext context)
    {
        var threshold = promotionData.Amounts
            .FirstOrDefault(c => c.Currency == context.OrderGroup.Currency);
        var subTotal = context.OrderForm
            .GetSubTotal(context.OrderGroup.Currency).Amount;
        if (threshold.Amount <= subTotal)
        {
            var redemptions = new[] { CreateRedemptionDescription(context.OrderForm) };
            return RewardDescription.CreatePercentageReward(
                FulfillmentStatus.Fulfilled, redemptions,
                promotionData, promotionData.Discount, "Fulfilled");
        }
        return RewardDescription.CreateNotFulfilledDescription(
            promotionData, FulfillmentStatus.NotFulfilled);
    }
    protected override bool CanBeFulfilled(
        SpendAmountGetDiscount p, PromotionProcessorContext ctx)
        => p.Amounts.Any(c => c.Currency == ctx.OrderGroup.Currency);
    protected override PromotionItems GetPromotionItems(SpendAmountGetDiscount p)
        => new PromotionItems(p,
            new CatalogItemSelection(null, CatalogItemSelectionType.All, true),
            new CatalogItemSelection(null, CatalogItemSelectionType.All, true));
}
```

### Coupon Codes (new system)

Coupons are stored on `IOrderForm.CouponCodes` (persisted with the cart):

```csharp
cart.GetFirstForm().CouponCodes.Add(coupon);
```

### Promotion Exclusion

`PromotionData.ExcludedItems` holds `ContentReference` values pointing to other promotions,
campaigns, or `CampaignRoot` (all). Exclusion is mutual. Priority determines which is excluded.
From Commerce 11.3, `PromotionExclusionLevel=Unit` enables per-unit exclusion (configured in
`appSettings`).

---

## Chapter 14 — Business Foundation Overview

Business Foundation (BF) is the extensible entity/metadata system used for customer contacts,
addresses, credit cards, and organisations. Every object is an `EntityObject` with a `MetaClassName`
and `PrimaryKeyId`. All CRUD flows through `BusinessManager.Execute(Request)`, enabling
pipeline interception via `IPlugin`.

**Key types:**
- `Mediachase.BusinessFoundation.Data.Business.BusinessManager`
- `Mediachase.BusinessFoundation.Data.Business.EntityObject`
- `Mediachase.BusinessFoundation.Data.Business.FilterElement`
- `Mediachase.BusinessFoundation.Data.Business.SortingElement`
- `Mediachase.BusinessFoundation.Data.Business.IPlugin`
- `Mediachase.BusinessFoundation.Data.Business.BusinessContext`
- `Mediachase.BusinessFoundation.Data.Business.RequestMethod`
- `Mediachase.BusinessFoundation.Data.Meta.MetaClassManager`
- `Mediachase.BusinessFoundation.Data.Meta.MetaFieldBuilder`
- `Mediachase.Commerce.Customers.CustomerContext`
- `Mediachase.Commerce.Customers.CustomerContact`
- `Mediachase.Commerce.Customers.CustomerAddress` (alias `AddressEntity`)

**Extend BF objects via code:**

```csharp
MetaClassManager manager = DataContext.Current.MetaModel;
using (var scope = manager.BeginEdit(
    MetaClassManagerEditScope.SystemOwner, AccessLevel.System))
{
    var mc = manager.CreateMetaClass(
        "CreditCard", "Credit Card", "CreditCards",
        "cls_CreditCard", PrimaryKeyIdValueType.Guid);
    using (var builder = new MetaFieldBuilder(mc))
    {
        builder.CreateText("CreditCardNumber", "Credit Card Number", false, 512, false);
        builder.SaveChanges();
    }
    scope.SaveChanges();
}
```

`BusinessManager.List` returns at most 1,000 objects by default; change `maxListObject` in
`Configs\baf.data.metaobject.config`. Performance tip: add non-clustered indexes to custom
columns queried frequently (e.g. `PersonNumber` in `cls_Contact`).

---

## Extending the System — Framework Way

Plug custom implementations via `IConfigurableModule` (implement `ConfigureContainer`) or the
`[ServiceConfiguration]` attribute. Commerce provides two key modules to depend on:

- `Mediachase.Commerce.Initialization.CommerceInitialization` — registers core services
  (`ICatalogSystem`, `IInventoryService`, `IMarketService`, etc.)
- `EPiServer.Commerce.Initialization.InitializationModule` — registers content and routing;
  depends on `CommerceInitialization`.

The later registration wins in the DI container. Life-cycle options: `AddSingleton`,
`AddTransient`, `AddScoped` (per `HttpContext` or thread), `AddHttpContextScoped`.

**Key types:**
- `EPiServer.Framework.IInitializationModule`
- `EPiServer.Framework.IConfigurableModule`
- `EPiServer.ServiceLocation.ServiceConfigurationContext`
- `EPiServer.ServiceLocation.ServiceInstanceScope`
- `EPiServer.Commerce.Initialization.InitializationModule`
- `Mediachase.Commerce.Initialization.CommerceInitialization`
- `EPiServer.Commerce.Core.ICurrentMarket`

---

## The Unusual Ways (Workflows, Commerce Manager, Payment Providers)

### Customising Order Processing (ecf.order.config)

Register a custom subclass of `PurchaseOrder`, `Cart`, `PaymentPlan`, `OrderForm`, or
`LineItem` in `Configs\ecf.order.config` under `<MappedTypes>`. Override `AcceptChanges()` for
custom persistence. Deprecated in favour of the new abstraction APIs.

### ActivityFlow (Workflows)

Commerce 9 replaced Windows Workflow Foundation with a plain-C# `ActivityFlow` engine
(open-source: `Mediachase.Commerce.Workflow`). Workflows are decorated with
`[ActivityFlowConfiguration(Name="...", Priority=10)]` and use a fluent API:

```csharp
[ActivityFlowConfiguration(
    Name = OrderGroupWorkflowManager.CartCheckOutWorkflowName,
    Priority = 10, AvailableInBetaMode = true)]
public class MyCartCheckoutActivityFlow : ActivityFlow
{
    public override ActivityFlowRunner Configure(ActivityFlowRunner flow)
        => flow.If(() => ShouldProcessPayment())
               .Do<ProcessPaymentActivity>()
               .EndIf()
               .Do<CalculateTotalsActivity>()
               .Do<AdjustInventoryActivity>()
               .Do<RecordPromotionUsageActivity>();
}
```

Pass optional parameters via `Dictionary<string, object>` to
`OrderGroupWorkflowManager.RunWorkflow`. Mark context properties with
`[ActivityFlowContextProperty]`.

**Key types:**
- `Mediachase.Commerce.Engine.ActivityFlow`
- `Mediachase.Commerce.Engine.ActivityFlowConfiguration` (attribute)
- `Mediachase.Commerce.Engine.ActivityFlowRunner`
- `Mediachase.Commerce.Engine.ActivityFlowContextProperty` (attribute)
- `Mediachase.Commerce.Orders.Managers.OrderGroupWorkflowManager`

### Payment Providers

Implement `Mediachase.Commerce.Orders.Managers.IPaymentGateway` (or
`ISplitPaymentGateway` for per-shipment payments). Register via Commerce Manager UI;
`ProcessPayment(Payment, ref string)` is called for both charge and refund — check
`Payment.TransactionType` (string matching `TransactionType` enum values: `Authorization`,
`Capture`, `Sale`, `Void`, `Credit`, `CaptureOnly`).

**Key types:**
- `Mediachase.Commerce.Plugins.Payment.IPaymentGateway`
- `Mediachase.Commerce.Plugins.Payment.ISplitPaymentGateway`
- `Mediachase.Commerce.Orders.Payment` (abstract)
- `Mediachase.Commerce.Orders.CreditCardPayment`
- `Mediachase.Commerce.Orders.OtherPayment`
- `Mediachase.Commerce.Orders.TransactionType` (enum)
- `Mediachase.Commerce.Orders.Managers.PaymentManager`

### Commerce Manager Extensions

Extend Commerce Manager by editing XML config files in `Apps\<Module>\Config\View\` and
adding WebForms `.ascx` controls. Navigation links, commands, grids, and permission types are
all XML-driven. Register new `PermissionType` values via
`Mediachase.Commerce.Permissions.PermissionTypeRepository.Register(PermissionType)`.

---

## ProfileModule and CustomerContact Events

### ProfileModule / IProfileMigrator

`EPiServer.Business.Commerce.HttpModules.ProfileModule` merges anonymous carts and migrates
orders when a user logs in. From Commerce 10.4, override by implementing
`EPiServer.Commerce.Order.IProfileMigrator` and registering it via DI — no web.config change
needed.

**Key types:**
- `EPiServer.Business.Commerce.HttpModules.ProfileModule`
- `EPiServer.Commerce.Order.IProfileMigrator`
- `EPiServer.Commerce.Order.ProfileMigrator` (default implementation)

### CustomerContact Events via IPlugin

Since there is no `ICustomerContactService`, react to contact changes by implementing
`IPlugin`. Register the plugin in `Configs\baf.data.manager.config` with `method`, `metaClass`,
`eventStage`, and `type` attributes. `eventStage` values: `PreMainOperation`,
`PreMainOperationInsideTranasaction`, `PostMainOperationInsideTranasaction`,
`PostMainOperation`.

```csharp
public class CustomerContactHandler : IPlugin
{
    public void Execute(BusinessContext context)
    {
        if (context.Request.Target.MetaClassName != "Contact") return;
        if (context.Request.Method == RequestMethod.Delete)
        {
            // react to contact deletion
        }
    }
}
```

---

## Migration Steps

`IMigrationStep` (in `EPiServer.Commerce.Internal.Migration` — internal, no backwards
compatibility guarantees) provides a one-time-per-database execution mechanism. Implement
`Execute(IProgressMessenger)` returning `true` on success. `MigrationManager` records completed
steps in the database. Enable auto-run via `appSettings` key `AutoMigrateEPiServer=true` or in
code:

```csharp
context.Locate.Advanced.GetInstance<MigrationSettings>().AutoMigrate = true;
```

Steps run after `IInitializationModule` and before site traffic. Requires
`EPiServer.Commerce.UI` for the migration view. Commerce Manager will refuse to start until all
steps are complete.

**Key types:**
- `EPiServer.Commerce.Internal.Migration.IMigrationStep`
- `EPiServer.Commerce.Internal.Migration.MigrationManager`
- `EPiServer.Commerce.Internal.Migration.MigrationSettings`
- `EPiServer.Commerce.Internal.Migration.IProgressMessenger`

---

## Caching

Use `ISynchronizedObjectInstanceCache` (EPiServer Framework) for all caching — it synchronises
invalidation across servers. Best practices from the book:

- Keep cache keys short and unique; use a prefix pattern such as `EP:EC:<Area>:<id>`.
- Define master keys (e.g. `EP:EC:Catalog:*`) and attach dependent entries; call
  `Remove(masterKey)` to bust all related entries.
- Use `MasterCacheKeys.RootKey`, `AnyEntryKey`, `AnyNodeKey`, `GetEntryKey`,
  `GetNodeKey` for Commerce-standard master keys.
- Prefer `ISynchronizedObjectInstanceCache.ReadThrough` with `ReadStrategy.Wait` (CMS/Commerce
  10+) to avoid race conditions.
- Use `CacheTimeoutType.Sliding` for frequently accessed entries; avoid indefinite caching.
- Do not double-cache: Commerce already caches contents, DTOs, MetaObjects, carts,
  `IPriceValue`s, and inventory by default.

**Key types:**
- `EPiServer.Framework.Cache.ISynchronizedObjectInstanceCache`
- `EPiServer.Framework.Cache.IObjectInstanceCache`
- `EPiServer.Framework.Cache.CacheEvictionPolicy`
- `EPiServer.Commerce.Catalog.MasterCacheKeys`

---

## Routing

Two routing systems coexist:

### SEO URI (Legacy)

Unique per-language URIs stored in `CatalogItemSeo`. Handled by `SeoUriRouter`
(`ISimpleAddressRouteHandler`). URI generation by `Mediachase.Commerce.Catalog.Data.UniqueSeoGenerator`
(virtual — override `UriExtension`, `GenerateSeoUri`, `GetRandomToken`). SEO URIs are
the only routing option in Commerce R1–7.4.

### Hierarchical Partial Router (Commerce 7.5+)

`HierarchicalCatalogPartialRouter` implements `ICommerceRouter` / `IPartialRouter`. Reflects
catalog hierarchy in URL segments using `RouteSegment` (stored as `UriSegment` in
`CatalogItemSeo`). Configure via:

```csharp
// Default setup (StartPage as root, Commerce root as catalog root)
CatalogRouteHelper.MapDefaultHierarchialRouter(RouteTable.Routes, false);

// Custom root (hide catalog segment from URL)
var commerceRoot = contentLoader
    .GetChildren<CatalogContent>(referenceConverter.GetRootLink()).First();
RouteTables.Routes.RegisterPartialRouter(
    new HierarchicalCatalogPartialRouter(startingPoint, commerceRoot, false));
```

To redirect hierarchical URLs to SEO URIs: call
`CatalogRouteHelper.MapDefaultHierarchialRouter(routes, true)` then
`CatalogRouteHelper.SetupSeoUriPermanentRedirect()`.

To redirect SEO URIs to hierarchical: subscribe to `ContentRoute.RoutedContent` and
inspect `RoutingEventArgs.RoutingSegmentContext.LastConsumedFragment` (empty for hierarchical
routes, non-empty for SEO URI routes).

**Key types:**
- `EPiServer.Commerce.Routing.HierarchicalCatalogPartialRouter`
- `EPiServer.Commerce.Routing.CatalogRouteHelper`
- `EPiServer.Commerce.Routing.SeoUriRouter`
- `EPiServer.Commerce.Routing.ICommerceRouter`
- `EPiServer.Web.Routing.IPartialRouter<TContent, TRoutedData>`
- `EPiServer.Web.Routing.ContentRoute` (events: `RoutingContent`, `RoutedContent`)
- `Mediachase.Commerce.Catalog.Data.UniqueSeoGenerator`

---

## Appendix — Performance and Upgrade Guidance

**Performance summary:**
- Keep performance in mind from the start; define measurable targets.
- Always use the latest Commerce version — core-level optimisations are delivered there.
- Use profilers (dotTrace, ANTS, SQL Server Profiler) to identify bottlenecks before
  attempting optimisation.
- `ISynchronizedObjectInstanceCache` with `ReadStrategy.Wait` prevents redundant DB hits.
- Add non-clustered SQL indexes only to highly-selective, frequently-queried custom columns;
  avoid indexing default system metafields.

**Upgrade guidance summary:**
- Back up site and databases before every upgrade step.
- Upgrade one major version at a time; verify both front-end and Commerce Manager after
  each step.
- Always upgrade the front-end site first (database changes and migration steps run there).
- Migration steps must all complete successfully before going live; do not tamper with the
  migration step records in the database.
- For very old sites (pre-Commerce R3a), consider export/import of the catalog rather than
  relying solely on upgrade scripts.

---

## Migration Notes for Current Optimizely 14 Codebases

The patterns below changed between the era this book documents (Commerce 9–12, .NET Framework, WebForms Commerce Manager) and current **Optimizely Commerce 14** (.NET 6/8, ASP.NET Core, no WebForms, Optimizely DXP). Use this as a quick "what changed" map when reading legacy code:

| Older concept (book) | Current Optimizely 14 equivalent / replacement |
|---|---|
| `EPiServer.ServiceLocation.ServiceLocator.Current.GetInstance<T>()` | Constructor injection via `Microsoft.Extensions.DependencyInjection` (`IServiceProvider`); `ServiceLocator` still exists but is discouraged |
| `IConfigurableModule.ConfigureContainer(ServiceConfigurationContext)` registering with StructureMap-style API | Same interface, but `context.Services` is `IServiceCollection` (Microsoft DI). Registration happens on the standard `IServiceCollection` |
| `[InitializableModule]` + `IInitializationModule` | Still present and supported, but `Program.cs` / `Startup.cs` configures services via `services.AddCommerce()` and friends |
| `CartHelper`, `OrderContext`, `OrderGroupWorkflowManager`, `OrderStatusManager` static APIs | **Removed** (deprecated). Use `IOrderRepository`, `IOrderGroupFactory`, `IOrderGroupCalculator`, `IPlacedPriceProcessor`, `IOrderValidationService` |
| `Mediachase.Commerce.Orders.Cart` / `OrderGroup` / `PurchaseOrder` concrete classes | Use the **abstraction interfaces** `ICart`, `IOrderGroup`, `IPurchaseOrder`, `IOrderForm`, `ILineItem`, `IShipment`, `IPayment` exclusively |
| Workflows (`ActivityFlow` / `CartValidate` / `CartPrepare` / `CartCheckout`) | **Removed.** Workflow-free checkout: `cart.ApplyDiscounts()`, `cart.ValidateOrRemoveLineItems(...)`, `cart.UpdateInventoryOrRemoveLineItems(...)`, `cart.ProcessPayments()`, `cart.AdjustInventoryOrRemoveLineItems(...)` |
| `MarketingContext` + old promotion engine | Removed. New promotion engine only: `IPromotionEngine`, `PromotionData`, processors derived from `PromotionProcessorBase<T>` |
| `Mediachase.Commerce.Catalog.ICatalogSystem` (DTO API) | Marked obsolete in favour of `IContentRepository`/`IContentLoader` for catalog content. Some legacy methods still exist for back-compat |
| WebForms Commerce Manager extensions (`.ascx`, `Apps\<Module>\Config\View\` XML) | **Gone** in Optimizely 12+. Catalog and Order management consolidated into the Optimizely CMS UI (Dojo + React); custom UI uses Dojo widgets or React micro-frontends |
| `ProfileModule` HTTP module (web.config) | Implement `IProfileMigrator` and register via DI; no `web.config` (no longer used in ASP.NET Core) |
| `Configs\ecf.app.config`, `Configs\ecf.order.config`, `Configs\Mediachase.Search.config`, `Configs\baf.data.manager.config` | Replaced by `appsettings.json` configuration sections and `IConfigurableModule` registrations. Most file-based XML config is obsolete |
| `IPaymentGateway.ProcessPayment(Payment, ref string)` | New `IPaymentPlugin` / `IPaymentGateway` overloads using `IPayment` abstraction; transaction types still apply |
| `IShippingPlugin` / `IShippingGateway` (old) | Same intent — current API uses `IShippingPlugin` and `IShippingGateway` working with `IShipment` and `LineItem` abstractions |
| Lucene / Solr `SearchProvider` system | Removed (Lucene provider deprecated). Use **EPiServer Find** (Find.Commerce) for full-text/faceted search; SQL Search API still available for back-end |
| `ServiceAPI` (REST catalog API) WebForms-hosted | Replaced by **Content Delivery API** / **Commerce REST API** in ASP.NET Core; OData endpoints |
| `CatalogEventBroadcaster` UDP multicast remote events | Replaced by **Azure Service Bus** events on DXP / Optimizely Cloud. Local in-process events via `IContentEvents` still work |
| `web.config` `<system.web>` settings | Replaced by `appsettings.json` and `Program.cs` host configuration |
| `Mediachase.MetaDataPlus` direct usage | Discouraged. Strongly typed catalog content (`ProductContent`, `VariationContent`, etc.) is the canonical approach; metaclasses are auto-synced from C# types |
| StructureMap container | Microsoft DI container (`Microsoft.Extensions.DependencyInjection`) |
| `BusinessFoundation` for customer contacts | Still present (`CustomerContext`, `CustomerContact`, `CustomerAddress`) but newer `IUserService` / Identity-based abstractions are preferred |

For current Optimizely Commerce 14 / .NET 8 patterns and APIs, see [pro-optimizely-commerce-cloud.md](pro-optimizely-commerce-cloud.md) — the modern reference doc that aligns with this project's tech stack.
