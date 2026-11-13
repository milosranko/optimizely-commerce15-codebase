using EPiServer.Core;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;

public interface IListable : IContent
{
    int PageSize { get; set; }
}
