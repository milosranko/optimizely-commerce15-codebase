using Commerce.Demo.Optimizely.ContentTypes.Commerce.Products.Base;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants;
using Commerce.Demo.Optimizely.Core.Attributes;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.Commerce.SpecializedProperties;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using static Commerce.Demo.Optimizely.Core.Constants.Globals;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Products;

[CatalogContentType(
    GUID = "{74152223-99F0-4B3F-8FD0-DDCD535EC861}",
    GroupName = GroupNames.Fashion,
    DisplayName = "Fashion Product",
    Description = "Fashion product with Add to Cart button.")]
[AvailableContentTypes(Include = [typeof(FashionVariant)])]
[SiteImageUrl("fashion-product.png")]
public class FashionProduct : ProductBase
{
    #region Content tab

    [Searchable]
    [CultureSpecific]
    [Tokenize]
    [IncludeInDefaultSearch]
    [Display(
        Name = "Brand",
        Order = 10,
        GroupName = SystemTabNames.Content)]
    public virtual string? Brand { get; set; }

    [Searchable]
    [CultureSpecific]
    [Tokenize]
    [IncludeInDefaultSearch]
    [Display(
        Name = "Description",
        Order = 20,
        GroupName = SystemTabNames.Content)]
    public virtual XhtmlString? Description { get; set; }

    [Searchable]
    [CultureSpecific]
    [Tokenize]
    [IncludeInDefaultSearch]
    [Display(
        Name = "Long Description",
        Order = 30,
        GroupName = SystemTabNames.Content)]
    public virtual XhtmlString? LongDescription { get; set; }

    [Searchable]
    [CultureSpecific]
    [Tokenize]
    [IncludeInDefaultSearch]
    [Display(
        Name = "Sizing",
        Order = 40,
        GroupName = SystemTabNames.Content)]
    public virtual XhtmlString? Sizing { get; set; }

    [CultureSpecific]
    [Display(
        Name = "Product Teaser",
        Order = 50,
        GroupName = SystemTabNames.Content)]
    public virtual XhtmlString? ProductTeaser { get; set; }

    [Searchable]
    [IncludeInDefaultSearch]
    [BackingType(typeof(PropertyStringDictionaryMultiple))]
    [Display(
        Name = "Available Sizes",
        Order = 60,
        GroupName = SystemTabNames.Content)]
    public virtual ItemCollection<string>? AvailableSizes { get; set; }

    [Searchable]
    [IncludeInDefaultSearch]
    [BackingType(typeof(PropertyStringDictionaryMultiple))]
    [Display(
        Name = "Available Colors",
        Order = 70,
        GroupName = SystemTabNames.Content)]
    public virtual ItemCollection<string>? AvailableColors { get; set; }

    [Searchable]
    [IncludeInDefaultSearch]
    [BackingType(typeof(PropertyDictionarySingle))]
    [Display(
        Name = "Available Colors Single",
        Order = 80,
        GroupName = SystemTabNames.Content)]
    public virtual string? AvailableColor { get; set; }

    #endregion
}
