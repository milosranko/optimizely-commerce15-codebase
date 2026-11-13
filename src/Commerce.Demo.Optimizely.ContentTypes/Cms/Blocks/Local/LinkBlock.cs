using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Base;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Local;

[ContentType(
    DisplayName = "Link",
    GUID = "{BBB5D413-D002-4124-B4A5-7770327B93E5}",
    GroupName = Globals.Tabs.Default,
    AvailableInEditMode = false)]
public class LinkBlock : BlockBase
{
    #region Content tab

    [Display(
        Name = "Link Text",
        GroupName = SystemTabNames.Content,
        Order = 10)]
    [CultureSpecific]
    public virtual string? LinkText { get; set; }

    [Display(
        Name = "Link URL",
        GroupName = SystemTabNames.Content,
        Order = 20)]
    [CultureSpecific]
    public virtual Url? LinkUrl { get; set; }

    #endregion
}
