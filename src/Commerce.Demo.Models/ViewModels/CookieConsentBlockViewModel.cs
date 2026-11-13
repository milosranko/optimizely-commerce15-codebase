using Commerce.Demo.Web.Models;
using EPiServer.Core;

namespace Commerce.Demo.Models.ViewModels;

public record CookiesConsentBlockViewModel(
    string? CookieName,
    DateTime? CookieExpirationDate,
    XhtmlString? DialogContent,
    bool HideCookiesConsent,
    bool ShowCookieTypeSelection,
    string? PolicyVersion,
    IEnumerable<CookieItemBlockViewModel>? Cookies);
