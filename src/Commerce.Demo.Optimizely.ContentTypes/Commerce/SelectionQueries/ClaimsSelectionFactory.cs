using EPiServer.Framework;
using EPiServer.Shell.ObjectEditing;
using Mediachase.MetaDataPlus;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Queries;

public class ClaimsSelectionFactory : ISelectionFactory
{
    IEnumerable<ISelectItem> _items = [];

    public ClaimsSelectionFactory()
    {
        var claimMetaField = Mediachase.MetaDataPlus.Configurator.MetaField.Load(MetaDataContext.Instance, "PurchaseClaims");

        if (claimMetaField is null || claimMetaField.Dictionary.Count == 0)
            return;

        for (int i = 0; i < claimMetaField.Dictionary.Count; i++)
        {
            var item = claimMetaField.Dictionary[i];
            if (item is null)
                continue;

            _items = _items.Append(new SelectItem
            {
                Text = item.Value,
                Value = item.Id
            });
        }
    }

    public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentException.ThrowIfNullOrEmpty(metadata.PropertyName);

        return _items;
    }
}
