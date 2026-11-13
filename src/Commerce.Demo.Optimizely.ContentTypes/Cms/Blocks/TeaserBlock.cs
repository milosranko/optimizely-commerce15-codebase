using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Base;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Local;
using Commerce.Demo.Optimizely.Core.Attributes;
using EPiServer.Authorization;
using EPiServer.Cms.Shell.UI.ObjectEditing;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Framework.DataAnnotations;
using EPiServer.Web;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks;

[Access(Roles = $"{Roles.WebAdmins}")]
[ContentType(
    GUID = "{C98C99EA-A630-49CD-8A45-5AEF47EE265D}",
    DisplayName = "Teaser Block")]
[SiteImageUrl("TeaserBlock.png")]
public class TeaserBlock : BlockBase
{
    #region Content tab

    [ReloadOnChange]
    [CultureSpecific]
    [Display(
        Name = "Heading",
        GroupName = SystemTabNames.Content,
        Order = 100)]
    [StringLength(50)]
    public virtual string? Heading { get; set; }

    [CultureSpecific]
    [Display(
        Name = "Lead Text",
        GroupName = SystemTabNames.Content,
        Order = 110)]
    [UIHint(UIHint.Textarea, PresentationLayer.Edit)]
    public virtual string? LeadText { get; set; }

    [Display(
        Name = "Link Button",
        GroupName = SystemTabNames.Content,
        Order = 120)]
    public virtual LinkBlock? LinkButton { get; set; }

    [CultureSpecific]
    [Display(
        Name = "Image",
        GroupName = SystemTabNames.Content,
        Order = 130)]
    [UIHint(UIHint.Image)]
    public virtual ContentReference? Image { get; set; }

    #endregion
}
