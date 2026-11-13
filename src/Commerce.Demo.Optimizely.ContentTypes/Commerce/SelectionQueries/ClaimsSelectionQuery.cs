using EPiServer.ServiceLocation;
using EPiServer.Shell.ObjectEditing;
using Mediachase.MetaDataPlus;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Queries;

[ServiceConfiguration(typeof(ISelectionQuery))]
public class ClaimsSelectionQuery : ISelectionQuery
{
    IEnumerable<SelectItem> _items = [];

    public ClaimsSelectionQuery()
    {
        var claimMetaField = Mediachase.MetaDataPlus.Configurator.MetaField.Load(MetaDataContext.Instance, "Claims");

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

    public ISelectItem GetItemByValue(string value)
    {
        return _items.FirstOrDefault(i => i.Value.ToString().Equals(value));
    }

    public IEnumerable<ISelectItem> GetItems(string query)
    {
        return _items.Where(i => i.Text.StartsWith(query, StringComparison.OrdinalIgnoreCase));
    }
}
