using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Shell.ObjectEditing;
using EPiServer.Shell.ObjectEditing.EditorDescriptors;

namespace Commerce.Demo.Optimizely.Core.Infrastructure.EditorDescriptors;

[EditorDescriptorRegistration(TargetType = typeof(String[]), UIHint = Globals.SiteUIHints.StringList)]
public class StringListEditorDescriptor : EditorDescriptor
{
    public override void ModifyMetadata(
        ExtendedMetadata metadata,
        IEnumerable<Attribute> attributes)
    {
        ClientEditingClass = "alloy/editors/StringList";

        base.ModifyMetadata(metadata, attributes);
    }
}
