using Commerce.Demo.Optimizely.ContentTypes.Commerce.Bundles;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Categories.Base;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Packages;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Products;
using Commerce.Demo.Optimizely.Core.Attributes;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.DataAnnotations;
using static Commerce.Demo.Optimizely.Core.Constants.Globals;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Categories;

[CatalogContentType(
    GUID = "{AAB498B1-FE77-4B30-A39D-9388A93C355E}",
    GroupName = GroupNames.Fashion,
    DisplayName = "Fashion Category",
    Description = "Display fashion products.")]
[AvailableContentTypes(Include =
    [
        typeof(BookProduct),
        typeof(EventProduct),
        typeof(FashionProduct),
        typeof(FashionPackage),
        typeof(FashionBundle),
        typeof(FashionCategory)
    ])]
[SiteImageUrl("fashion-category.png")]
public class FashionCategory : CategoryBase
{ }
