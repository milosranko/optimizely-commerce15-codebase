using Commerce.Demo.Optimizely.ContentTypes.Commerce.Properties;
using EPiServer.Cms.Shell.UI.ObjectEditing.EditorDescriptors;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.EditorDescriptors;

public class LabelXhtmlCollectionEditorDescriptor<T> : CollectionEditorDescriptor<T> where T : LabelXhtmlProperty, new()
{
    public LabelXhtmlCollectionEditorDescriptor()
    {
        GridDefinition.AutoGenerate = true;
        GridDefinition.ExcludeColumn(x => x.Text);
    }
}
