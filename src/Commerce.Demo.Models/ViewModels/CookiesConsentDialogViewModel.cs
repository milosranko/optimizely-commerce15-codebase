using Commerce.Demo.Web.Models;
using EPiServer.Core;

namespace Commerce.Demo.Models.ViewModels;

public record CookiesConsentDialogViewModel()
{
    public string? CookieName { get; init; }
    public DateTime? CookieExpirationDate { get; init; }
    public XhtmlString? DialogContent { get; init; }
    public bool HideCookiesConsent { get; init; }
    public bool ShowCookieTypeSelection { get; init; }
    public string? PolicyVersion { get; init; }
    public IList<CookieItemBlockViewModel> Cookies { get; set; } = [];
}
