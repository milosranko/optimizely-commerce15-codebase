using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog;
using Microsoft.Extensions.Logging;

namespace Commerce.Demo.Optimizely.ContentTypes.Extensions;

public static class ProductContentExtensions
{
    public static IEnumerable<int> ProductCategories(this ProductContent product)
    {
        var contentLoader = ServiceLocator.Current.GetInstance<IContentRepository>();
        var linksRepository = ServiceLocator.Current.GetInstance<IRelationRepository>();
        var referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();
        var allRelations = linksRepository.GetParents<Relation>(product.ContentLink);
        var categories = allRelations.OfType<NodeRelation>().ToList();
        var parentCategories = new List<CatalogContentBase>();
        var log = ServiceLocator.Current.GetInstance<ILogger>();

        try
        {
            if (categories.Count != 0)
            {
                parentCategories.AddRange(categories
                    .Where(nodeRelation => nodeRelation.Parent != referenceConverter.GetRootLink())
                    .Select(nodeRelation => contentLoader.Get<CatalogContentBase>(nodeRelation.Parent, LanguageSelector.AutoDetect()))
                    .Where(parentCategory => parentCategory != null));
            }
            else if (product.ParentLink != null && product.ParentLink != referenceConverter.GetRootLink())
            {
                var parentCategory = contentLoader.Get<CatalogContentBase>(product.ParentLink, LanguageSelector.AutoDetect());
                parentCategories.Add(parentCategory);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "An error occurred while retrieving product categories for product with ID {ProductId}", product.ContentLink.ID);
        }

        return parentCategories.Select(x => x.ContentLink.ID);
    }
}
