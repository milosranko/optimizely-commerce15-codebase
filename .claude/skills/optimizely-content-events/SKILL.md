---
name: optimizely-content-events
description: Add or change a CMS content event handler in an Optimizely CMS 12 (.NET) site - an [InitializableModule] that subscribes to IContentEvents (saving, publishing, moving, deleting, loading, approval, check-in) and detaches again in Uninitialize. Covers the full event list and which one to pick, attach/detach symmetry, resolving services without constructor injection, decorator and scoped-service resolution, filtering by content type, mutating e.Content in place versus the save loop a post-event causes, cancelling an operation with CancelAction, and the neighbouring buses (IOrderEvents, ICatalogEvents, CatalogKeyEventBroadcaster) for work IContentEvents never sees. Use whenever asked to run code when content is saved, published, moved, deleted or created, to validate or block an editor action, to invalidate a cache on publish, or when a handler fires twice, never fires, loops, or throws inside the editor.
---

# Adding a CMS content event handler

A content event handler is a method attached to an `IContentEvents` event from an `[InitializableModule]`, and detached again in `Uninitialize`. It runs in-process on the server performing the operation, inside the same call that is saving or publishing the content.

This repo's module is `src/Wisbar.Web/Infrastructure/CmsInitialization.cs`. Read it before writing anything - a new handler usually belongs in it rather than in a new module.

## Step 0: Is `IContentEvents` the right bus?

`IContentEvents` fires only for operations that go through `IContentRepository`. Work that bypasses it raises nothing here.

| You want to react to | Bus | Notes |
|---|---|---|
| Page, block, media, product or variant saved/published/moved/deleted | `IContentEvents` | This skill. Catalog content is CMS content, so products and variants come through here too. |
| Catalog changes made by ServiceAPI, import/export or `ICatalogSystem` | `ICatalogEvents` / `CatalogEventListenerBase` | Lower, DTO-level layer. `IContentEvents` never sees these. |
| Price or inventory changes | `CatalogKeyEventBroadcaster` | `PriceUpdated`, `InventoryUpdated`, `CatalogMetaDataUpdated`. Inventory fires very often. |
| Cart or order saved/deleted | `IOrderEvents` | Only when saved through `IOrderRepository`. Never `OrderContext` - see `.claude/rules/commerce.md`. |
| Access rights, categories, site definitions, projects, approvals, scheduled jobs | Their own `I*Events` services | Full list in `references/events.md`. |

Cross-server note: these events are raised on the instance that performed the operation, not on every DXP instance. Anything that has to take effect site-wide must be flushed through something that broadcasts - in this repo `CacheService.Remove` (which calls `CacheManager.Remove`, the synchronized removal) rather than `Clear`/`RemoveLocalOnly`, which are local only.

## Step 1: Pick the exact event

`references/events.md` lists all 34 `IContentEvents` events with their argument types and when each one actually fires. The five that cover most work:

| Intent | Event | Why |
|---|---|---|
| Normalize or reconcile property values on the way in | `SavingContent` | Mutating `e.Content` lands in the same write - no second save. Applies to drafts too. |
| Validate and block an editor action | `PublishingContent` (or `SavingContent`) | Only `-ing` events can cancel. |
| React after content is live - cache flush, reindex, notify | `PublishedContent` | The published version is committed by now. |
| Seed defaults on a brand-new, unsaved item | `LoadedDefaultContent` | Fires for `GetDefault<T>()`, before the editor sees the form. |
| Clean up things keyed to content that is going away | `DeletingContent` | At `DeletedContent` the content is already gone. |

Two traps worth naming up front:

- **Moving to the wastebasket is a move, not a delete.** `DeletingContent` fires on permanent deletion, not when an editor deletes from the tree. To catch that, handle `MovingContent` and check `e.TargetLink` against `ContentReference.WasteBasket`.
- **`-ing` and `-ed` are not interchangeable.** `-ing` runs before the write and can cancel or edit in place. `-ed` runs after, so any change you make there needs a second, explicit save - see Step 5.

