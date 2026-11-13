using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Base;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks;

[ContentType(
    DisplayName = "Carousel Container Block",
    Description = "A carousel/slider container that holds carousel item slides.",
    GUID = "{A7B8C9D0-E1F2-4A3B-8C5D-6E7F8A9B0C1D}")]
public class CarouselContainerBlock : BlockBase
{
    #region Content tab

    [CultureSpecific]
    [Display(
        GroupName = SystemTabNames.Content,
        Order = 100)]
    public virtual string Heading { get; set; }

    [Display(
        GroupName = SystemTabNames.Content,
        Order = 110)]
    [AllowedTypes(typeof(CarouselItemBlock))]
    public virtual ContentArea Items { get; set; }

    #endregion
}
