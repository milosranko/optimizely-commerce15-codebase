using EPiServer;
using EPiServer.Core;
using System.Globalization;

namespace Commerce.Demo.Optimizely.Core.Extensions;

public static class ContentLoaderExtensions
{
    public static IEnumerable<IContent> GetAncestors(this IContentLoader contentLoader, ContentReference contentLink, CultureInfo language)
    {
        var ancestorRefs = contentLoader
            .GetAncestors(contentLink)
            .Select(x => x.ContentLink);

        return contentLoader.GetItems(ancestorRefs, language);
    }
}
