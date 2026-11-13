using Commerce.Demo.Optimizely.ContentTypes.Commerce.Properties;
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
    GUID = "{C681FAE0-442E-49F9-A5F0-F9AEB7624B2E}",
    GroupName = Globals.GroupNames.Event,
    DisplayName = "Event Add-on Variant",
    Description = "Display event add-on variant")]
[SiteImageUrl("event-addon-variant.png")]
public class EventAddonVariant : EventVariant, IAddon
{
    #region Content tab

    [Display(
        Name = "Add-on Relations",
        Order = 10,
        GroupName = SystemTabNames.Content)]
    [BackingType(typeof(PropertyContentReferenceList))]
    [AllowedTypes(typeof(EventAddonVariant))]
    public virtual IList<ContentReference>? AddonRelations { get; set; }

    [Display(
        Name = "Dependent Add-ons",
        Order = 20,
        GroupName = SystemTabNames.Content)]
    [BackingType(typeof(PropertyContentReferenceList))]
    [AllowedTypes(typeof(EventAddonVariant))]
    public virtual IList<ContentReference>? DependentAddons { get; set; }

    [Display(
        Name = "Add-on Conflicts",
        Order = 30,
        GroupName = SystemTabNames.Content)]
    [BackingType(typeof(PropertyContentReferenceList))]
    [AllowedTypes(typeof(EventAddonVariant))]
    public virtual IList<ContentReference>? AddonConflicts { get; set; }

    [Display(
        Name = "Add-on Group Name",
        Order = 40,
        GroupName = SystemTabNames.Content)]
    public virtual string? AddonGroupName { get; set; }

    [Display(
        Name = "Add-on Description",
        Order = 45,
        GroupName = SystemTabNames.Content)]
    public virtual XhtmlString? AddOnDescription { get; set; }

    [Display(
        Name = "Questions",
        Order = 50,
        GroupName = SystemTabNames.Content)]
    [BackingType(typeof(EventQuestionPropertyMetaData))]
    [EditorDescriptor(EditorDescriptorType = typeof(CollectionEditorDescriptor<EventQuestionProperty>))]
    public virtual IList<EventQuestionProperty>? Questions { get; set; }

    #endregion
}
