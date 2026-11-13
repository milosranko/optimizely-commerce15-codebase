using EPiServer.Shell.ObjectEditing;
using EPiServer.Shell.ObjectEditing.EditorDescriptors;

namespace Commerce.Demo.Optimizely.Core.Infrastructure.EditorDescriptors;

[EditorDescriptorRegistration(TargetType = typeof(string), UIHint = "IconPicker")]
public class IconPickerEditorDescriptor : EditorDescriptor
{
    public override void ModifyMetadata(ExtendedMetadata metadata, IEnumerable<Attribute> attributes)
    {
        ClientEditingClass = "foundation/Editors/IconPicker";

        base.ModifyMetadata(metadata, attributes);
    }
}
