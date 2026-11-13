using Commerce.Demo.Optimizely.ContentTypes.Commerce.Queries;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Shell.ObjectEditing;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Properties;

public class EventCreditProperty
{
    [Display(
        Name = "Credit Type",
        GroupName = SystemTabNames.Content,
        Order = 10)]
    //[BackingType(typeof(PropertyDictionarySingle))]
    [SelectOne(SelectionFactoryType = typeof(CatalogCategorySelectionFactory))]
    public virtual string? CreditType { get; set; }

    [Display(
        Name = "Amount",
        GroupName = SystemTabNames.Content,
        Order = 20,
        Prompt = "1.5")]
    public virtual decimal Score { get; set; }
}

[PropertyDefinitionType]
public class EventCreditPropertyMetaData : PropertyList<EventCreditProperty>
{ }
