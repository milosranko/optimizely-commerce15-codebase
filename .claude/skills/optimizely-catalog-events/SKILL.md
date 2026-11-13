---
name: optimizely-catalog-events
description: Add a Commerce catalog event handler in Optimizely Commerce Connect (CMS 12 / Commerce 14) - ICatalogEvents subscribed from an [InitializableModule], or a CatalogEventListenerBase registered with [ServiceConfiguration]. Covers the sixteen entry, node, catalog, association and relation events, the change-collection args that carry integer IDs rather than content, turning those IDs into ContentReference with ReferenceConverter, why catalog events cannot be cancelled, surviving bulk imports that raise thousands of them, the Deleted args that are the only place the ID still exists, and the neighbouring buses (IContentEvents, CatalogKeyEventBroadcaster for price and inventory, CatalogEventBroadcaster for cross-instance). Use whenever asked to react to catalog entry, node, catalog, association or relation changes, to catch edits made by Commerce Manager, ServiceAPI or catalog import that IContentEvents never sees, or to debug a catalog handler that fires thousands of times, fires with no usable content, or never fires.
---

# Adding a Commerce catalog event handler

`ICatalogEvents` is the **DTO-layer** catalog bus. It fires for catalog writes regardless of which API made them - `IContentRepository`, `ICatalogSystem`, ServiceAPI, catalog XML import/export, or the Commerce Manager UI. That breadth is its entire reason to exist, and it is also why the arguments hand you integer IDs and change records rather than content objects.

This repo has **no catalog event subscriber yet**. `src/Wisbar.Web/Infrastructure/CommerceInitialization.cs` is the module that would host one; read it and the content-event module next to it first.

## Step 0: Is `ICatalogEvents` the right bus?

Answer this before writing code. In this repo the answer is often no.

| You want to react to | Bus | Why |
|---|---|---|
| A product or variant published by the site's own code | `IContentEvents` | Gives you the typed `IContent`. **This repo's iMIS imports (`ProductsImportService`, `UCommerceDataImportService`) write through `IContentRepository`**, so they already raise content events - see the `optimizely-content-events` skill. |
| Catalog changes from Commerce Manager, ServiceAPI, catalog XML import, or direct `ICatalogSystem` calls | `ICatalogEvents` | This skill. `IContentEvents` never sees these. |
| Price or inventory changes | `CatalogKeyEventBroadcaster` | `PriceUpdated`, `InventoryUpdated`, `CatalogMetaDataUpdated`. Inventory fires very frequently. |
| The same change on **every** DXP instance, not just the one that made it | `Event.Get(CatalogEventBroadcaster.CommerceProductUpdated).Raised` | `ICatalogEvents` is in-process only. See Step 7. |
| Cart or order writes | `IOrderEvents` | Not catalog. See `.claude/rules/commerce.md`. |

Choosing `ICatalogEvents` when `IContentEvents` would do costs you the typed content object and buys you nothing. Choosing `IContentEvents` when the change arrives through ServiceAPI or a catalog import means the handler silently never runs.

## Step 1: Pick the shape

Two supported shapes, same events either way.

| Shape | Registration | Use when |
|---|---|---|
| Subscribe to `ICatalogEvents` | `[InitializableModule]`, attach in `Initialize`, detach in `Uninitialize` | Default. Matches how this repo already wires `IContentEvents`, and you choose exactly which events to attach. |
| Derive from `CatalogEventListenerBase` | `[ServiceConfiguration(typeof(CatalogEventListenerBase))]` on the class, no module | You want many events at once and prefer overrides to subscriptions. This is how Optimizely's own `CatalogLogger` and `CatalogEventHandler` are registered. |

`ICatalogEvents` is itself registered by attribute (`[ServiceConfiguration(typeof(ICatalogEvents))]` on the internal `DefaultCatalogEvents`), so resolve it from DI and never `new` it.

**`Mediachase.Commerce.Catalog.EventContext` is the legacy raiser. Do not subscribe to it.** Same rule as `OrderContext` and `CartHelper` in `.claude/rules/commerce.md`.

## Step 2: Register and unregister symmetrically

