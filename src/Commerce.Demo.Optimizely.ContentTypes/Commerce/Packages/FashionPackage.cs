using Commerce.Demo.Optimizely.ContentTypes.Commerce.Packages.Base;
using Commerce.Demo.Optimizely.Core.Attributes;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using static Commerce.Demo.Optimizely.Core.Constants.Globals;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Packages;

[CatalogContentType(
    GUID = "0E2BA809-EAE7-463F-904D-B2F43C0B4FC1",
    GroupName = GroupNames.Fashion,
    DisplayName = "Fashion Package",
    Description = "Displays a package, which is comparable to an individual SKU because Package item must be purchased as a whole.")]
[SiteImageUrl("fashion-package.png")]
public class FashionPackage : PackageBase
{
    #region Content tab

    [Searchable]
    [CultureSpecific]
    [Tokenize]
    [IncludeInDefaultSearch]
    [Display(
        Name = "Description",
        Order = 1,
        GroupName = SystemTabNames.Content)]
    public virtual XhtmlString Description { get; set; }

    #endregion
}