## Step 2: Register and unregister symmetrically

In `CmsInitialization`, attach in `Initialize`, detach in `Uninitialize` in reverse order, and guard both with the `_eventsAttached` flag that is already there (the engine can call `Initialize` more than once).

Resolve `IContentEvents` **once into a field** and use that same instance to detach. Three separate `ServiceLocator.Current.GetInstance<IContentEvents>()` calls, as the current file has, only unsubscribe correctly because the service happens to be a singleton; one field removes the assumption:

```csharp
private IContentEvents? _contentEvents;

public void Initialize(InitializationEngine context)
{
    if (_eventsAttached) return;
    _contentEvents = context.Locate.Advanced.GetRequiredService<IContentEvents>();
    _contentEvents.PublishedContent += PublishedContent;
    _eventsAttached = true;
}
```

Add `[ModuleDependency(typeof(EPiServer.Web.InitializationModule))]` when the module needs CMS initialization to have finished (Commerce work uses `EPiServer.Commerce.Initialization.InitializationModule`; `FindIndexingConventionsModule` depends on `EPiServer.Find.Cms.Module.IndexingModule`). Never leave a subscription without its matching removal - a missing detach is what leaves handlers doubled after a re-initialize.

## Step 3: Resolve services without constructor injection

An `IInitializableModule` is constructed by the framework, so there is no DI through the constructor.

- **In `Initialize`:** `context.Locate.Advanced.GetRequiredService<T>()`. Use this for anything you hold in a field.
- **Inside a handler:** `ServiceLocator.Current.GetInstance<T>()` at the point of use. Don't cache scoped or per-request services in a field on the module - the module is a singleton and outlives every request.
- **Decorated services:** the interface resolves to the outermost decorator, and the method you want may exist only on the decorator. That is why the existing `PublishedContent` handler does

  ```csharp
  ServiceLocator.Current.GetAllInstances<ISiteSettingsManager>()
      .OfType<SiteSettingsManagerCacheDecorator>()
      .SingleOrDefault()?.RemoveFromCache();
  ```

  `RemoveFromCache` is on `CacheDecoratorBase`, not on `ISiteSettingsManager`. The `OfType`/`SingleOrDefault` form degrades to a no-op if the decorator is ever unregistered, where a hard cast would throw inside a publish.
- **Scoped services outside a request:** content events also fire from scheduled jobs, imports and migrations, where there is no request scope. `ISiteSettingsManager`, `ICacheService` and most feature services in this repo are `AddScoped`, and resolving a scoped service from the root provider throws in Development, where scope validation is on. If the handler can run outside a request, create a scope with `IServiceScopeFactory` (or hand the work to `IOutOfRequestExecutor`, `Wisbar.Business/Infrastructure/Execution/`) instead of resolving straight off `ServiceLocator.Current`.

## Step 4: Filter for the content type first, and bail early

Every handler is called for **every** piece of content on the site - pages, blocks, media, products, variants, every save of every draft. The type check is the first statement:

```csharp
private void SavingContent(object? sender, ContentEventArgs e)
{
    if (e.Content is not SearchProductLandingPage page)
    {
        return;
    }
    ...
}
```

- Match on the narrowest type that is correct. Matching a base page type or an interface (`SiteSettingsPageBase`, `ProductBase`) catches derived types too, which is usually what you want for cache invalidation and rarely what you want for validation.
- `e.Content` can be null on some events; the pattern match above handles that for free.
- Don't call `IContentLoader.Get` on `e.ContentLink` to inspect the content being saved - that returns the *stored* version, not the one in flight. That difference is deliberately exploited in `PreviousSelectAllSortOptions`, which loads the saved copy precisely to compare it against the incoming one.

## Step 5: Mutate in place, don't save inside a save

In a `-ing` handler, assign to `e.Content`'s properties directly. The object is the one about to be written, so the change is part of the same save. This is the whole reason the sort-options reconciliation lives in `SavingContent` and not in `PublishedContent`.

