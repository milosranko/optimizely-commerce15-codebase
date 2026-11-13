# Catalog event catalogue

Reflected out of the assemblies this solution builds against: `Mediachase.Commerce` / `EPiServer.Commerce.Core 14.45.5`, alongside `EPiServer.CMS.Core 12.24.0`. Names, argument types and inheritance below are read from those binaries, not recalled.

## `Mediachase.Commerce.Catalog.Events.ICatalogEvents` - all 16 events

Resolve with `context.Locate.Advanced.GetRequiredService<ICatalogEvents>()` or `ServiceLocator.Current.GetInstance<ICatalogEvents>()`. Implemented by the internal `DefaultCatalogEvents`, registered through `[ServiceConfiguration(typeof(ICatalogEvents))]`.

Every handler is `EventHandler<TArgs>`. **None of these can be cancelled** - see the args hierarchy below.

### Entries (products, variants, bundles, packages)

| Event | Args | Carries |
|---|---|---|
| `EntryUpdating` | `EntryEventArgs` | `Changes` - `IEnumerable<EntryChange>`. Fires before the write; notification only. |
| `EntryUpdated` | `EntryEventArgs` | `Changes`. The normal hook for reacting to entry edits. |
| `EntryDeleted` | `DeletedEntryEventArgs` | `CatalogId`, `EntryId` directly, plus inherited `Changes`. There is no `EntryDeleting`. |

### Nodes (categories)

| Event | Args | Carries |
|---|---|---|
| `NodeUpdating` | `NodeEventArgs` | `Changes` - `IEnumerable<NodeChange>`; `HasChangedParent` (settable). |
| `NodeUpdated` | `NodeEventArgs` | Same. `HasChangedParent` tells you a node moved in the tree. |
| `NodeDeleted` | `DeletedNodeEventArgs` | `CatalogId`, `NodeId`, `ParentNodeId` directly. |

### Catalogs

| Event | Args | Carries |
|---|---|---|
| `CatalogUpdating` | `CatalogEventArgs` | `CatalogChanges` - `IEnumerable<CatalogChange>`. |
| `CatalogUpdated` | `CatalogEventArgs` | Same. |
| `CatalogDeleted` | `DeletedCatalogEventArgs` | `CatalogId` directly. |
| `CatalogLanguageUpdated` | `CatalogLanguageEventArgs` | `CatalogId`; the catalog's language configuration changed. |

### Associations (cross-sell, up-sell, accessories)

| Event | Args | Carries |
|---|---|---|
| `AssociationUpdating` | `AssociationEventArgs` | `Changes` - `IEnumerable<AssociationChange>`. |
| `AssociationUpdated` | `AssociationEventArgs` | Same. |
| `AssociationDeleted` | `DeletedAssociationEventArgs` | `AssociationId`, `ParentEntryId`, `ChildEntryId` directly. |

### Relations (entry-to-node, entry-to-entry, node-to-node)

| Event | Args | Carries |
|---|---|---|
| `RelationUpdating` | `RelationEventArgs` | `EntryRelationChanges`, `NodeEntryRelationChanges`, `NodeRelationChanges`, plus inherited `CatalogChanges`. |
| `RelationUpdated` | `RelationEventArgs` | Same. This is what fires when a product is linked into or out of a category. |
| `RelationDeleted` | `DeletedRelationEventArgs` | `CatalogId`. The runtime object is one of three subclasses - cast to find out which relation went. |

`DeletedRelationEventArgs` subclasses, each adding its own pair:

- `DeletedEntryRelationEventArgs` - `ParentEntryId`, `ChildEntryId`
- `DeletedNodeEntryRelationEventArgs` - `NodeId`, `EntryId`
- `DeletedNodeRelationEventArgs` - `ParentNodeId`, `ChildNodeId`

## Args hierarchy

