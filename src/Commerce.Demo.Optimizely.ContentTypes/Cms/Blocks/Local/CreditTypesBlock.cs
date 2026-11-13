using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Base;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Commerce.Shell.UIDescriptors.EditorDescriptors.SelectionFactories;
using EPiServer.Commerce.SpecializedProperties;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Shell.ObjectEditing;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Local;

[ContentType(
    DisplayName = "Credit Types Block",
    GUID = "{90C01FE5-36C3-4103-A541-9921D3679828}",
    GroupName = Globals.Tabs.Default,
    AvailableInEditMode = false)]
public class CreditTypesBlock : BlockBase
{
    #region Content tab

    [Display(
        Name = "Credit Type",
        GroupName = SystemTabNames.Content,
        Order = 10)]
    [BackingType(typeof(PropertyDictionarySingle))]
    [SelectOne(SelectionFactoryType = typeof(DictionarySelectionFactory))]
    public virtual string CreditType { get; set; }

    [Display(
        Name = "Display Name",
        GroupName = SystemTabNames.Content,
        Order = 20)]
    [CultureSpecific]
    public virtual string? DisplayName { get; set; }

    #endregion
}
