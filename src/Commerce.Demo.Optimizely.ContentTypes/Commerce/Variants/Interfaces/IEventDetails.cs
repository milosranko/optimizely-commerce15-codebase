using EPiServer.Core;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Interfaces;

public interface IEventDetails : IContentData
{
    DateTime? StartDateTime { get; set; }
    DateTime? EndDateTime { get; set; }
    string? LocationName { get; set; }
    string? LocationAddress { get; set; }
    string? City { get; set; }
}