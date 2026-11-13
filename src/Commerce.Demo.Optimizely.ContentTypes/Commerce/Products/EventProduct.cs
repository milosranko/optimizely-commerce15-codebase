using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Base;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.EditorDescriptors;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Products.Base;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Properties;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants;
using Commerce.Demo.Optimizely.Core.Attributes;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Shell.ObjectEditing;
using EPiServer.Web;
using System.ComponentModel.DataAnnotations;
using static Commerce.Demo.Optimizely.Core.Constants.Globals;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Products;

[CatalogContentType(
    GUID = "{E24487F1-D7FD-43A2-8718-3F49AF09B2C7}",
    GroupName = GroupNames.Products,
    DisplayName = "Event Product",
    Description = "Event product with schedule")]
[AvailableContentTypes(Include = [typeof(EventVariant)])]
[SiteImageUrl("event-product.png")]
public class EventProduct : ProductBase
{
    #region Content tab

    [Display(
        Name = "Event Schedule",
        Order = 100,
        GroupName = SystemTabNames.Content)]
    [BackingType(typeof(EventSchedulePropertyMetaData))]
    [EditorDescriptor(EditorDescriptorType = typeof(EventScheduleCollectionEditorDescriptor<EventScheduleProperty>))]
    [ListItemHeaderProperty(nameof(EventScheduleProperty.Heading))]
    public virtual IList<EventScheduleProperty>? EventSchedule { get; set; }

    [Display(
        Name = "Icon Picker",
        GroupName = SystemTabNames.Content,
        Order = 110)]
    [UIHint("IconPicker")]
    public virtual string? Icon { get; set; }

    [Display(
        Name = "Sponsors",
        GroupName = SystemTabNames.Content,
        Order = 120)]
    [AllowedTypes(typeof(BlockBase))]
    public virtual ContentArea? Sponsors { get; set; }

    [Display(
        Name = "Sponsor Message",
        GroupName = SystemTabNames.Content,
        Order = 130)]
    [UIHint(UIHint.Block)]
    public virtual ContentReference? SponsorMessageBlock { get; set; }

    [Display(
        Name = "Presented By",
        GroupName = SystemTabNames.Content,
        Order = 140)]
    [CultureSpecific]
    public virtual string? PresentedBy { get; set; }

    #endregion
}
