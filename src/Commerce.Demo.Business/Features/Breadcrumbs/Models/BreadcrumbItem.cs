using EPiServer.Core;

namespace Commerce.Demo.Business.Features.Breadcrumbs.Models;

public record BreadcrumbItem(PageData Page, bool IsActive);
