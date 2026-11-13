using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Enums;
using Commerce.Demo.Optimizely.Core.Infrastructure.EditorDescriptors;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Shell.ObjectEditing;
using EPiServer.Web;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Local;

[ContentType(
    DisplayName = "Cookie Block",
    AvailableInEditMode = false,
    GUID = "{DC741B71-DE0B-4B4B-A099-7000B22B9859}")]
public class CookieBlock : BlockData
{
    [Display(
        Name = "Name",
        GroupName = SystemTabNames.Content,
        Order = 10)]
    [Required]
    public virtual required string Name { get; set; }

    [Display(
        Name = "Description",
        GroupName = SystemTabNames.Content,
        Order = 20)]
    [UIHint(UIHint.Textarea)]
    public virtual string? Description { get; set; }

    [Display(
        Name = "Usage",
        Description = "Cookie usage.",
        GroupName = SystemTabNames.Content,
        Order = 30)]
    [EditorDescriptor(EditorDescriptorType = typeof(EnumEditorDescriptor<SnippetUsage>))]
    public virtual SnippetUsage Usage { get; set; }
}
