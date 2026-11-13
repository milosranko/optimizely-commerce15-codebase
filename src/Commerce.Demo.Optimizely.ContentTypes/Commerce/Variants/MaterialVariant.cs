using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Base;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Interfaces;
using Commerce.Demo.Optimizely.Core.Attributes;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using static Commerce.Demo.Optimizely.Core.Constants.Globals;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants;

[CatalogContentType(
    GUID = "{E3BFA5DA-F3ED-46D8-9000-2097402C9FB3}",
    GroupName = GroupNames.Products,
    DisplayName = "Material Variant",
    Description = "Display material variant")]
[SiteImageUrl("material-variant.png")]
public class MaterialVariant : VariantBase, ILanguage, IEndorsedMessage
{
    #region Content tab

    [CultureSpecific]
    [Display(
        Name = "Language",
        Order = 10,
        GroupName = SystemTabNames.Content)]
    public virtual string? LanguageName { get; set; } //There is a base property with the name Language, so we need to use a different name for this property

    [CultureSpecific]
    [Display(
        Name = "Endorsed Message",
        Order = 20,
        GroupName = SystemTabNames.Content)]
    public virtual string? EndorsedMessage { get; set; }

    #endregion
}