```csharp
[InitializableModule]
[ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
public class CommerceInitialization : IInitializableModule
{
    private ICatalogEvents? _catalogEvents;

    public void Initialize(InitializationEngine context)
    {
        _catalogEvents = context.Locate.Advanced.GetRequiredService<ICatalogEvents>();
        _catalogEvents.EntryUpdated += OnEntryUpdated;
    }

    public void Uninitialize(InitializationEngine context)
    {
        if (_catalogEvents is null) return;
        _catalogEvents.EntryUpdated -= OnEntryUpdated;
        _catalogEvents = null;
    }
}
```

- The `ModuleDependency` on Commerce's `InitializationModule` is what the existing `CommerceInitialization` already declares. Keep it.
- Resolve the service **once into a field** and detach from that same instance.
- `CommerceInitialization.Uninitialize` is currently empty. If you add a subscription there, it stops being empty - a subscription without its removal is what leaves handlers doubled after a re-initialize.
- Put the module in `Wisbar.Web/Infrastructure/` for site-wide wiring, or let a feature own its module in `Wisbar.Business` the way `FindIndexingConventionsModule` does. Never in `Wisbar.Optimizely.Core` or `Wisbar.Integrations` - they are leaves.

## Step 3: The args carry change records, not content

This is the one thing that makes catalog handlers different from content handlers. There is no `e.Content`. Every non-deleted event exposes a **collection of change records**, each holding integer IDs and a `ChangeType` of `Added`, `Modified` or `Deleted`:

```csharp
private void OnEntryUpdated(object? sender, EntryEventArgs e)
{
    foreach (var change in e.Changes)
    {
        // change.EntryId, change.CatalogId, change.ChangeType
    }
}
```

One event can carry hundreds of changes. Handle the collection, never just its first element.

**Turning an ID into something usable** needs `ReferenceConverter`:

```csharp
var link = _referenceConverter.GetContentLink(change.EntryId, CatalogContentType.CatalogEntry, 0);
var code = _referenceConverter.GetCode(link);

if (_contentLoader.TryGet<ProductBase>(link, out var product)) { ... }
```

`CatalogContentType` is `CatalogEntry`, `CatalogNode`, `Catalog` or `Root`. Pass `0` as the version ID for the current version. Load with `TryGet`, not `Get` - on an `Updating` event the row may not be readable yet, and on a delete it is gone.

Property-by-property detail for all sixteen events and every args and change type: `references/events.md`.

## Step 4: `Deleted` events are shaped differently, and are your last chance

The `Deleted*` args classes are the only ones carrying the identity **directly** rather than in a change collection - `DeletedEntryEventArgs.EntryId`, `DeletedNodeEventArgs.NodeId`, `DeletedAssociationEventArgs.AssociationId`, and so on.

By the time they fire the row is gone: `ReferenceConverter.GetContentLink` still computes a link, but nothing loads from it and the code is unresolvable. Anything you need about the deleted item has to have been captured on the matching `Updating` event or already be stored elsewhere. There is no `Deleting` event for entries and nodes - `EntryDeleted` **is** the earliest hook.

## Step 5: Catalog events cannot be cancelled

`CatalogSystemEventArgs` derives from `FrameworkEventArgs`, which exposes only `EventName`. There is no `CancelAction` and no `CancelReason` anywhere in the hierarchy - verified against `Mediachase.Commerce` 14.45.5.

So the `Updating` events are **notifications that a write is about to happen**, not a veto. Validation that must block an editor belongs on `IContentEvents.PublishingContent`, which is cancellable. Throwing from a catalog handler does not cleanly abort the write and will surface as an unhandled error in an import or a ServiceAPI response.

Use `Updating` for one thing only: capturing the before-state you will need once `Updated` fires.

## Step 6: Assume bulk, and be idempotent

A catalog import raises these events continuously, in large batches, with no request context.

- **No per-item network or database work.** A handler that does one HTTP call per change turns a 10,000-row import into 10,000 calls. Collect the IDs, dedupe them, and hand the batch to a background job - see `hangfire-background-jobs`.
- **No `HttpContext`, no `SiteDefinition.Current`, no visitor identity.** Imports and scheduled jobs raise these with no request scope. Most services in this repo are `AddScoped`, and resolving a scoped service off the root provider throws in Development, where scope validation is on. Create a scope with `IServiceScopeFactory` when the handler can run outside a request.
- **Catch and log.** An exception escaping a catalog handler propagates into whatever was writing the catalog - a Commerce Manager save, or a running import.
- **Expect duplicates.** The same entry can appear in several events during one operation (an entry update plus a relation update plus an association update). Make the work idempotent rather than assuming one change means one event.
- **The events are in-process.** They fire on the instance that performed the write. Anything that must take effect site-wide needs a broadcasting mechanism - Step 7, or in this repo `CacheService.Remove`, which calls the synchronized `CacheManager.Remove` rather than `RemoveLocalOnly`.

