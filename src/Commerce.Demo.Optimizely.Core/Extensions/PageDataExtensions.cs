using EPiServer;
using EPiServer.Core;
using EPiServer.Web.Routing;

namespace Commerce.Demo.Optimizely.Core.Extensions;

public static class PageDataExtensions
{
    public static string CreateUrl(this PageData pd)
    {
        if (pd is null)
            return string.Empty;

        //Checks if the page provided is first page below root
        if (ContentReferenceComparer.IgnoreVersion.Compare(pd.ParentLink, ContentReference.RootPage) == 0 && pd.LinkType == PageShortcutType.Shortcut)
            return UrlResolver.Current.GetUrl(UrlResolver.Current.Route(new UrlBuilder(pd.LinkURL)).ContentLink, pd.Language.Name);

        return UrlResolver.Current.GetUrl(pd.ContentLink, pd.Language.Name);
    }
}
