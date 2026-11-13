using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Base;
using Commerce.Demo.Optimizely.Core.Attributes;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants;

[CatalogContentType(
    GUID = "{D27B133B-F9C5-438C-BC51-F4DE632BFC17}",
    GroupName = Globals.GroupNames.Fashion,
    DisplayName = "Fashion Variant",
    Description = "Display fashion variant")]
[SiteImageUrl("fashion-variant.png")]
public class FashionVariant : VariantBase
{
    #region Content tab

    [Searchable]
    [Tokenize]
    [IncludeInDefaultSearch]
    [Display(
        Name = "Size",
        Order = 10,
        GroupName = SystemTabNames.Content)]
    public virtual string Size { get; set; }

    [Searchable]
    [CultureSpecific]
    [Tokenize]
    [IncludeInDefaultSearch]
    [Display(
        Name = "Color",
        Order = 20,
        GroupName = SystemTabNames.Content)]
    public virtual string Color { get; set; }

    [Searchable]
    [CultureSpecific]
    [Tokenize]
    [IncludeInDefaultSearch]
    [Display(
        Name = "Description",
        Order = 30,
        GroupName = SystemTabNames.Content)]
    public virtual XhtmlString Description { get; set; }

    #endregion
}
