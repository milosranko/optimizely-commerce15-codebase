using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Shell.ObjectEditing;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Local;

[ContentType(
    DisplayName = "Cookies Consent",
    AvailableInEditMode = false,
    GUID = "{54B7C3D6-4042-4AA4-854F-59C2D9B44507}")]
public class CookiesConsentBlock : BlockData
{
    #region Content tab

    [Display(
        Name = "Cookie Name",
        GroupName = SystemTabNames.Content,
        Order = 10)]
    public virtual string? CookieName { get; set; }

    [Display(
        Name = "Cookie Expiration Date",
        GroupName = SystemTabNames.Content,
        Order = 20)]
    public virtual DateTime? CookieExpirationDate { get; set; }

    [Display(
        Name = "Policy Version",
        Description = "Update when cookie policy changes (e.g., '1.0', '2.1')",
        GroupName = SystemTabNames.Content,
        Order = 25)]
    public virtual string? PolicyVersion { get; set; }

    [Display(
        Name = "Consent Dialog Content",
        GroupName = SystemTabNames.Content,
        Order = 30)]
    [CultureSpecific]
    public virtual XhtmlString? DialogContent { get; set; }

    [Display(
        Name = "Hide Cookies Consent Dialog",
        GroupName = SystemTabNames.Content,
        Order = 40)]
    public virtual bool HideCookiesConsent { get; set; }

    [Display(
        Name = "Show Selection For Each Cookie Type",
        GroupName = SystemTabNames.Content,
        Order = 50)]
    public virtual bool ShowCookieTypeSelection { get; set; }

    [Display(
        Name = "Cookies",
        GroupName = SystemTabNames.Content,
        Order = 60)]
    [ListItemHeaderProperty(nameof(CookieBlock.Name))]
    public virtual IList<CookieBlock>? Cookies { get; set; }

    #endregion
}
