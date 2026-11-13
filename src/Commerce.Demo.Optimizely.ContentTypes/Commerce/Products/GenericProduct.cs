using Commerce.Demo.Optimizely.ContentTypes.Commerce.Products.Base;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants;
using Commerce.Demo.Optimizely.Core.Attributes;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.DataAnnotations;
using static Commerce.Demo.Optimizely.Core.Constants.Globals;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Products;

[CatalogContentType(
    GUID = "{ADBBBD0C-DD39-4C0A-99DD-2BCD5ED337FF}",
    GroupName = GroupNames.Products,
    DisplayName = "Generic Product",
    Description = "Generic product")]
[AvailableContentTypes(Include = [typeof(MaterialVariant), typeof(MeetingVariant), typeof(SubscriptionVariant), typeof(OnlineFormVariant), typeof(LawOfficeVideoVariant)])]
[SiteImageUrl("generic-product.png")]
public class GenericProduct : ProductBase
{
    #region Content tab

    #endregion
}
