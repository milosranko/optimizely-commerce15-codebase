using EPiServer.Cms.Shell.UI.ObjectEditing;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Blocks;

[ContentType(
    GUID = "{D634ACDE-0A92-4415-B652-AAA911EEBC49}",
    DisplayName = "Label With XHtml Block")]
public class LabelXhtmlBlock : BlockData
{
    #region Content tab

    [Display(
        Name = "Label",
        GroupName = SystemTabNames.Content,
        Order = 100)]
    [CultureSpecific]
    [StringLength(50)]
    public virtual string? Label { get; set; }

    [Display(
        Name = "Text",
        GroupName = SystemTabNames.Content,
        Order = 110)]
    [CultureSpecific]
    public virtual XhtmlString? Text { get; set; }

    #endregion
}
