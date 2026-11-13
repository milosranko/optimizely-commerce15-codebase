using Commerce.Demo.Optimizely.ContentTypes.Commerce.Properties;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Base;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Interfaces;
using Commerce.Demo.Optimizely.Core.Attributes;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Cms.Shell.UI.ObjectEditing.EditorDescriptors;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Shell.ObjectEditing;
using EPiServer.SpecializedProperties;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants;

[CatalogContentType(
    GUID = "{306556BF-257F-4321-91EF-3F4B9B29A6CB}",
    GroupName = Globals.GroupNames.Event,
    DisplayName = "Meeting Variant",
    Description = "Display meeting variant")]
[SiteImageUrl("meeting-variant.png")]
public class MeetingVariant : VariantBase, ICredits, IRegistration, IAddon, IEventDetails
{
    #region Content tab

    [Display(
        Name = "Credits",
        Order = 10,
        GroupName = SystemTabNames.Content)]
    [BackingType(typeof(EventCreditPropertyMetaData))]
    [EditorDescriptor(EditorDescriptorType = typeof(CollectionEditorDescriptor<EventCreditProperty>))]
    public virtual IList<EventCreditProperty>? Credits { get; set; }

    [Display(
        Name = "Credit Expiration Date",
        Order = 20,
        GroupName = SystemTabNames.Content)]
    public virtual DateTime? CreditExpirationDate { get; set; }

    [Display(
        Name = "Requires Registration",
        Order = 30,
        GroupName = SystemTabNames.Content)]
    public virtual bool RequiresRegistration { get; set; }

    [Display(
        Name = "Registration Questions",
        Order = 40,
        GroupName = SystemTabNames.Content)]
    public virtual string? RegistrationQuestions { get; set; }

    [Display(
        Name = "Range",
        Order = 50,
        GroupName = SystemTabNames.Content)]
    [Range(0, 10)]
    [UIHint("Slider")]
    public virtual double? RangeTest1 { get; set; }

    [Display(
        Name = "Addon Relations",
        Order = 50,
        GroupName = SystemTabNames.Content)]
    [BackingType(typeof(PropertyContentReferenceList))]
    public virtual IList<ContentReference>? AddonRelations { get; set; }

    [Display(
        Name = "Dependent Add-ons",
        Order = 50,
        GroupName = SystemTabNames.Content)]
    [BackingType(typeof(PropertyContentReferenceList))]
    public virtual IList<ContentReference>? DependentAddons { get; set; }

    [Display(
        Name = "Add-on Conflicts",
        Order = 50,
        GroupName = SystemTabNames.Content)]
    [BackingType(typeof(PropertyContentReferenceList))]
    public virtual IList<ContentReference>? AddonConflicts { get; set; }

    [Display(
        Name = "Add-on Group Name",
        Order = 50,
        GroupName = SystemTabNames.Content)]
    public virtual string? AddonGroupName { get; set; }

    [Display(
        Name = "Add-On Description",
        Order = 50,
        GroupName = SystemTabNames.Content)]
    public virtual XhtmlString? AddOnDescription { get; set; }

    [Display(
        Name = "Start Date Time",
        Order = 50,
        GroupName = SystemTabNames.Content)]
    public virtual DateTime? StartDateTime { get; set; }

    [Display(
        Name = "End Date Time",
        Order = 50,
        GroupName = SystemTabNames.Content)]
    public virtual DateTime? EndDateTime { get; set; }

    [Display(
        Name = "Location Name",
        Order = 50,
        GroupName = SystemTabNames.Content)]
    public virtual string? LocationName { get; set; }

    [Display(
        Name = "Location Address",
        Order = 50,
        GroupName = SystemTabNames.Content)]
    public virtual string? LocationAddress { get; set; }

    [Display(
        Name = "City",
        Order = 50,
        GroupName = SystemTabNames.Content)]
    public virtual string? City { get; set; }

    #endregion
}
