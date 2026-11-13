# Catalog event handler templates

Namespaces use the Wisbar layout. The repo is the authority - these are the shape. Comments stay within the two-line limit from `CLAUDE.md`.

## A. Subscribing from the Commerce module

`Wisbar.Web/Infrastructure/CommerceInitialization.cs`. The existing module already carries the Commerce `ModuleDependency` and an empty `Uninitialize`; adding a subscription means filling that in. The `Initialize` body below is abridged - the real file also sets catalog access rights and personalization widgets, so add to it rather than replacing it.

```csharp
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using Mediachase.Commerce.Catalog.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Wisbar.Web.Infrastructure;

[InitializableModule]
[ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
public class CommerceInitialization : IInitializableModule
{
    private ICatalogEvents? _catalogEvents;

    public void Initialize(InitializationEngine context)
    {
        CatalogRouteHelper.MapDefaultHierarchialRouter(false);

        // DTO-layer events, so Commerce Manager and ServiceAPI edits are covered too -
        // IContentEvents only sees writes the site itself makes through IContentRepository.
        _catalogEvents = context.Locate.Advanced.GetRequiredService<ICatalogEvents>();
        _catalogEvents.EntryUpdated += OnEntryUpdated;
        _catalogEvents.EntryDeleted += OnEntryDeleted;
    }

    public void Uninitialize(InitializationEngine context)
    {
        if (_catalogEvents is null)
        {
            return;
        }

        _catalogEvents.EntryDeleted -= OnEntryDeleted;
        _catalogEvents.EntryUpdated -= OnEntryUpdated;
        _catalogEvents = null;
    }
}
```

## B. The listener shape

For a handler that wants many events, derive from `CatalogEventListenerBase` and register by attribute. No module, no attach/detach. This is how `CatalogLogger` and `CatalogEventHandler` ship.

```csharp
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog.Events;

namespace Wisbar.Business.Features.Catalog;

[ServiceConfiguration(typeof(CatalogEventListenerBase))]
public class CatalogChangeListener : CatalogEventListenerBase
{
    private readonly ICatalogChangeCollector _collector;

    public CatalogChangeListener(ICatalogChangeCollector collector) => _collector = collector;

    public override void EntryUpdated(object source, EntryEventArgs args)
        => _collector.Collect(args.Changes.Select(c => c.EntryId));

    public override void RelationUpdated(object source, RelationEventArgs args)
        => _collector.Collect(args.NodeEntryRelationChanges.Select(c => c.EntryId));
}
```

## C. Reading the change collection

Every non-deleted event hands you a collection. Iterate it; one event routinely carries hundreds of changes.

```csharp
private void OnEntryUpdated(object? sender, EntryEventArgs e)
{
    var referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();
    var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();

    foreach (var change in e.Changes.Where(c => c.ChangeType != ChangeType.Deleted))
    {
        var link = referenceConverter.GetContentLink(change.EntryId, CatalogContentType.CatalogEntry, 0);

        // TryGet, not Get - an Updating event can fire before the row is readable.
        if (contentLoader.TryGet<ProductBase>(link, out var product))
        {
            // ...product.Code, product.DisplayName
        }
    }
}
```

## D. Deletes: read the args, load nothing

```csharp
private void OnEntryDeleted(object? sender, DeletedEntryEventArgs e)
{
    // The row is already gone - EntryId off the args is all that is left, and there is
    // no EntryDeleting event to have captured more from.
    ServiceLocator.Current.GetInstance<IProductProjectionStore>()
        .Forget(e.CatalogId, e.EntryId);
}
```

Relation deletes arrive as one of three subclasses. Cast to find out which:

```csharp
private void OnRelationDeleted(object? sender, DeletedRelationEventArgs e)
{
    switch (e)
    {
        case DeletedNodeEntryRelationEventArgs nodeEntry:
            // product nodeEntry.EntryId removed from category nodeEntry.NodeId
            break;
        case DeletedEntryRelationEventArgs entryEntry:
            // bundle/package child entryEntry.ChildEntryId removed from entryEntry.ParentEntryId
            break;
        case DeletedNodeRelationEventArgs nodeNode:
            // category nodeNode.ChildNodeId unlinked from nodeNode.ParentNodeId
            break;
    }
}
```

## E. Batch instead of working per change

A catalog import raises these continuously. Collect IDs, dedupe, hand one batch to a background job.

