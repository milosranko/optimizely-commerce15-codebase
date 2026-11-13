using Commerce.Demo.Models.ViewModels;

namespace Commerce.Demo.Web.Models.Layouts;

public record SiteSettingsViewModel
{
    public string? SiteName { get; init; }
    public CookiesConsentBlockViewModel? CookiesConsent { get; init; }
    public IEnumerable<SnippetBlockViewModel>? Snippets { get; init; }
}
