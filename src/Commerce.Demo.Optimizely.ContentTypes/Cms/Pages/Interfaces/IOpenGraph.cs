using EPiServer.Core;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;

public interface IOpenGraph
{
    string? OpenGraphTitle { get; set; }
    string? OpenGraphDescription { get; set; }
    ContentReference? OpenGraphImage { get; set; }
}
