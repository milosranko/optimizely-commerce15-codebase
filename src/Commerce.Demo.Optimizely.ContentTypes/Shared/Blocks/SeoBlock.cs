using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Web;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Shared.Blocks;

[ContentType(
    DisplayName = "SEO",
    GUID = "{8F521BD5-5AD9-46D2-A826-73B0A11C2C26}",
    GroupName = Globals.Tabs.Default,
    AvailableInEditMode = false)]
public class SeoBlock : BlockData, ISeoMeta
{
    #region Content tab

    [Display(
        Name = "Title",
        GroupName = SystemTabNames.Content,
        Order = 10)]
    [CultureSpecific]
    public virtual string? MetaTitle { get; set; }

    [Display(
        Name = "No Robots",
        GroupName = SystemTabNames.Content,
        Order = 20)]
    [CultureSpecific]
    public virtual bool MetaNoRobots { get; set; }

    [Display(
        Name = "Keywords",
        GroupName = SystemTabNames.Content,
        Order = 30)]
    [CultureSpecific]
    [UIHint(UIHint.Textarea)]
    public virtual string MetaKeywords { get; set; }

    [Display(
        Name = "Description",
        GroupName = SystemTabNames.Content,
        Order = 40)]
    [CultureSpecific]
    [UIHint(UIHint.Textarea)]
    public virtual string? MetaDescription { get; set; }

    #endregion
}
