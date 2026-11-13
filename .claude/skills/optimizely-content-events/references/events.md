# Event catalogue

Verified against the assemblies this solution actually builds against: `EPiServer.CMS.Core 12.24.0`, `EPiServer.Commerce.Core 14.45.5` (package references `EPiServer.CMS 12.34.5`, `EPiServer.Commerce 14.45.5`). Names and argument types below were reflected out of those binaries, not remembered.

## `EPiServer.Core.IContentEvents` - all 34 events

Resolve with `ServiceLocator.Current.GetInstance<IContentEvents>()` or `context.Locate.Advanced.GetRequiredService<IContentEvents>()`.

Handler signature is `EventHandler<ContentEventArgs>` unless the Args column says otherwise. The runtime object can be a *derived* args type even where the interface declares `ContentEventArgs` - cast with `as` and handle null.

### Create

| Event | Args | Fires |
|---|---|---|
| `CreatingContent` | `ContentEventArgs` | Before new content is created. Cancellable. |
| `CreatedContent` | `ContentEventArgs` | After new content is created. The repo seeds reCAPTCHA keys onto `RecaptchaElementBlock` here. |
| `CreatingContentLanguage` | `ContentEventArgs` | Before a new language branch is created. Cancellable. |
| `CreatedContentLanguage` | `ContentEventArgs` | After a new language branch is created. |

### Load

Hot path - these fire on ordinary page views, many times per request. Anything but a trivial type check here is a site-wide performance problem.

| Event | Args | Fires |
|---|---|---|
| `LoadingContent` | `ContentEventArgs` | Before content is loaded. Cancellable. |
| `LoadedContent` | `ContentEventArgs` | After content is loaded (cache hits included). |
| `FailedLoadingContent` | `ContentEventArgs` | Load failed - content missing or access denied. |
| `LoadingDefaultContent` | `ContentEventArgs` | Before `GetDefault<T>()` builds a new, unsaved instance. |
| `LoadedDefaultContent` | `ContentEventArgs` | After `GetDefault<T>()`. The place to seed defaults an editor should see pre-filled. |
| `LoadingChildren` | `ChildrenEventHandler` | Before children are loaded. Cancellable. |
| `LoadedChildren` | `ChildrenEventHandler` | After children are loaded. `e.ChildrenItems` is settable - filtering here changes what every caller sees. |
| `FailedLoadingChildren` | `ChildrenEventHandler` | Children load failed. |

`ChildrenEventArgs` carries `ChildrenItems`, `ContentLink`, `CancelAction`, `CancelReason`, `RequiredAccess`, `Creator`, `Items` - it does **not** derive from `ContentEventArgs`, so there is no `e.Content`.

`DefaultContentEvents` also exposes `FinishedLoadingChildren`, which is **not** on the `IContentEvents` interface. Don't reach for it; it needs a cast to the implementation and is not part of the supported surface.

### Save

| Event | Args | Fires |
|---|---|---|
| `SavingContent` | `SaveContentEventArgs` | Before a version is written - including every draft save, and including the save inside a publish. Cancellable. Mutate `e.Content` here. |
| `SavedContent` | `SaveContentEventArgs` | After the version is written. |

`SaveContentEventArgs` adds `Action` and `MaskedAction` (`SaveAction`), `Transition` (`StatusTransition`), `RequireNewVersion`, `ForceCurrentVersion`. Cast to read them:

```csharp
if (e is SaveContentEventArgs save && save.Action.HasFlag(SaveAction.Publish)) { ... }
```

### Publish and schedule

| Event | Args | Fires |
|---|---|---|
| `PublishingContent` | `ContentEventArgs` | Before publish. Cancellable. The pipeline-style hook: edit `e.Content` directly, no clone or save needed. |
| `PublishedContent` | `ContentEventArgs` | After publish commits. Cache invalidation, reindexing, notifications. |
| `SchedulingContent` | `ContentEventArgs` | Before content is scheduled for delayed publish. Cancellable. |
| `ScheduledContent` | `ContentEventArgs` | After scheduling. Note this is the *scheduling* action, not the later automatic publish - that raises the publish events when it runs. |

### Approval workflow

| Event | Args | Fires |
|---|---|---|
| `RequestingApproval` | `ContentEventArgs` | Before content is submitted for approval. Cancellable. |
| `RequestedApproval` | `ContentEventArgs` | After submission. |
| `RejectingContent` | `ContentEventArgs` | Before a reviewer's rejection is applied. Cancellable. |
| `RejectedContent` | `ContentEventArgs` | After rejection. |

