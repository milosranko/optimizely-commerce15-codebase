using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Core;
using EPiServer.Shell.ObjectEditing.EditorDescriptors;

namespace Commerce.Demo.Optimizely.Core.Infrastructure.EditorDescriptors;

[EditorDescriptorRegistration(
        TargetType = typeof(XhtmlString),
        EditorDescriptorBehavior = EditorDescriptorBehavior.ExtendBase,
        UIHint = Globals.SiteUIHints.DisableBlocksInsideXhtml)]
public class XhtmlEditorDescriptor : EditorDescriptor
{
    public XhtmlEditorDescriptor()
    {
        AllowedTypes = new[] { typeof(PageData) };
    }
}
