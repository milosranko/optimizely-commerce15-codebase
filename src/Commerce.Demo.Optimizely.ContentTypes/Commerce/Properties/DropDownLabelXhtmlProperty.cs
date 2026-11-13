using Commerce.Demo.Optimizely.ContentTypes.Commerce.Queries;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Shell.ObjectEditing;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Properties;

public class DropDownLabelXhtmlProperty
{
    [Display(
        GroupName = SystemTabNames.Content,
        Order = 10)]
    [CultureSpecific]
    [SelectOne(SelectionFactoryType = typeof(LabelSelectionFactory))]
    public virtual string Label { get; set; }

    [Display(
        GroupName = SystemTabNames.Content,
        Order = 20)]
    [CultureSpecific]
    public virtual XhtmlString Text { get; set; }
}

[PropertyDefinitionType]
public class DropDownLabelXhtmlPropertyMetaData : PropertyList<DropDownLabelXhtmlProperty>
{ }
