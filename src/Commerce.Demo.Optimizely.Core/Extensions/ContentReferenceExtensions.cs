using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;

namespace Commerce.Demo.Optimizely.Core.Extensions;

public static class ContentReferenceExtensions
{
    public static TContent Get<TContent>(this ContentReference contentLink) where TContent : IContent
    {
        return ServiceLocator.Current.GetInstance<IContentLoader>().Get<TContent>(contentLink);
    }

    public static PageData GetPage(this ContentReference pageLink)
    {
        return ServiceLocator.Current.GetInstance<IContentLoader>().Get<PageData>(pageLink);
    }

    public static T? GetBlock<T>(this ContentReference contentLink) where T : IContentData
    {
        return ServiceLocator.Current.GetInstance<IContentLoader>().TryGet<T>(contentLink, out var content) ? content : default;
    }

    public static T? GetPage<T>(this ContentReference contentLink) where T : IContentData
    {
        return ServiceLocator.Current.GetInstance<IContentLoader>().TryGet<T>(contentLink, out var content) ? content : default;
    }

    public static string GetExternalUrl(this ContentReference contentLink)
    {
        if (!ContentReference.IsNullOrEmpty(contentLink))
        {
            var internalUrl = UrlResolver.Current.GetUrl(contentLink);

            var url = new UrlBuilder(internalUrl);
            //EPiServer.Url.UrlRewriteProvider.ConvertToExternal(url, null, System.Text.Encoding.UTF8);

            var friendlyUrl = ServiceLocator.Current.GetInstance<IUriSupport>().AbsoluteUrlBySettings(url.ToString());

            return friendlyUrl;
        }

        return string.Empty;
    }

    public static string GetInternalUrl(this ContentReference contentLink, string? language = null)
    {
        if (!ContentReference.IsNullOrEmpty(contentLink))
        {
            var internalUrl = UrlResolver.Current.GetUrl(contentLink, language);
            return internalUrl;
        }

        return string.Empty;
    }
}
