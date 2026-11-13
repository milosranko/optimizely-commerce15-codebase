using EPiServer.Core;
using EPiServer.Data.Entity;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;

public interface ISnippets : IContent, IReadOnly
{
    ContentArea? Snippets { get; set; }
}
