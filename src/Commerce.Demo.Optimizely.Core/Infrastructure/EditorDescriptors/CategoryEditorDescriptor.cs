using EPiServer.Cms.Shell.UI.ObjectEditing;
using EPiServer.Core;
using EPiServer.Shell.ObjectEditing;
using EPiServer.Shell.ObjectEditing.EditorDescriptors;

namespace Commerce.Demo.Optimizely.Core.Infrastructure.EditorDescriptors;

[EditorDescriptorRegistration(TargetType = typeof(CategoryList))]
public class CategoryEditorDescriptor : EditorDescriptor
{
    public override void ModifyMetadata(
        ExtendedMetadata metadata,
        IEnumerable<Attribute> attributes)
    {
        //Use it to hide Category selector on page/block
        var ownerContent = ((ContentDataMetadata)metadata).OwnerContent;
        if (ownerContent is PageData ||
            ownerContent is BlockData ||
            ownerContent is MediaData)
        {
            metadata.ShowForEdit = false;
        }
    }
}
