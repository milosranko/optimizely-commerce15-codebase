using Commerce.Demo.Business.Features.Breadcrumbs.Filters;
using Commerce.Demo.Business.Features.Breadcrumbs.Interfaces;
using Commerce.Demo.Business.Features.Breadcrumbs.Models;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;
using Commerce.Demo.Optimizely.Core.Extensions;
using EPiServer;
using EPiServer.Core;
using System.Globalization;

namespace Commerce.Demo.Optimizely.Core.Features.Breadcrumbs.Services;

internal class BreadcrumbsService : IBreadcrumbsService
{
    private readonly IContentLoader _contentLoader;

    public BreadcrumbsService(IContentLoader contentLoader)
    {
        _contentLoader = contentLoader;
    }

    public BreadcrumbData? GetBreadcrumbs(ContentReference activePageRef, CultureInfo language)
    {
        var success = _contentLoader.TryGet(activePageRef, language, out PageData activePage);

        if (!success)
            return null;

        var filter = new FilterBreadcrumbItemForVisitor();

        if (filter.ShouldFilter(activePage))
            return null;

        var items = _contentLoader
            .GetAncestors(activePageRef, language)
            .OfType<PageData>()
            .Reverse()
            .SkipWhile(page => page is not StartPageBase)
            .Where(page => !filter.ShouldFilter(page))
            .Select(x => new BreadcrumbItem(x, false))
            .ToList();

        items.Add(new BreadcrumbItem(activePage, true));

        return new BreadcrumbData(items);
    }
}
