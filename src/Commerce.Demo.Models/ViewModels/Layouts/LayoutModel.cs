using Commerce.Demo.Models.ViewModels;

namespace Commerce.Demo.Web.Models.Layouts;

public record LayoutModel
{
    public MetaDataModel? MetaData { get; set; }
    public OpenGraphModel? OpenGraph { get; set; }
    public HeaderModelBase? Header { get; set; }
    public FooterModelBase? Footer { get; set; }
    public string? PageTitle { get; set; }
    public string? SiteName { get; set; }
    public IEnumerable<SnippetBlockViewModel>? Snippets { get; set; }
    public CookiesConsentDialogViewModel? CookiesConsent { get; set; }
}