```csharp
private void OnEntryUpdated(object? sender, EntryEventArgs e)
{
    var entryIds = e.Changes
        .Where(c => c.ChangeType != ChangeType.Deleted)
        .Select(c => c.EntryId)
        .Distinct()
        .ToArray();

    if (entryIds.Length == 0)
    {
        return;
    }

    // One job per event, not per entry - an import can raise this thousands of times.
    BackgroundJob.Enqueue<ICatalogProjectionJob>(job => job.Refresh(entryIds));
}
```

## F. Scoped services, and containing failures

Catalog events fire from imports and jobs with no request scope. Most services in this repo are `AddScoped`, and a root-provider resolve of one throws in Development.

```csharp
private void OnEntryUpdated(object? sender, EntryEventArgs e)
{
    var logger = ServiceLocator.Current.GetInstance<ILogger<CommerceInitialization>>();

    try
    {
        using var scope = ServiceLocator.Current
            .GetInstance<IServiceScopeFactory>()
            .CreateScope();

        scope.ServiceProvider
            .GetRequiredService<ICatalogProjectionService>()
            .Refresh(e.Changes.Select(c => c.EntryId).Distinct());
    }
    catch (Exception ex)
    {
        // An escaping exception propagates into the import or Commerce Manager save.
        logger.LogWarning(ex, "Catalog projection refresh failed for {ChangeCount} entry changes.", e.Changes.Count());
    }
}
```

## G. Node moves

`NodeEventArgs.HasChangedParent` is the flag that separates a rename from a move in the tree.

```csharp
private void OnNodeUpdated(object? sender, NodeEventArgs e)
{
    if (!e.HasChangedParent)
    {
        return;
    }

    foreach (var change in e.Changes)
    {
        // change.NodeId moved under change.ParentNodeId
    }
}
```

## H. Price and inventory

A different broadcaster, keyed by entry **code** rather than ID.

```csharp
public void Initialize(InitializationEngine context)
{
    _keyBroadcaster = context.Locate.Advanced.GetRequiredService<CatalogKeyEventBroadcaster>();
    _keyBroadcaster.PriceUpdated += OnPriceUpdated;
}

private void OnPriceUpdated(object? sender, PriceUpdateEventArgs e)
{
    var codes = e.CatalogKeys.Select(k => k.CatalogEntryCode).Distinct().ToArray();
    // Find.Commerce already reindexes on this - only add work it does not already do.
}
```

`InventoryUpdated` additionally carries `InventoryChanges`, each an `InventoryKey` with `CatalogEntryCode` and `WarehouseCode` plus the six decimal deltas. It fires on every stock movement, checkout included - filter hard or do not subscribe.

## I. Cross-instance events

Only if in-process is genuinely not enough. Note the .NET 8 constraint in SKILL.md Step 7: do not deserialize the payload with `BinaryFormatter`.

```csharp
public void Initialize(InitializationEngine context)
{
    Event.Get(CatalogEventBroadcaster.CommerceProductUpdated).Raised += OnRemoteCatalogChange;
}

public void Uninitialize(InitializationEngine context)
{
    Event.Get(CatalogEventBroadcaster.CommerceProductUpdated).Raised -= OnRemoteCatalogChange;
}
```

## J. Testing

Catalog events test better than content events do: every change record and args class has a **public constructor**, so a test can build real arguments rather than mocking `IContent`.

Put the decision in a pure function over the change records, as `SortOptionsExtensionsTests` does for the content-event reconciler:

```csharp
public static IReadOnlyCollection<int> EntriesNeedingRefresh(IEnumerable<EntryChange> changes) =>
    changes.Where(c => c.ChangeType != ChangeType.Deleted)
           .Select(c => c.EntryId)
           .Distinct()
           .ToArray();

[TestMethod]
public void EntriesNeedingRefresh_DropsDeletesAndDuplicates()
{
    // ctor order is (changeType, entryId, catalogId).
    var result = CatalogChangeRules.EntriesNeedingRefresh(
    [
        new EntryChange(ChangeType.Added, 1, 5),
        new EntryChange(ChangeType.Modified, 1, 5),
        new EntryChange(ChangeType.Deleted, 2, 5),
    ]);

    CollectionAssert.AreEquivalent(new[] { 1 }, result.ToArray());
}
```

A `CatalogEventListenerBase` subclass (template B) can be exercised end to end, because the args are constructible too:

```csharp
var listener = new CatalogChangeListener(collector);

listener.EntryUpdated(null!, new EntryEventArgs("EntryUpdated", [new EntryChange(ChangeType.Modified, 1, 5)]));

Assert.AreEqual(1, collector.Collected.Single());
```

Constructor signatures for every change record are in `references/events.md`.
