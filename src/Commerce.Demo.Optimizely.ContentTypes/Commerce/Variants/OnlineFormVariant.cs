using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Base;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Interfaces;
using Commerce.Demo.Optimizely.Core.Attributes;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.DataAbstraction;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants;

[CatalogContentType(
    GUID = "{E3A167A6-4F0B-4F63-87F7-44D057DC820E}",
    GroupName = Globals.GroupNames.Products,
    DisplayName = "Online Form Variant",
    Description = "Display on-line form variant")]
[SiteImageUrl("onlineform-variant.png")]
public class OnlineFormVariant : VariantBase, ISubscription
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
