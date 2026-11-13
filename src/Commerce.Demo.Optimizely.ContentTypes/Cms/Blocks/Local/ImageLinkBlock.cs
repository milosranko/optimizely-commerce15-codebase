using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Local;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Web;
using System.ComponentModel.DataAnnotations;

namespace Wisbar.Optimizely.ContentTypes.Cms.Blocks.Local;

[ContentType(
    DisplayName = "Image Link",
    GUID = "{C2ECE54E-6F19-4E20-9CD2-D20DA8124D78}",
    GroupName = Globals.Tabs.Default,
    AvailableInEditMode = false)]
public class ImageLinkBlock : LinkBlock
{
    #region Content tab

    [Display(
        Name = "Image",
        GroupName = SystemTabNames.Content,
        Order = 10)]
    [UIHint(UIHint.Image)]
    public virtual ContentReference? Image { get; set; }

    #endregion
}
