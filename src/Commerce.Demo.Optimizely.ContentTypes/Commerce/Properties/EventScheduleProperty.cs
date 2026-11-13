using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.SpecializedProperties;
using EPiServer.Web;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Properties;

public class EventScheduleProperty
{
    [Display(
        Name = "Time",
        GroupName = SystemTabNames.Content,
        Order = 10)]
    public virtual DateTime? Time { get; set; }

    [Display(
        Name = "Heading",
        GroupName = SystemTabNames.Content,
        Order = 20,
        Prompt = "Some heading")]
    [CultureSpecific]
    [Searchable]
    public virtual string? Heading { get; set; }

    [Display(
        Name = "Description",
        GroupName = SystemTabNames.Content,
        Order = 30)]
    [CultureSpecific]
    [Searchable]
    public virtual XhtmlString Description { get; set; }

    [Display(
        Name = "Speakers",
        GroupName = SystemTabNames.Content,
        Order = 40)]
    [BackingType(typeof(PropertyContentReferenceList))]
    [AllowedTypes(typeof(TeaserBlock))]
    [UIHint(UIHint.Block)]
    public virtual IList<ContentReference>? Speakers { get; set; }
}

[PropertyDefinitionType]
public class EventSchedulePropertyMetaData : PropertyList<EventScheduleProperty>
{ }
