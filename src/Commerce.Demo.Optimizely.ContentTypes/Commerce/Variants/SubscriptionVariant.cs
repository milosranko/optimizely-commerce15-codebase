using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Base;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Interfaces;
using Commerce.Demo.Optimizely.Core.Attributes;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.DataAbstraction;
using System.ComponentModel.DataAnnotations;
using static Commerce.Demo.Optimizely.Core.Constants.Globals;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants;

[CatalogContentType(
    GUID = "{DDB410B1-9D10-47E6-8A58-6E5B6C658FF9}",
    GroupName = GroupNames.Products,
    DisplayName = "Subscription Variant",
    Description = "Display subscription variant")]
[SiteImageUrl("subscription-variant.png")]
public class SubscriptionVariant : VariantBase, ISubscription
{
    #region Content tab

    [Display(
        Name = "Subscription Length",
        Order = 10,
        GroupName = SystemTabNames.Content)]
    public virtual int? SubscriptionLength { get; set; }

    [Display(
        Name = "Subscription Renewal",
        Order = 20,
        GroupName = SystemTabNames.Content)]
    public virtual int? SubscriptionRenewal { get; set; }

    #endregion
}