In a `-ed` handler, `e.Content` is committed content. Writing to it does nothing useful, and calling `IContentRepository.Save` on it re-enters the same event - a publish loop that only stops when something else breaks. If you genuinely must write after the fact:

```csharp
var clone = (SearchProductLandingPage)e.Content.CreateWritableClone();
clone.Something = value;
_contentRepository.Save(clone, SaveAction.Patch | SaveAction.SkipValidation, AccessLevel.NoAccess);
```

and guard re-entry by checking that the value actually changed before saving. Prefer restructuring the work into a `-ing` handler over doing this at all.

## Step 6: Cancelling

Only `-ing` events are cancellable, and cancellation is two properties, not an exception:

```csharp
e.CancelAction = true;
e.CancelReason = "Give the editor a sentence they can act on.";
```

Throwing instead of cancelling surfaces as a raw error in the editor and can leave a half-finished operation. `CancelReason` is shown to the editor, so write it as user-facing copy - and per `CLAUDE.md`, with hyphens, never em dashes.

## Step 7: Keep the handler fast, quiet and testable

The handler runs inside the editor's request, synchronously, holding up the save.

- **No slow work.** No HTTP calls, no catalog walks, no email. Queue it - see the `hangfire-background-jobs` skill.
- **Catch and log your own exceptions** in `-ed` handlers. An unhandled throw there aborts an operation that has already committed, and the editor sees a failure for work that succeeded. In `-ing` handlers, cancel instead of throwing (Step 6).
- **No `HttpContext` assumptions.** Imports and scheduled jobs raise these events with no request, no visitor identity and possibly no `SiteDefinition.Current`.
- **Put the decision in a pure function.** `SavingContent` delegates to `SortOptionsExtensions.ReconcileSelectAllSortOptions(...)`, a static function with no Optimizely dependencies, which is why it has real unit tests in `Wisbar.Tests/Features/Search/SortOptionsExtensionsTests.cs` while the handler itself has none. Do the same: the handler gathers state and applies the result, the function decides.
- **Two-line comment maximum** on the handler, per `CLAUDE.md`. Say why that event was chosen, not what the code does - the existing handlers are the model.

## Step 8: Verify

```bash
dotnet build src/Wisbar.sln
dotnet test src/Wisbar.Tests/Wisbar.Tests.csproj
```

A build proves nothing about editor behaviour. Report which of these you actually checked:

- The handler fires for the intended content type and returns immediately for everything else.
- Saving a draft, publishing, and publishing a second time all behave (no loop, no doubled work).
- Content that is *not* the target is unaffected - save an unrelated page and a block.
- Cancellation shows the intended message in the editor and leaves the content unchanged.
- Cache invalidation actually evicts: check that the key was cached on the instance doing the publish, since `CacheService.Remove` returns early for a key this instance never cached.
- If the event can fire from an import or scheduled job, exercise that path too - that is where missing request scope and a null `SiteDefinition.Current` show up.

## Checklist

- [ ] `IContentEvents` is the right bus (not `ICatalogEvents`, `IOrderEvents`, `CatalogKeyEventBroadcaster`)
- [ ] Event chosen deliberately from `references/events.md`; `-ing` vs `-ed` correct for the intent
- [ ] Wastebasket deletes handled as `MovingContent` if that was the intent
- [ ] Attached in `Initialize`, detached in `Uninitialize` in reverse, behind the `_eventsAttached` guard
- [ ] `IContentEvents` resolved once and reused for attach and detach
- [ ] `[ModuleDependency]` set when initialization order matters
- [ ] Type check is the first statement; everything else returns early
- [ ] `-ing` handlers mutate `e.Content` in place; no `Save` from a `-ed` handler without a re-entry guard
- [ ] Cancellation uses `CancelAction`/`CancelReason` with editor-facing copy, no em dashes
- [ ] No slow work, no `HttpContext` assumption, scoped services resolved through a scope
- [ ] Decision logic extracted to a pure function with unit tests
- [ ] Solution builds and tests pass; editor behaviour checked, not just the build

Full code templates: `references/templates.md`. Complete event catalogue: `references/events.md`.
