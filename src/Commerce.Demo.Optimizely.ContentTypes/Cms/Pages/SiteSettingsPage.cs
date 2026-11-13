using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Local;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;
using Commerce.Demo.Optimizely.Core.Attributes;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Authorization;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Shell.ObjectEditing;
using System.ComponentModel.DataAnnotations;
using Wisbar.Optimizely.ContentTypes.Cms.Blocks.Local;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Pages;

[ContentType(
    GUID = "{7A553D60-F2DF-4094-A650-8812F755EF6E}",
    DisplayName = "Site Settings",
    GroupName = Globals.Tabs.Specialized)]
[AvailableContentTypes(
    Availability.None,
    IncludeOn =
    [
        typeof(StartPage)
    ])]
[SiteImageUrl("sitesettings-page.png")]
[Access(Roles = Roles.WebAdmins)]
public class SiteSettingsPage : SiteSettingsPageBase
{
    #region Content tab

    [Display(
        Name = "Image Link",
        GroupName = SystemTabNames.Content,
        Order = 100)]
    public virtual ImageLinkBlock? ImageLink { get; set; }

    [Display(
        Name = "Credit Types",
        GroupName = SystemTabNames.Content,
        Order = 110)]
    [ListItemHeaderProperty(nameof(CreditTypesBlock.CreditType))]
    public virtual IList<CreditTypesBlock>? CreditTypes { get; set; }

    [Display(
        Name = "Some API key",
        GroupName = SystemTabNames.Content,
        Order = 120)]
    public virtual string? CopyrightText { get; set; }

    #endregion
}
