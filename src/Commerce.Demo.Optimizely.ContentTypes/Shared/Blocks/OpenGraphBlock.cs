using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Web;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Shared.Blocks;

[ContentType(
    DisplayName = "Open Graph",
    GUID = "{830E0258-8196-4514-BA8E-8E63E9007CE7}",
    GroupName = Globals.Tabs.Default,
    AvailableInEditMode = false)]
public class OpenGraphBlock : BlockData, IOpenGraph
{
    #region Content tab

    [Display(
        Name = "Title",
        GroupName = SystemTabNames.Content,
        Order = 10)]
    [CultureSpecific]
    public virtual string? OpenGraphTitle { get; set; }

    [Display(
        Name = "Description",
        GroupName = SystemTabNames.Content,
        Order = 20)]
    [CultureSpecific]
    [UIHint(UIHint.Textarea)]
    public virtual string? OpenGraphDescription { get; set; }

    [Display(
        Name = "Image",
        GroupName = SystemTabNames.Content,
        Order = 30)]
    [UIHint(UIHint.Image)]
    public virtual ContentReference? OpenGraphImage { get; set; }

    #endregion
}
