using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Properties;

public class TabProperty
{
    [Display(
        GroupName = SystemTabNames.Content,
        Order = 10)]
    [CultureSpecific]
    public virtual string? Label { get; set; }

    [Display(
        GroupName = SystemTabNames.Content,
        Order = 20)]
    [CultureSpecific]
    public virtual XhtmlString Text { get; set; }
}

[PropertyDefinitionType]
public class TabPropertyMetaData : PropertyList<TabProperty>
{ }
