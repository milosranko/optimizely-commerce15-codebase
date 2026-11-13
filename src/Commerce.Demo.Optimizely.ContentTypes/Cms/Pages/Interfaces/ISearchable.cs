using EPiServer.Core;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;

public interface ISearchable : IContent
{
    bool ExcludeFromSearchResults { get; set; }
    string? SearchSummaryText { get; set; }
}