```
EventArgs
 └─ FrameworkEventArgs                    EventName
     └─ CatalogSystemEventArgs            (no members of its own)
         ├─ EntryEventArgs                Changes : IEnumerable<EntryChange>
         │   └─ DeletedEntryEventArgs     CatalogId, EntryId
         ├─ NodeEventArgs                 Changes : IEnumerable<NodeChange>, HasChangedParent
         │   └─ DeletedNodeEventArgs      CatalogId, NodeId, ParentNodeId
         ├─ AssociationEventArgs          Changes : IEnumerable<AssociationChange>
         │   └─ DeletedAssociationEventArgs  AssociationId, ParentEntryId, ChildEntryId
         └─ CatalogEventArgs              CatalogChanges : IEnumerable<CatalogChange>
             ├─ CatalogLanguageEventArgs  CatalogId
             ├─ DeletedCatalogEventArgs   CatalogId
             └─ RelationEventArgs         EntryRelationChanges, NodeEntryRelationChanges,
                 │                        NodeRelationChanges
                 └─ DeletedRelationEventArgs  CatalogId
                     ├─ DeletedEntryRelationEventArgs
                     ├─ DeletedNodeEntryRelationEventArgs
                     └─ DeletedNodeRelationEventArgs
```

**`FrameworkEventArgs` exposes only `EventName`.** No `CancelAction`, no `CancelReason`, anywhere in this tree. Catalog events are notifications; `IContentEvents` is where cancellation lives.

## Change records

`ChangeType` is `Added`, `Modified` or `Deleted` (`Mediachase.Commerce.Catalog.Events.ChangeType`). All IDs are `int`.

| Type | Members |
|---|---|
| `EntryChange` | `CatalogId`, `EntryId`, `ChangeType` |
| `NodeChange` | `CatalogId`, `NodeId`, `ParentNodeId`, `ChangeType` |
| `CatalogChange` | `CatalogId`, `ChangeType`, `LanguageConfigurationChanged` |
| `AssociationChange` | `AssociationId`, `ParentEntryId`, `ChildEntryId`, `ChangeType` |
| `EntryRelationChange` | `ParentEntryId`, `ChildEntryId`, `ChangeType` |
| `NodeEntryRelationChange` | `CatalogId`, `NodeId`, `EntryId`, `ChangeType` |
| `NodeRelationChange` | `CatalogId`, `ParentNodeId`, `ChildNodeId`, `ChangeType` |

All of them are **publicly constructible**, which is what makes catalog handlers unit testable without mocking content. Note that `changeType` comes first in every signature and `catalogId` last:

```csharp
new EntryChange(ChangeType changeType, int entryId, int catalogId)
new NodeChange(ChangeType changeType, int nodeId, int parentNodeId, int catalogId)
new CatalogChange(ChangeType changeType, int catalogId, bool languageConfigurationChanged)
new AssociationChange(ChangeType changeType, int parentEntryId, int childEntryId, int associationId)
new EntryRelationChange(ChangeType changeType, int parentEntryId, int childEntryId)
new NodeEntryRelationChange(ChangeType changeType, int nodeId, int entryId, int catalogId)
new NodeRelationChange(ChangeType changeType, int parentNodeId, int childNodeId, int catalogId)
```

The args classes are constructible too - `new EntryEventArgs(string eventName, IEnumerable<EntryChange> changes)`, `new DeletedEntryEventArgs(int entryId, int catalogId)` or `new DeletedEntryEventArgs(EntryChange change)` - so a listener's methods can be called directly from a test.

## `CatalogEventListenerBase`

`Mediachase.Commerce.Catalog.Events.CatalogEventListenerBase` - abstract class with a virtual method per event, same sixteen names, each `(object source, TArgs args)`. Override only what you need.

```
EntryUpdating / EntryUpdated / EntryDeleted
NodeUpdating / NodeUpdated / NodeDeleted
CatalogUpdating / CatalogUpdated / CatalogDeleted / CatalogLanguageUpdated
AssociationUpdating / AssociationUpdated / AssociationDeleted
RelationUpdating / RelationUpdated / RelationDeleted
```

Register with `[ServiceConfiguration(typeof(CatalogEventListenerBase))]`. Optimizely's own implementations and how they register:

| Class | Registered as |
|---|---|
| `Mediachase.Commerce.Catalog.Loggers.CatalogLogger` | `CatalogEventListenerBase` |
| `EPiServer.Commerce.Catalog.Provider.CatalogEventHandler` | `CatalogEventListenerBase` **and** itself |
| `Mediachase.Commerce.Catalog.Events.CatalogEventBroadcaster` | itself; takes `ICatalogEvents` in its constructor |

