using Commerce.Demo.Optimizely.ContentTypes.Commerce.Properties;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Base;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Interfaces;
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
    GUID = "{6A02CD56-B338-4C5F-BD22-F56D6BAADEDF}",
    GroupName = Globals.GroupNames.Event,
    DisplayName = "Event Variant",
    Description = "Display event variant")]
[SiteImageUrl("event-variant.png")]
public class EventVariant : VariantBase, ICredits, IRegistration, IEventDetails, IEndorsedMessage
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

    [CultureSpecific]
    [Display(
        Name = "Endorsed Message",
        Order = 40,
        GroupName = SystemTabNames.Content)]
    public virtual string? EndorsedMessage { get; set; }

    #endregion
}
