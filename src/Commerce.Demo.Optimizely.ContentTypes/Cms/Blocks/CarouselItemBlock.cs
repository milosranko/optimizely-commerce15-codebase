using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Base;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Local;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Web;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks;

[ContentType(
    DisplayName = "Carousel Item Block",
    Description = "A single slide within a carousel container.",
    GUID = "{F1A2B3C4-D5E6-4F78-9A0B-1C2D3E4F5A6B}")]
public class CarouselItemBlock : BlockBase
{
    #region Content tab

    [CultureSpecific]
    [Display(
        Name = "Heading",
        GroupName = SystemTabNames.Content,
        Order = 100)]
    public virtual string? Heading { get; set; }

    [CultureSpecific]
    [Display(
        Name = "Intro Text",
        GroupName = SystemTabNames.Content,
        Order = 110)]
    public virtual XhtmlString? IntroText { get; set; }

    [CultureSpecific]
    [Display(
        Name = "Background Image",
        GroupName = SystemTabNames.Content,
        Order = 120)]
    [UIHint(UIHint.Image)]
    public virtual ContentReference? BackgroundImage { get; set; }

    [Display(
        Name = "CTA Link",
        GroupName = SystemTabNames.Content,
        Order = 130)]
    public virtual LinkBlock? CtaLink { get; set; }

    #endregion
}
