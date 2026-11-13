using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Shell.ObjectEditing;
using EPiServer.Shell.ObjectEditing.EditorDescriptors;

namespace Commerce.Demo.Optimizely.Core.Infrastructure.EditorDescriptors;

/// <summary>
/// Edits a <c>string</c> holding a <c>#RRGGBB</c> colour with the browser's native colour input
/// plus a hex text box. Replaces DoubleJay.Epi.ConfigurableColorPicker, which has no CMS 13 build.
/// Server-side validation stays on the property (<c>[RegularExpression(Globals.Validation.HexColorPattern)]</c>).
/// </summary>
[EditorDescriptorRegistration(TargetType = typeof(string), UIHint = Globals.SiteUIHints.HexColor)]
public class HexColorEditorDescriptor : EditorDescriptor
{
    public override void ModifyMetadata(ExtendedMetadata metadata, IEnumerable<Attribute> attributes)
    {
        ClientEditingClass = "foundation/Editors/HexColorPicker";

        base.ModifyMetadata(metadata, attributes);
    }
}
