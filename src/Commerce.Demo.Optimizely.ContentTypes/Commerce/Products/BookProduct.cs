using Commerce.Demo.Optimizely.ContentTypes.Commerce.Products.Base;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants;
using Commerce.Demo.Optimizely.Core.Attributes;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using static Commerce.Demo.Optimizely.Core.Constants.Globals;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Products;

[CatalogContentType(
    GUID = "{C705C8E3-5015-4424-84BF-DE8A989CCFF6}",
    GroupName = GroupNames.Products,
    DisplayName = "Book Product",
    Description = "Book product")]
[AvailableContentTypes(Include = [typeof(BookVariant)])]
[SiteImageUrl("book-product.png")]
public class BookProduct : ProductBase
{
    #region Content tab

    [Display(
        Name = "ISBN",
        GroupName = SystemTabNames.Content,
        Order = 100)]
    [CultureSpecific]
    [Required]
    public virtual string Isbn { get; set; }

    [Display(
        Name = "Published By",
        GroupName = SystemTabNames.Content,
        Order = 110)]
    [CultureSpecific]
    public virtual string? PublishedBy { get; set; }

    #endregion
}
