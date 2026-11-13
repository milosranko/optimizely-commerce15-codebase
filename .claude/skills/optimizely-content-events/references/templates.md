# Content event handler templates

Namespaces use the Wisbar layout. The repo is the authority - these are the shape. Comments are kept to two lines, per `CLAUDE.md`.

## A. The module

`Wisbar.Web/Infrastructure/CmsInitialization.cs`. One module holds all CMS content handlers; add to it rather than creating a second module that subscribes to the same service.

```csharp
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using Microsoft.Extensions.DependencyInjection;

namespace Wisbar.Web.Infrastructure;

[InitializableModule]
[ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
public class CmsInitialization : IInitializableModule
{
    private bool _eventsAttached;
    private IContentEvents? _contentEvents;

    public void Initialize(InitializationEngine context)
    {
        if (_eventsAttached)
        {
            return;
        }

        // Resolved once so Uninitialize detaches from the same instance it attached to.
        _contentEvents = context.Locate.Advanced.GetRequiredService<IContentEvents>();

        _contentEvents.CreatedContent += CreatedContent;
        _contentEvents.SavingContent += SavingContent;
        _contentEvents.PublishedContent += PublishedContent;

        _eventsAttached = true;
    }

    public void Uninitialize(InitializationEngine context)
    {
        if (!_eventsAttached || _contentEvents is null)
        {
            return;
        }

        _contentEvents.PublishedContent -= PublishedContent;
        _contentEvents.SavingContent -= SavingContent;
        _contentEvents.CreatedContent -= CreatedContent;

        _contentEvents = null;
        _eventsAttached = false;
    }
}
```

## B. Normalize on the way in (`SavingContent`)

The house pattern: type check first, delegate the decision to a pure function, assign back to `e.Content`. No second save.

```csharp
// Keep the checkbox and the multi-select in step. Applied on save so drafts store the
// reconciled pair too, and mutating e.Content lands it in the same write.
private void SavingContent(object? sender, ContentEventArgs e)
{
    if (e.Content is not SearchProductLandingPage page)
    {
        return;
    }

    var (selectAll, sortOptions) = SortOptionsExtensions.ReconcileSelectAllSortOptions(
        page.SelectAllSortOptions,
        page.SortOptions,
        PreviousSelectAllSortOptions(page));

    page.SelectAllSortOptions = selectAll;
    page.SortOptions = sortOptions;
}
```

Reading the previously stored state, when the decision needs a before/after comparison. Loading `e.Content`'s own `ContentLink` returns the stored version - the save in flight has not been written yet.

```csharp
private static bool PreviousSelectAllSortOptions(SearchProductLandingPage page)
{
    if (ContentReference.IsNullOrEmpty(page.ContentLink))
    {
        return SearchProductLandingPage.DefaultSelectAllSortOptions;
    }

    return ServiceLocator.Current.GetInstance<IContentLoader>()
        .TryGet<SearchProductLandingPage>(page.ContentLink, out var saved)
        ? saved.SelectAllSortOptions
        : SearchProductLandingPage.DefaultSelectAllSortOptions;
}
```

## C. Branch on the save action

`SavingContent` also runs inside a publish. Cast the args when the two need to behave differently.

```csharp
private void SavingContent(object? sender, ContentEventArgs e)
{
    if (e.Content is not ArticlePage article)
    {
        return;
    }

    var publishing = e is SaveContentEventArgs save && save.Action.HasFlag(SaveAction.Publish);

    article.LastReviewed = publishing ? DateTime.UtcNow : article.LastReviewed;
}
```

## D. Block an editor action (`PublishingContent`)

Cancellation is `CancelAction` plus a reason the editor can act on. Never throw to stop a publish.

```csharp
// Publishing is the last point where the page can still be stopped; SavingContent would
// also block drafts, which editors need to be able to keep.
private void PublishingContent(object? sender, ContentEventArgs e)
{
    if (e.Content is not ClassifiedAdPage ad)
    {
        return;
    }

    if (ContentReference.IsNullOrEmpty(ad.ContactPage))
    {
        e.CancelAction = true;
        e.CancelReason = "Select a contact page before publishing this ad.";
    }
}
```

## E. React after the fact (`PublishedContent`)

Cache invalidation is the canonical `-ed` handler. The decorator holds the eviction method, so resolve it by type.

```csharp
private void PublishedContent(object? sender, ContentEventArgs e)
{
    if (e.Content is not (SiteSettingsPageBase or MascotImageBlock or AlertBannerBlock or SnippetBlock))
    {
        return;
    }

    ServiceLocator.Current.GetAllInstances<ISiteSettingsManager>()
        .OfType<SiteSettingsManagerCacheDecorator>()
        .SingleOrDefault()?
        .RemoveFromCache();
}
```

When the `-ed` work can fail, contain it. The operation has already committed; an escaping exception reports a failure that did not happen.

```csharp
private void PublishedContent(object? sender, ContentEventArgs e)
{
    if (e.Content is not VariationContent variant)
    {
        return;
    }

    var logger = ServiceLocator.Current.GetInstance<ILogger<CmsInitialization>>();

    try
    {
        Reindex(variant);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to reindex after variant {VariantCode} was published.", variant.Code);
    }
}
```

## F. Catch an editor deleting from the tree

