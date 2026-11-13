using Commerce.Demo.Optimizely.ContentTypes.Commerce.Properties;
using EPiServer.Cms.Shell.UI.ObjectEditing.EditorDescriptors;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.EditorDescriptors;

public class EventScheduleCollectionEditorDescriptor<T> : CollectionEditorDescriptor<T> where T : EventScheduleProperty, new()
{
    public EventScheduleCollectionEditorDescriptor()
    {
        GridDefinition.AutoGenerate = true;
        GridDefinition.ExcludeColumn(x => x.Speakers);
        GridDefinition.ExcludeColumn(x => x.Description);
    }
}
