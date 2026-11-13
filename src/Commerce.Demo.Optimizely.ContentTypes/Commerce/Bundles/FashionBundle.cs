using Commerce.Demo.Optimizely.ContentTypes.Commerce.Bundles.Base;
using Commerce.Demo.Optimizely.Core.Attributes;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using static Commerce.Demo.Optimizely.Core.Constants.Globals;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Bundles;

[CatalogContentType(
    GUID = "{9A374ED1-AC7E-4329-B13D-6BB9A0344B79}",
    GroupName = GroupNames.Fashion,
    DisplayName = "Fashion Bundle",
    Description = "Displays a bundle, which is collection of individual fashion variants.")]
[SiteImageUrl("fashion-bundle.png")]
public class FashionBundle : BundleBase
{
    #region Content tab

    [Searchable]
    [CultureSpecific]
    [Tokenize]
    [IncludeInDefaultSearch]
    [Display(
        Name = "Description",
        Order = 10,
        GroupName = SystemTabNames.Content)]
    public virtual XhtmlString Description { get; set; }

    #endregion
}
