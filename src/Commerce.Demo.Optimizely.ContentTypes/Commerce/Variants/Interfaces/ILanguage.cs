using EPiServer.Core;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Interfaces;

public interface ILanguage : IContentData
{
    string? LanguageName { get; set; }
}