Deleting in the editor is a move to the wastebasket. `DeletingContent` only fires on permanent deletion.

```csharp
private void MovingContent(object? sender, ContentEventArgs e)
{
    if (e.TargetLink?.CompareToIgnoreWorkID(ContentReference.WasteBasket) != true)
    {
        return;
    }

    if (e.Content is not ClassifiedAdPage ad)
    {
        return;
    }

    // Archive before the editor loses sight of it; permanent deletion may never happen.
    ServiceLocator.Current.GetInstance<IAdArchiveService>().Archive(ad.ContentLink);
}
```

## G. Scoped services from a handler that can run outside a request

Content events fire from scheduled jobs, imports and migrations as well as from the editor. Most feature services here are `AddScoped`, and a root-provider resolve of a scoped service throws in Development.

```csharp
private void PublishedContent(object? sender, ContentEventArgs e)
{
    if (e.Content is not ProductBase product)
    {
        return;
    }

    using var scope = ServiceLocator.Current
        .GetInstance<IServiceScopeFactory>()
        .CreateScope();

    scope.ServiceProvider.GetRequiredService<IProductCacheInvalidator>().Invalidate(product.Code);
}
```

## H. Passing state from `-ing` to `-ed`

`e.Items` is per-operation and shared between the two handlers. A field on the module is not - the module is a singleton shared across every request and every thread.

```csharp
private const string PreviousNameKey = "Wisbar.PreviousName";

private void PublishingContent(object? sender, ContentEventArgs e)
{
    if (e.Content is ProductBase product)
    {
        e.Items[PreviousNameKey] = product.DisplayName;
    }
}

private void PublishedContent(object? sender, ContentEventArgs e)
{
    if (e.Content is not ProductBase product ||
        e.Items[PreviousNameKey] is not string previousName ||
        previousName == product.DisplayName)
    {
        return;
    }

    // ...name actually changed
}
```

## I. Writing content from a `-ed` handler, when there is no alternative

Re-entrant by construction: the save raises the same events again. Only do this when the work cannot move to a `-ing` handler, and always guard on an actual change.

```csharp
private void PublishedContent(object? sender, ContentEventArgs e)
{
    if (e.Content is not ArticlePage article || article.Slug == Slugify(article.Name))
    {
        return;
    }

    // No generic overload in CMS 12 - CreateWritableClone() returns ContentData, so cast.
    var clone = (ArticlePage)article.CreateWritableClone();
    clone.Slug = Slugify(article.Name);

    ServiceLocator.Current.GetInstance<IContentRepository>()
        .Save(clone, SaveAction.Patch | SaveAction.SkipValidation, AccessLevel.NoAccess);
}
```

## J. A separate module for a feature-owned handler

When the handler belongs to a feature rather than to site-wide CMS wiring, it can own its module in `Wisbar.Business` - `FindIndexingConventionsModule` is the precedent. Same attach/detach rules.

```csharp
[InitializableModule]
[ModuleDependency(typeof(EPiServer.Find.Cms.Module.IndexingModule))]
public class ProductIndexingModule : IInitializableModule
{
    private IContentEvents? _contentEvents;

    public void Initialize(InitializationEngine context)
    {
        ConfigureConventions(context);

        _contentEvents = context.Locate.Advanced.GetRequiredService<IContentEvents>();
        _contentEvents.PublishedContent += OnVariantPublished;
    }

    public void Uninitialize(InitializationEngine context)
    {
        if (_contentEvents is null)
        {
            return;
        }

        _contentEvents.PublishedContent -= OnVariantPublished;
        _contentEvents = null;
    }
}
```

The shipped `FindIndexingConventionsModule` resolves `IContentEvents` separately in each method rather than holding the field. That works because the service is a singleton; prefer the field in new code.

## K. Order events (`IOrderEvents`), for contrast

Not a content event - carts and orders are not IContent. Same module lifecycle.

```csharp
[InitializableModule]
[ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
public class OrderEventsInitialization : IInitializableModule
{
    private IOrderEvents? _orderEvents;

    public void Initialize(InitializationEngine context)
    {
        _orderEvents = context.Locate.Advanced.GetRequiredService<IOrderEvents>();
        _orderEvents.SavedOrder += OnSavedOrder;
    }

    public void Uninitialize(InitializationEngine context)
    {
        if (_orderEvents is null)
        {
            return;
        }

        _orderEvents.SavedOrder -= OnSavedOrder;
        _orderEvents = null;
    }

    private void OnSavedOrder(object? sender, OrderEventArgs e)
    {
        if (e.OrderGroup is not IPurchaseOrder order)
        {
            return;
        }

        // ...
    }
}
```

## L. Testing the decision, not the handler

The handler itself is deliberately untestable shell - it resolves services off `ServiceLocator` and takes framework event args. Put the logic in a static function with no Optimizely types and test that, as `SortOptionsExtensionsTests` does.

```csharp
[TestMethod]
public void Reconcile_WhenCheckboxJustTicked_SelectsEverySort()
{
    var (selectAll, sortOptions) = SortOptionsExtensions.ReconcileSelectAllSortOptions(
        selectAll: true,
        sortOptions: "1,2",
        previousSelectAll: false);

    Assert.IsTrue(selectAll);
    Assert.AreEqual(SortOptionsExtensions.AllSortOptionsCsv, sortOptions);
}
```