## Step 7: Cross-instance, and the .NET 8 caveat

`CatalogEventBroadcaster` re-raises catalog changes over EPiServer Framework Events (Service Bus on DXP), which is how other instances hear about them:

```csharp
Event.Get(CatalogEventBroadcaster.CommerceProductUpdated).Raised += OnRemoteCatalogChange;
```

The payload is a serialized `CatalogContentUpdateEventArgs` (`EventType`, `CatalogIds`, `CatalogNodeIds`, `CatalogEntryIds`, `CatalogAssociationIds`, `HasChangedParent`).

**The `BinaryFormatter` deserialization shown in `.claude/knowledge-base/epi-commerce-recipes.md` does not work on .NET 8** - `BinaryFormatter` is removed from the supported surface and throws at runtime. This solution targets net8.0. Do not copy that recipe verbatim; if you need cross-instance catalog reactions, prefer letting Find.Commerce's own listeners handle reindexing, or raise your own event with your own payload, and say so rather than shipping code that throws on the first remote event.

## Step 8: Price and inventory are a different broadcaster

`CatalogKeyEventBroadcaster` (`Mediachase.Commerce.Engine.Events`) exposes `PriceUpdated`, `InventoryUpdated` and `CatalogMetaDataUpdated`. Args carry `CatalogKeys` (each a `CatalogKey` with `CatalogEntryCode`), and `InventoryUpdateEventArgs` adds `InventoryChanges` keyed by `CatalogEntryCode` plus `WarehouseCode`.

Find.Commerce already listens to these and reindexes. Before adding a handler here, check you are not duplicating that. Inventory in particular fires on every stock movement, checkout included.

## Step 9: Verify

```bash
dotnet build src/Wisbar.sln
dotnet test src/Wisbar.Tests/Wisbar.Tests.csproj
```

Catalog handlers are more testable than content handlers: every change record and args class has a public constructor, so a test can build real `EntryChange` values and call a `CatalogEventListenerBase` method directly, with no `IContent` to mock. Still put the decision in a pure function over the change records. Then report which of these you actually checked:

- The handler fires for the change you targeted, and the `Changes` collection is iterated rather than sampled.
- IDs resolve: `ReferenceConverter` produces a link that loads for an updated entry, and the `Deleted` path does not try to load anything.
- A bulk change (import, or a multi-select edit in Commerce Manager) does not produce per-item slow work.
- Nothing throws into the writer - force a failure inside the handler and confirm the catalog write still completes.
- Detach works: the handler is not registered twice after a re-initialize.
- If it can run outside a request, exercise that path - that is where scoped services and `SiteDefinition.Current` break.

## Checklist

- [ ] `ICatalogEvents` is genuinely the right bus, not `IContentEvents` (which this repo's own imports already raise)
- [ ] Shape chosen deliberately: module subscription, or `CatalogEventListenerBase` + `[ServiceConfiguration]`
- [ ] No use of the legacy `EventContext`
- [ ] `[ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]` on the module
- [ ] Service resolved once into a field; every `+=` has its `-=` in `Uninitialize`
- [ ] `Changes` collections iterated in full; IDs converted with `ReferenceConverter` and loaded with `TryGet`
- [ ] `Deleted` events read the ID off the args, and load nothing
- [ ] No attempt to cancel or throw from a catalog handler
- [ ] Bulk-safe: batched, deduped, idempotent, no per-item network work, exceptions caught and logged
- [ ] Scoped services resolved through an explicit scope if the handler can run outside a request
- [ ] No `BinaryFormatter` in any remote-event code on .NET 8
- [ ] Decision logic extracted to a testable pure function; solution builds and tests pass

Full code templates: `references/templates.md`. Complete event catalogue: `references/events.md`.
