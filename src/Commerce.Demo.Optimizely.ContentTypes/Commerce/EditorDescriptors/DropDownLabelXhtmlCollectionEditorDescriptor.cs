using Commerce.Demo.Optimizely.ContentTypes.Commerce.Properties;
using EPiServer.Cms.Shell.UI.ObjectEditing.EditorDescriptors;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.EditorDescriptors;

public class DropDownLabelXhtmlCollectionEditorDescriptor<T> : CollectionEditorDescriptor<T> where T : DropDownLabelXhtmlProperty, new()
{
    public DropDownLabelXhtmlCollectionEditorDescriptor()
    {
        GridDefinition.AutoGenerate = true;
        GridDefinition.ExcludeColumn(x => x.Text);
    }
}
