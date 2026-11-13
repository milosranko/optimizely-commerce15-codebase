using EPiServer.Core;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;

namespace Commerce.Demo.Optimizely.Core.Extensions;

public static class ContentAreaExtensions
{
    /// <summary>
    /// Replacement for <c>ContentArea.FilteredItems</c>, removed in CMS 13: applies the registered
    /// <see cref="IContentAreaItemsRenderingFilter"/>s (publish status, access rights, personalization)
    /// for the current principal and context mode. Accepts <c>null</c> and returns an empty list then.
    /// </summary>
    public static IList<ContentAreaItem> GetFilteredItems(this ContentArea? contentArea)
    {
        var items = contentArea?.Items?.ToList() ?? [];
        if (items.Count == 0)
        {
            return items;
        }

        var locator = ServiceLocator.Current;
        var principal = locator.GetInstance<IPrincipalAccessor>().Principal;
        var contextMode = locator.GetInstance<IContextModeResolver>().CurrentMode;

        foreach (var filter in locator.GetAllInstances<IContentAreaItemsRenderingFilter>())
        {
            filter.FilterAsync(items, principal, contextMode).GetAwaiter().GetResult();
        }

        return items;
    }
}