For the approval sequence itself (started, step approved, step commented, aborted), use `EPiServer.Approvals.IApprovalEngineEvents` instead - see below.

### Check in / check out

| Event | Args | Fires |
|---|---|---|
| `CheckingInContent` | `ContentEventArgs` | Before check-in. Cancellable. |
| `CheckedInContent` | `ContentEventArgs` | After check-in. |
| `CheckingOutContent` | `ContentEventArgs` | Before check-out. Cancellable. |
| `CheckedOutContent` | `ContentEventArgs` | After check-out. |

### Move

| Event | Args | Fires |
|---|---|---|
| `MovingContent` | `ContentEventArgs` (runtime `MoveContentEventArgs`) | Before a move. Cancellable. **This is also what an editor's "delete" raises** - the move is to `ContentReference.WasteBasket`, which you read from `e.TargetLink`. |
| `MovedContent` | `ContentEventArgs` (runtime `MoveContentEventArgs`) | After a move, including restore from wastebasket. |

`MoveContentEventArgs` adds `OriginalParent`, `OriginalContentLink`, `Descendents`. A move fires once for the moved item, not once per descendant - walk `Descendents` when the subtree matters.

### Delete

| Event | Args | Fires |
|---|---|---|
| `DeletingContent` | `DeleteContentEventArgs` | Before **permanent** deletion (emptying the wastebasket, `Delete`, `DeleteChildren`). Cancellable. Last chance to read the content. |
| `DeletedContent` | `DeleteContentEventArgs` | After permanent deletion. The content is gone; only `e.ContentLink` and `e.DeletedDescendents` are usable. |
| `DeletingContentVersion` | `ContentEventArgs` | Before a single version is deleted. Cancellable. |
| `DeletedContentVersion` | `ContentEventArgs` | After a version is deleted. |
| `DeletingContentLanguage` | `ContentEventArgs` | Before a language branch is deleted. Cancellable. |
| `DeletedContentLanguage` | `ContentEventArgs` | After a language branch is deleted. |

`DeleteContentEventArgs` adds `DeletedDescendents` (`IList<ContentReference>`).

## `EPiServer.ContentEventArgs`

```
Content         IContent          { get; set; }   // the instance in flight on -ing events
ContentLink     ContentReference  { get; set; }
TargetLink      ContentReference  { get; set; }   // move/copy destination; WasteBasket on a tree delete
CancelAction    bool              { get; set; }   // honoured on -ing events only
CancelReason    string            { get; set; }   // shown to the editor
RequiredAccess  AccessLevel       { get; set; }
Creator         object            { get; set; }
Items           IDictionary       { get; }        // per-operation scratch space, shared between handlers
```

`Items` is the supported way to pass state from an `-ing` handler to the matching `-ed` handler in the same operation - use it instead of a field on the module, which is a singleton shared across requests.

## Other CMS event services

Same pattern in every case: resolve the interface, subscribe in `Initialize`, unsubscribe in `Uninitialize`.

| Service | Namespace | Events |
|---|---|---|
| `IContentSecurityEvents` | `EPiServer.DataAbstraction` | `ContentSecuritySaving`, `ContentSecuritySaved`, `ContentSecurityDeleting`, `ContentSecurityDeleted` |
| `ICategoryEvents` | `EPiServer.DataAbstraction` | `CategoryCreated`, `CategoryUpdated`, `CategoryDeleted` |
| `ISiteDefinitionEvents` | `EPiServer.Web` | `SiteCreated`, `SiteUpdated`, `SiteDeleted` |
| `IApprovalEngineEvents` | `EPiServer.Approvals` | `Started`, `Approved`, `Rejected`, `Aborted`, `StepStarted`, `StepApproved`, `StepRejected`, `StepCommented` |
| `IScheduledJobEvents` | `EPiServer.DataAbstraction` | `Saved`, `Deleted`, `Executed` |
| `IPropertyDefinitionTypeEvents` | `EPiServer.DataAbstraction` | `Saving`, `Saved`, `Deleted` |
| `IContentTypeModelScannerEvents` | `EPiServer.DataAbstraction.RuntimeModel` | `SynchronizationStarted`, `SynchronizationCompleted` |
| `IActivityEvents` | `EPiServer.DataAbstraction.Activities` | `ActivityCreated`, `ActivityUpdated`, `ActivityDeleted` |
| `IContentUrlGeneratorEvents` | `EPiServer.Core.Routing` | `GeneratingUrl`, `GeneratedUrl` |
| `IContentUrlResolverEvents` | `EPiServer.Core.Routing` | `ResolvingUrl`, `ResolvedUrl` |
| `IContentRouteEvents` | `EPiServer.Web.Routing` | `RoutingContent`, `RoutedContent`, `CreatingVirtualPath`, `CreatedVirtualPath` |
| `ITemplateResolverEvents` | `EPiServer.Web` | `TemplateResolving`, `TemplateResolved` |
| `IVisitorGroupEvents` | `EPiServer.Personalization.VisitorGroups` | `SavingVisitorGroup`, `SavedVisitorGroup`, `DeletingVisitorGroup`, `DeletedVisitorGroup` |
| `IDataImportEvents` | `EPiServer.Enterprise` | `Starting`, `Completed`, `ContentImporting`, `ContentImported`, `PropertyImporting`, `BlobImporting`, `BlobImported` |
| `IDataExportEvents` | `EPiServer.Enterprise` | `Starting`, `ContentExporting`, `ContentExported`, `PropertyExporting`, `BlobExporting`, `BlobExported` |
| `INotifier` | `EPiServer.Notification` | `NotificationSaved`, `NotificationPosted`, `NotificationFiltered` |
| `ProjectRepository` | `EPiServer.DataAbstraction` | `ProjectSaved`, `ProjectDeleted`, `ProjectItemsSaved`, `ProjectItemsDeleted` (concrete class, not an interface) |
| `ContentTypeRepository` | `EPiServer.DataAbstraction` | `ContentTypeSaved`, `ContentTypeDeleted` (concrete class) |
| `FormsEvents` | `EPiServer.Forms.Core.Events` | `FormsSubmitting`, `FormsStepSubmitted`, `FormsSubmissionFinalized`, `FormsStructureChange` |

