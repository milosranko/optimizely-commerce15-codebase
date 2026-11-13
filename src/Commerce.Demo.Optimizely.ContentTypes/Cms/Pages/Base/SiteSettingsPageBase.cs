using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Local;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Web;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;

public abstract class SiteSettingsPageBase : PageData, ISnippets, ICookiesConsent
{
    #region Content tab

    [Display(
        Name = "Site Name",
        GroupName = SystemTabNames.Content,
        Order = 10)]
    [CultureSpecific]
    [Required]
    public virtual string? SiteName { get; set; }

    [Display(
        Name = "Consent Cookie Settings",
        GroupName = Globals.Tabs.CookiesConsent,
        Order = 20)]
    public virtual CookiesConsentBlock? CookiesConsent { get; set; }

    #endregion

    #region Snippets tab

    [Display(
        Name = "Snippets Collection",
        GroupName = Globals.Tabs.Snippets,
        Order = 30)]
    [AllowedTypes(typeof(SnippetBlock))]
    [UIHint(Globals.SiteUIHints.Snippets)]
    public virtual ContentArea? Snippets { get; set; }

    #endregion

    #region Secrets

    [Display(
        Name = "Some API key",
        GroupName = Globals.Tabs.Secrets,
        Order = 40)]
    public virtual string? SomeApiKey { get; set; }

    #endregion

    #region Public methods

    public override void SetDefaultValues(ContentType contentType)
    {
        base.SetDefaultValues(contentType);

        base.VisibleInMenu = false;

        CookiesConsent ??= new CookiesConsentBlock();
        CookiesConsent.CookieName = "cookies-consent";
        CookiesConsent.PolicyVersion = "1.0";
    }

    #endregion
}
