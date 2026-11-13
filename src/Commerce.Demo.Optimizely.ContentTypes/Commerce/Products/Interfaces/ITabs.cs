using Commerce.Demo.Optimizely.ContentTypes.Commerce.Properties;
using EPiServer.Core;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Products.Interfaces;

public interface ITabs : IContentData
{
    IList<LabelXhtmlProperty>? Tabs { get; set; }
}
