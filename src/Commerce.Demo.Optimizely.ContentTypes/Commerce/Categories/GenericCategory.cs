using Commerce.Demo.Optimizely.ContentTypes.Commerce.Categories.Base;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Products.Base;
using Commerce.Demo.Optimizely.Core.Attributes;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.DataAnnotations;
using static Commerce.Demo.Optimizely.Core.Constants.Globals;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Categories;

[CatalogContentType(
    GUID = "{FC87989F-0728-484B-9203-E0354B641F18}",
    GroupName = GroupNames.Event,
    DisplayName = "Generic Category",
    Description = "Generic Category.")]
[AvailableContentTypes(Include =
    [
        typeof(ProductBase),
        typeof(GenericCategory)
    ])]
[SiteImageUrl("generic-category.png")]
public class GenericCategory : CategoryBase
{ }
