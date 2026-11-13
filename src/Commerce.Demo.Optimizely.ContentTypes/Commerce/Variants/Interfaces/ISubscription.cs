using EPiServer.Core;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Interfaces;

public interface ISubscription : IContentData
{
    int? SubscriptionLength { get; set; }
    int? SubscriptionRenewal { get; set; }
}