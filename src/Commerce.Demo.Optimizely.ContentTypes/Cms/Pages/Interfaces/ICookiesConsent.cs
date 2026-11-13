using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Local;
using EPiServer.Core;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;

public interface ICookiesConsent : IContent
{
    CookiesConsentBlock? CookiesConsent { get; set; }
}
