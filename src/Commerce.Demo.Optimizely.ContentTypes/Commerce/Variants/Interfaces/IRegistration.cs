using EPiServer.Core;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Interfaces;

public interface IRegistration : IContentData
{
    bool RequiresRegistration { get; set; }
    string? RegistrationQuestions { get; set; }
}