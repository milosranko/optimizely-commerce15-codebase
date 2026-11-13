using Commerce.Demo.Optimizely.ContentTypes.Commerce.Properties.Enums;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants;
using Commerce.Demo.Optimizely.Core.Infrastructure.EditorDescriptors;
using EPiServer.Commerce;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Shell.ObjectEditing;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Properties;

public class EventAddonProperty
{
    [Display(
        GroupName = SystemTabNames.Content,
        Order = 10)]
    [AllowedTypes(typeof(EventVariant))]
    [UIHint(UIHint.ProductVariation)]
    public virtual ContentReference Variant { get; set; }

    [Display(
        GroupName = SystemTabNames.Content,
        Order = 20)]
    [EditorDescriptor(EditorDescriptorType = typeof(EnumEditorDescriptor<LogicalOperator>))]
    //[BackingType(typeof(PropertyNumber))]
    public virtual LogicalOperator Operator { get; set; }
}

[PropertyDefinitionType]
public class EventAddonPropertyMetaData : PropertyList<EventAddonProperty>
{ }
