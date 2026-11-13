using EPiServer.Core;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;

public interface IExcludeFromSitemap : IContent
{
    bool ExcludeFromSitemap { get; set; }
}