## Turning catalog IDs into content

`Mediachase.Commerce.Catalog.ReferenceConverter`:

```
GetContentLink(int objectId, CatalogContentType contentType, int versionId)
GetContentLink(string code)                GetContentLink(string code, CatalogContentType type)
GetContentLinks(IEnumerable<string> codes) GetContentLinks(IEnumerable<string> codes, CatalogContentType type)
GetCode(ContentReference contentLink)      GetCodes(IEnumerable<ContentReference>, CatalogContentType)
GetContentType(ContentReference)           GetObjectId(ContentReference)
GetRootLink()
```

`CatalogContentType`: `CatalogEntry`, `CatalogNode`, `Catalog`, `Root`. Pass `versionId: 0` for the current version.

## `CatalogEventBroadcaster` - cross-instance

`Mediachase.Commerce.Catalog.Events.CatalogEventBroadcaster` re-raises catalog changes over Framework Events. Subscribe with `Event.Get(guid).Raised`.

Public fields: `CommerceProductUpdated`, `EventRaiserId`, and the event-type strings `CatalogUpdatedEventType`, `CatalogDeletedEventType`, `CatalogLanguageUpdatedEventType`, `CatalogNodeUpdatedEventType`, `CatalogNodeDeletedEventType`, `CatalogEntryUpdatedEventType`, `CatalogEntryDeletedEventType`, `AssociationUpdatedEventType`, `AssociationDeletedEventType`, `RelationUpdatedEventType`, `RelationDeletedEventType`, `LanguageConfigurationChangedEventType`.

Payload type `CatalogContentUpdateEventArgs`: `EventType` (string, one of the above), `CatalogIds`, `CatalogNodeIds`, `CatalogEntryIds`, `CatalogAssociationIds`, `HasChangedParent`, `ApplicationHasContentModelTypes`.

The `BinaryFormatter`-based deserialization in `.claude/knowledge-base/epi-commerce-recipes.md` predates .NET 8 and throws there. See SKILL.md Step 7.

## `CatalogKeyEventBroadcaster` - price and inventory

`Mediachase.Commerce.Engine.Events.CatalogKeyEventBroadcaster`. Public fields `CatalogKeyEventGuid`, `EventRaiserId`.

| Event | Args | Carries |
|---|---|---|
| `PriceUpdated` | `PriceUpdateEventArgs : CatalogKeyEventArgs` | `CatalogKeys`, `Name` |
| `InventoryUpdated` | `InventoryUpdateEventArgs : CatalogKeyEventArgs` | `CatalogKeys`, `Name`, `InventoryChanges` |
| `CatalogMetaDataUpdated` | `CatalogKeyEventArgs` | `CatalogKeys`, `Name` |

- `CatalogKey` - `CatalogEntryCode` (string). Codes, not IDs, unlike `ICatalogEvents`.
- `InventoryChange : InventoryKey` - `CatalogEntryCode`, `WarehouseCode`, plus `PurchaseAvailableChange`, `PurchaseRequestedChange`, `BackorderAvailableChange`, `BackorderRequestedChange`, `PreorderAvailableChange`, `PreorderRequestedChange` (all `decimal`).

Find.Commerce already listens here and reindexes. Inventory fires on every stock movement, checkout included.

## Legacy, do not use

Per `.claude/rules/commerce.md`:

- `Mediachase.Commerce.Catalog.EventContext` - the pre-DI raiser, with `Raise*Event` methods and nested handler delegates. Use `ICatalogEvents`.
- `Mediachase.Commerce.Orders.OrderContext.OrderGroupUpdated` / `OrderGroupDeleted` - use `IOrderEvents`.

## Related in this repo

| File | Bus | Does |
|---|---|---|
| `Wisbar.Web/Infrastructure/CommerceInitialization.cs` | none yet | Commerce routing, catalog access rights, personalization widgets. The place a catalog subscription would go. |
| `Wisbar.Business/Features/Search/FindIndexingConventionsModule.cs` | `IContentEvents.PublishedContent` | Reindexes parent products when a variant is published. |
| `Wisbar.Business/Synchronization/Services/ProductsImportService.cs` | writes via `IContentRepository` | So the iMIS import raises **content** events, not just catalog events. |