## Commerce event buses

| Service | Namespace | Events |
|---|---|---|
| `IOrderEvents` | `EPiServer.Commerce.Order` | `SavingOrder`, `SavedOrder`, `DeletingOrder`, `DeletedOrder`. `OrderEventArgs` gives `OrderGroup` (`IOrderGroup`) and `OrderLink` (`OrderReference`). Fires only for saves through `IOrderRepository`. |
| `ICatalogEvents` | `Mediachase.Commerce.Catalog.Events` | `EntryUpdating/Updated/Deleted`, `NodeUpdating/Updated/Deleted`, `CatalogUpdating/Updated/Deleted`, `CatalogLanguageUpdated`, `AssociationUpdating/Updated/Deleted`, `RelationUpdating/Updated/Deleted`. DTO layer - catches ServiceAPI and import/export, which `IContentEvents` misses. |
| `CatalogKeyEventBroadcaster` | `Mediachase.Commerce.Engine.Events` | `PriceUpdated`, `InventoryUpdated`, `CatalogMetaDataUpdated`. Find.Commerce already listens and reindexes; inventory fires very frequently. |
| `CatalogEventBroadcaster` | `Mediachase.Commerce.Catalog.Events` | Re-broadcasts catalog events over Framework Events (Service Bus on DXP). Subscribe with `Event.Get(CatalogEventBroadcaster.CommerceProductUpdated).Raised`. |
| `MetaDataEventManager` | `EPiServer.Commerce` | `MetaFieldDataUpdated`, `MetaFieldDataLinked`, `MetaFieldDataUnlinked` |

Legacy, do not use in new code (`.claude/rules/commerce.md`): `Mediachase.Commerce.Orders.OrderContext.OrderGroupUpdated`/`OrderGroupDeleted` and `Mediachase.Commerce.Catalog.EventContext`. Use `IOrderEvents` and `ICatalogEvents`.

Deeper background on the Commerce buses: `.claude/knowledge-base/pro-optimizely-commerce-cloud.md` (Part 4, chapters 8-9) and `.claude/knowledge-base/epi-commerce-recipes.md`.

## Existing subscribers in this repo

Know these before adding another - the event you want may already be handled.

| File | Event | Does |
|---|---|---|
| `Wisbar.Web/Infrastructure/CmsInitialization.cs` | `CreatedContent` | Seeds reCAPTCHA keys on `RecaptchaElementBlock` |
| `Wisbar.Web/Infrastructure/CmsInitialization.cs` | `SavingContent` | Reconciles "Select All Sort Options" on `SearchProductLandingPage` |
| `Wisbar.Web/Infrastructure/CmsInitialization.cs` | `PublishedContent` | Evicts the Site Settings cache for `SiteSettingsPageBase`, `MascotImageBlock`, `AlertBannerBlock`, `SnippetBlock` |
| `Wisbar.Business/Features/Search/FindIndexingConventionsModule.cs` | `PublishedContent` | Reindexes parent products when a `VariationContent` is published |
