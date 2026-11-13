using Commerce.Demo.Optimizely.ContentTypes.Commerce.Properties;
using EPiServer.Core;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Interfaces;

public interface ICredits : IContentData
{
    IList<EventCreditProperty>? Credits { get; set; }
}