using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;
using EPiServer.Core;
using EPiServer.Filters;

namespace Commerce.Demo.Business.Features.Breadcrumbs.Filters;

internal class FilterBreadcrumbItemForVisitor
{
    private readonly FilterContentForVisitor _baseFilter = new();

    internal bool ShouldFilter(IContent content)
    {
        // returning true here means the content instance *should be* filtered out
        // and *not* appear in the breadcrumb

        if (content is not PageData pageData)
        {
            return _baseFilter.ShouldFilter(content);
        }

        // don't show folder pages in the breadcrumb
        if (pageData is ContainerBase)
        {
            return true;
        }

        // don't show pages that have explicitly had the "Display in Navigation" option disabled
        if (!pageData.VisibleInMenu)
        {
            return true;
        }

        return _baseFilter.ShouldFilter(pageData);
    }
}
