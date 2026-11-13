using Commerce.Demo.Optimizely.ContentTypes.Shared.Categories;
using EPiServer.Framework;
using EPiServer.Shell.ObjectEditing;
using Geta.Optimizely.Categories;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Queries;

public class CatalogCategorySelectionFactory : ISelectionFactory
{
    private readonly ICategoryContentLoader _categoryContentLoader;

    public CatalogCategorySelectionFactory(ICategoryContentLoader categoryContentLoader)
    {
        _categoryContentLoader = categoryContentLoader;
    }

    public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentException.ThrowIfNullOrEmpty(metadata.PropertyName);

        var category = GetCategoryData(metadata.PropertyName);

        if (category == null)
            return [];

        var categoryItems = _categoryContentLoader.GetChildren<CatalogCategory>(category.ContentLink);

        return categoryItems
            .OrderBy(x => x.SortOrder)
            .Select(x => new SelectItem
            {
                Text = x.Description,
                Value = x.Name
            });
    }

    private CategoryData? GetCategoryData(string name)
    {
        return _categoryContentLoader.GetSiteCategories<CatalogCategory>()
            .FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
