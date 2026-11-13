using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Base;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Interfaces;
using Commerce.Demo.Optimizely.Core.Attributes;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants;

[CatalogContentType(
    GUID = "{48880C7F-D9DA-412A-8828-4C3837711D43}",
    GroupName = Globals.GroupNames.Products,
    DisplayName = "Law Office Video Variant",
    Description = "Display law office video variant")]
[SiteImageUrl("lawofficevideo-variant.png")]
public class LawOfficeVideoVariant : VariantBase, ISubscription, ILanguage, IEndorsedMessage
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

    [CultureSpecific]
    [Display(
        Name = "Language",
        Order = 30,
        GroupName = SystemTabNames.Content)]
    public virtual string? LanguageName { get; set; }

    [CultureSpecific]
    [Display(
        Name = "Endorsed Message",
        Order = 40,
        GroupName = SystemTabNames.Content)]
    public virtual string? EndorsedMessage { get; set; }

    #endregion
}
