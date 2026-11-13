using EPiServer.Core;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Interfaces;

public interface IEndorsedMessage : IContentData
{
    string? EndorsedMessage { get; set; }
}