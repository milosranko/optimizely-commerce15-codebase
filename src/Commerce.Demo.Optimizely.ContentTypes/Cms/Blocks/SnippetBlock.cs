using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Base;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Enums;
using Commerce.Demo.Optimizely.Core.Infrastructure.EditorDescriptors;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Shell.ObjectEditing;
using EPiServer.Web;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks;

[ContentType(
    DisplayName = "Snippet Block",
    AvailableInEditMode = true,
    GUID = "{B2EE2229-C1C0-4439-874C-676D527A9EB8}")]
public class SnippetBlock : BlockBase
{
    #region Content tab

    [Display(
        Name = "Name",
        Description = "Snippet name.",
        GroupName = SystemTabNames.Content,
        Order = 10)]
    public virtual string? Name { get; set; }

    [Display(
        Name = "Description",
        Description = "Snippet description.",
        GroupName = SystemTabNames.Content,
        Order = 20)]
    [UIHint(UIHint.Textarea)]
    [CultureSpecific]
    public virtual string? Description { get; set; }

    [Display(
        Name = "Type",
        Description = "Snippet type.",
        GroupName = SystemTabNames.Content,
        Order = 30)]
    [EditorDescriptor(EditorDescriptorType = typeof(EnumEditorDescriptor<SnippetType>))]
    public virtual SnippetType Type { get; set; }

    [Display(
        Name = "Usage",
        Description = "Snippet usage.",
        GroupName = SystemTabNames.Content,
        Order = 40)]
    [EditorDescriptor(EditorDescriptorType = typeof(EnumEditorDescriptor<SnippetUsage>))]
    public virtual SnippetUsage Usage { get; set; }

    [Display(
        Name = "Placement",
        Description = "Place where the snippet will be rendered.",
        GroupName = SystemTabNames.Content,
        Order = 50)]
    [EditorDescriptor(EditorDescriptorType = typeof(EnumEditorDescriptor<SnippetArea>))]
    public virtual SnippetArea Placement { get; set; }

    [Display(
        Name = "Content",
        Description = "Insert snippet with or without tag.",
        GroupName = SystemTabNames.Content,
        Order = 60)]
    [UIHint(UIHint.Textarea)]
    public virtual string? Content { get; set; }

    [Display(
        Name = "Requires Visitor Consent",
        Description = "Check if it requires visitor consent.",
        GroupName = SystemTabNames.Content,
        Order = 70)]
    public virtual bool RequiresVisitorConsent { get; set; }

    [Display(
        Name = "Enable In Edit Mode",
        Description = "Check if it should be enabled in edit mode.",
        GroupName = SystemTabNames.Content,
        Order = 80)]
    public virtual bool EnableInEditMode { get; set; }

    #endregion
}
