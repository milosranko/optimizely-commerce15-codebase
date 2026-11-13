using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Blocks;
using Commerce.Demo.Optimizely.Core.Attributes;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Framework.DataAnnotations;
using EPiServer.Shell.ObjectEditing;
using EPiServer.SpecializedProperties;
using EPiServer.Web;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Pages;

[ContentType(
    GUID = "{E6F693A7-1436-49CC-B7F0-D7555D530DD6}",
    DisplayName = "Start Page",
    GroupName = Globals.Tabs.Specialized)]
[SiteImageUrl("start-page.png")]
public class StartPage : StartPageBase
{
    #region Content tab

    [CultureSpecific]
    [Display(
        Name = "Heading",
        GroupName = SystemTabNames.Content,
        Order = 100)]
    [Required]
    public virtual string? Heading { get; set; }

    [CultureSpecific]
    [Display(
        Name = "Lead Text",
        GroupName = SystemTabNames.Content,
        Order = 110)]
    [UIHint(UIHint.Textarea, PresentationLayer.Edit)]
    public virtual string? LeadText { get; set; }

    [CultureSpecific]
    [Display(
        Name = "Main Content Area",
        GroupName = SystemTabNames.Content,
        Order = 120)]
    [AllowedTypes([
        typeof(TeaserBlock),
        typeof(CarouselContainerBlock)
    ])]
    public virtual ContentArea? MainContentArea { get; set; }

    [CultureSpecific]
    [Display(
        Name = "Another Main Content Area",
        GroupName = SystemTabNames.Content,
        Order = 130)]
    public virtual ContentArea? AnotherMainContentArea { get; set; }

    [Display(
        Name = "Label Text List",
        GroupName = SystemTabNames.Content,
        Order = 140)]
    [ListItemHeaderProperty(nameof(LabelXhtmlBlock.Label))]
    public virtual IList<LabelXhtmlBlock>? Tabs1 { get; set; }

    [Display(
        Name = "Link Collection",
        GroupName = SystemTabNames.Content,
        Order = 150)]
    public virtual LinkItemCollection? Links { get; set; }

    #endregion

    #region Public properties

    public override void SetDefaultValues(ContentType contentType)
    {
        base.SetDefaultValues(contentType);
        //SiteName = "DEMO";
    }

    #endregion
}
