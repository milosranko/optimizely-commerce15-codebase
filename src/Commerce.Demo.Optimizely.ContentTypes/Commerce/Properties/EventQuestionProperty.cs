using Commerce.Demo.Optimizely.ContentTypes.Commerce.Properties.Enums;
using Commerce.Demo.Optimizely.Core.Infrastructure.EditorDescriptors;
using EPiServer.Commerce.Shell.UIDescriptors.EditorDescriptors.SelectionFactories;
using EPiServer.Commerce.SpecializedProperties;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Shell.ObjectEditing;
using EPiServer.Web;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Properties;

public class EventQuestionProperty
{
    [Display(
        Name = "Question",
        GroupName = SystemTabNames.Content,
        Order = 10)]
    [UIHint(UIHint.Textarea)]
    public string? Question { get; set; }

    [Display(
        Name = "Purchase Claims",
        GroupName = SystemTabNames.Content,
        Order = 20)]
    [BackingType(typeof(PropertyStringDictionaryMultiple))]
    //[BackingType(typeof(PropertyStringList))]
    //[UIHint(SystemUiHint.DictionaryMultipleSelector)]
    [SelectMany(SelectionFactoryType = typeof(DictionarySelectionFactory))]
    //[AutoSuggestSelection(typeof(ClaimsSelectionQuery), AllowCustomValues = false)]
    //MilosR property name must match dictionary name
    public virtual string? PurchaseClaims { get; set; }

    [Display(
        Name = "Question Type",
        GroupName = SystemTabNames.Content,
        Order = 30)]
    [EditorDescriptor(EditorDescriptorType = typeof(EnumEditorDescriptor<QuestionType>))]
    public QuestionType Type { get; set; }

    [Display(
        Name = "Required",
        GroupName = SystemTabNames.Content,
        Order = 40)]
    public bool Required { get; set; }
}

[PropertyDefinitionType]
public class EventQuestionPropertyMetaData : PropertyList<EventQuestionProperty>
{ }
