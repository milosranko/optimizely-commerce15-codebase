using EPiServer.Framework;
using EPiServer.Shell.ObjectEditing;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Queries;

public class LabelSelectionFactory : ISelectionFactory
{
    public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentException.ThrowIfNullOrEmpty(metadata.PropertyName);

        string[] labels = ["Label 1", "label 2", "label 3"];

        return labels.Select(label => new SelectItem
        {
            Text = label,
            Value = label
        });
    }
}
