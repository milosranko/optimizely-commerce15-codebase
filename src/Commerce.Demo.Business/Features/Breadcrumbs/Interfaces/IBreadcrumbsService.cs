using Commerce.Demo.Business.Features.Breadcrumbs.Models;
using EPiServer.Core;
using System.Globalization;

namespace Commerce.Demo.Business.Features.Breadcrumbs.Interfaces;

public interface IBreadcrumbsService
{
    BreadcrumbData? GetBreadcrumbs(ContentReference activePageRef, CultureInfo language);
}
