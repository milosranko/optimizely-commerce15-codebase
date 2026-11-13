using EPiServer.Core;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Interfaces;

public interface IAutoSupplementation : IContentData
{
    bool IsAutoSupp { get; set; }
    string? AutoSuppMessage { get; set; }
    string? AutoSuppCode { get; set; }
}
