using Commerce.Demo.Optimizely.ContentTypes.Commerce.Properties;
using Commerce.Demo.Optimizely.Core.Attributes;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Cms.Shell.UI.ObjectEditing.EditorDescriptors;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Shell.ObjectEditing;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants;

[CatalogContentType(
    GUID = "{5CE67A4E-E486-4AB6-AD4E-8A05A2A80E31}",
    GroupName = Globals.GroupNames.Event,
    DisplayName = "Event Tuition Variant",
    Description = "Display event tuition variant")]
[SiteImageUrl("event-tuition-variant.png")]
public class EventTuitionVariant : EventVariant
{
    #region Content tab

    [Display(
        Name = "Questions",
        Order = 10,
        GroupName = SystemTabNames.Content)]
    [BackingType(typeof(EventQuestionPropertyMetaData))]
    [EditorDescriptor(EditorDescriptorType = typeof(CollectionEditorDescriptor<EventQuestionProperty>))]
    public virtual IList<EventQuestionProperty>? Questions { get; set; }

    [Display(
        Name = "Color",
        Order = 20,
        GroupName = SystemTabNames.Content)]
    [UIHint(Globals.SiteUIHints.HexColor)]
    [RegularExpression(Globals.Validation.HexColorPattern, ErrorMessage = "Enter the colour as #RRGGBB, for example #1A2B3C.")]
    [StringLength(7)]
    // Replaces "ColorPicker" (DoubleJay IColor, no CMS 13 build). New property name on purpose:
    // existing environments keep the old IColor values in the DB for migration instead of a
    // content-type sync type-change conflict. Not "Color": catalog properties are shared metafields by
    // name and FashionVariant.Color (culture-specific colour name) already owns it. See ADR-008.
    public virtual string? DisplayColor { get; set; }

    #endregion
}
