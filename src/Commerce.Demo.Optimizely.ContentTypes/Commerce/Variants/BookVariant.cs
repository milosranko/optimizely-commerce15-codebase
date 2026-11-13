using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Base;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Interfaces;
using Commerce.Demo.Optimizely.Core.Attributes;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants;

[CatalogContentType(
    GUID = "{5AA0B8C7-E87D-49ED-A474-81B8C8154EA1}",
    GroupName = Globals.GroupNames.Book,
    DisplayName = "Book Variant",
    Description = "Display book variant")]
[SiteImageUrl("book-variant.png")]
public class BookVariant : VariantBase, ISubscription, ILanguage, IAutoSupplementation
{
    #region Content tab

    [Searchable]
    [CultureSpecific]
    [Tokenize]
    [IncludeInDefaultSearch]
    [Display(
        Name = "Title",
        Order = 10,
        GroupName = SystemTabNames.Content)]
    public virtual string? Title { get; set; }

    [Searchable]
    [CultureSpecific]
    [Tokenize]
    [IncludeInDefaultSearch]
    [Display(
        Name = "Author",
        Order = 20,
        GroupName = SystemTabNames.Content)]
    public virtual string? Author { get; set; }

    [Searchable]
    [CultureSpecific]
    [Tokenize]
    [IncludeInDefaultSearch]
    [Display(
        Name = "Description",
        Order = 30,
        GroupName = SystemTabNames.Content)]
    public virtual XhtmlString? Description { get; set; }

    [Display(
        Name = "Subscription Length",
        Order = 40,
        GroupName = SystemTabNames.Content)]
    public virtual int? SubscriptionLength { get; set; }

    [Display(
        Name = "Subscription Renewal",
        Order = 50,
        GroupName = SystemTabNames.Content)]
    public virtual int? SubscriptionRenewal { get; set; }

    [CultureSpecific]
    [Display(
        Name = "Language",
        Order = 60,
        GroupName = SystemTabNames.Content)]
    public virtual string? LanguageName { get; set; } //There is a base property with the name Language, so we need to use a different name for this property

    [Display(
        Name = "Is Auto Supplemental",
        Order = 70,
        GroupName = SystemTabNames.Content)]
    public virtual bool IsAutoSupp { get; set; }

    [CultureSpecific]
    [Display(
        Name = "Auto Supplemental Message",
        Order = 80,
        GroupName = SystemTabNames.Content)]
    public virtual string? AutoSuppMessage { get; set; }

    [Display(
        Name = "Auto Supplemental Code",
        Order = 90,
        GroupName = SystemTabNames.Content)]
    public virtual string? AutoSuppCode { get; set; }

    #endregion
}
