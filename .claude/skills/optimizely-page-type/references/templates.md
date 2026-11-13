# Page type templates

Namespaces below use the Wisbar layout. Substitute the host project's namespaces and base classes - the repo you are working in is the authority, these are the shape.

## The four files

For a page type called `PracticeAreaPage`:

```
Wisbar.Optimizely.ContentTypes/Cms/Pages/PracticeAreaPage.cs
Wisbar.Web/Models/Pages/PracticeAreaPageViewModel.cs
Wisbar.Web/Controllers/PracticeAreaPageController.cs
Wisbar.Web/Views/PracticeAreaPage/Index.cshtml
```

The controller and view folder names are what bind the template to the type. Keep them exactly `<PageType>Controller` and `Views/<PageType>/`.

## A. Content type

```csharp
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using Wisbar.Optimizely.ContentTypes.Cms.Blocks;
using Wisbar.Optimizely.ContentTypes.Cms.Media.Base;
using Wisbar.Optimizely.ContentTypes.Cms.Pages.Base;
using Wisbar.Optimizely.Core.Attributes;
using Wisbar.Optimizely.Core.Constants;

namespace Wisbar.Optimizely.ContentTypes.Cms.Pages;

[ContentType(
    DisplayName = "Practice Area Page",
    GUID = "{PASTE-A-FRESH-GUID-HERE}",
    GroupName = SystemTabNames.Content,
    Description = "Landing page for a single practice area: a cover header and a content area for rich-text and feature blocks.")]
[SiteImageUrl("practice-area-page.png")]
public class PracticeAreaPage : PageBaseSeo
{
    #region Content tab

    [CultureSpecific]
    [Required]
    [Display(
        Name = "Heading",
        Description = "Page title shown as the H1 in the cover header.",
        GroupName = SystemTabNames.Content,
        Order = 10)]
    public virtual string? Heading { get; set; }

    [Display(
        Name = "Background Images",
        Description = "Cover background. Add one or more images (multiple supports visitor-group personalization).",
        GroupName = SystemTabNames.Content,
        Order = 20)]
    [AllowedTypes(typeof(ImageFileBase))]
    public virtual ContentArea? BackgroundImages { get; set; }

    [CultureSpecific]
    [Display(
        Name = "Main Content Area",
        Description = "Page body. Add Content Section and Generic Page Card blocks.",
        GroupName = SystemTabNames.Content,
        Order = 30)]
    [AllowedTypes(typeof(ContentSectionBlock), typeof(GenericPageCardBlock))]
    public virtual ContentArea? MainContentArea { get; set; }

    #endregion
}
```

### Defaults on creation

```csharp
public override void SetDefaultValues(ContentType contentType)
{
    base.SetDefaultValues(contentType);

    ExcludeFromSitemap = true;
}
```

### Narrowing an existing page instead of writing a new one

`ContactPage` is a `GenericPage` that only changes which blocks its content area accepts:

```csharp
[ContentType(
    DisplayName = "Contact Page",
    GUID = "{FRESH-GUID}",
    GroupName = SystemTabNames.Content,
    Description = "Contact page with a cover header and content area.")]
[SiteImageUrl("contact-page.png")]
public class ContactPage : GenericPage
{
    [CultureSpecific]
    [Display(
        Name = "Main Content Area",
        Description = "Page body. Add the contact form and content blocks.",
        GroupName = SystemTabNames.Content,
        Order = 40)]
    [AllowedTypes(typeof(ContactFormBlock), typeof(ContactPageContentBlock))]
    public override ContentArea? MainContentArea { get; set; }
}
```

The subclass reuses the parent's controller and view unless you add its own.

## B. View model

`PageViewModel<T>` supplies `CurrentPage`, `Layout`, `IsInEditMode`, and `IsMember`. Deriving from it is what lets `PageContextActionFilter` fill in the layout.

```csharp
using Wisbar.Optimizely.ContentTypes.Cms.Pages;

namespace Wisbar.Web.Models.Pages;

public record PracticeAreaPageViewModel : PageViewModel<PracticeAreaPage>
{
    public PracticeAreaPageViewModel(PracticeAreaPage currentPage) : base(currentPage) { }

    public string? CoverImageUrl { get; init; }
    public bool ApplyBackgroundColor { get; init; }
}
```

## C. Controller

```csharp
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Mvc;
using Wisbar.Optimizely.ContentTypes.Cms.Pages;
using Wisbar.Web.Controllers.Base;
using Wisbar.Web.Models.Pages;

namespace Wisbar.Web.Controllers;

public class PracticeAreaPageController : PageControllerBase<PracticeAreaPage>
{
    private readonly IUrlResolver _urlResolver;

    public PracticeAreaPageController(IUrlResolver urlResolver)
    {
        _urlResolver = urlResolver;
    }

    public IActionResult Index(PracticeAreaPage currentPage)
    {
        var backgroundItem = currentPage.BackgroundImages?.FilteredItems.FirstOrDefault();

        var model = new PracticeAreaPageViewModel(currentPage)
        {
            CoverImageUrl = backgroundItem != null ? _urlResolver.GetUrl(backgroundItem.ContentLink) : default,
            ApplyBackgroundColor = backgroundItem is null
        };

        return View(model);
    }
}
```

Use `FilteredItems` rather than `Items` on a content area read in a controller - it applies publication status and visitor-group filtering.

### No computed data

```csharp
public IActionResult Index(PracticeAreaPage currentPage) => View(PageViewModel.Create(currentPage));
```

### Async

```csharp
public async Task<IActionResult> Index(PracticeAreaPage currentPage)
{
    var results = await _service.GetAsync(currentPage.Something, HttpContext.RequestAborted);
    return View(new PracticeAreaPageViewModel(currentPage) { Results = results });
}
```

### 404 for a page that should not be publicly reachable

```csharp
if (!PageHelpers.IsInEditMode() && !IsReachable(currentPage))
{
    return NotFound();
}
```

`UseStatusCodePagesWithReExecute` turns that into the site's NotFound page. The edit-mode guard keeps editors able to open pages that are still being staged.

## D. View - `Views/PracticeAreaPage/Index.cshtml`

```cshtml
@using Wisbar.Web.Models.Pages
@model PracticeAreaPageViewModel

<header class="practice-area__cover">
    @if (!string.IsNullOrEmpty(Model.CoverImageUrl) || Model.IsInEditMode)
    {
        <div class="practice-area__cover-bg" aria-hidden="true"
             @Html.EditAttributes(x => x.CurrentPage.BackgroundImages)
             @if (!string.IsNullOrEmpty(Model.CoverImageUrl)) { <text>style="background-image: url('@Model.CoverImageUrl');"</text> }>
        </div>
    }

    <div class="container">
        <div class="practice-area__cover-frame">
            <h1 @Html.EditAttributes(x => x.CurrentPage.Heading)>@Model.CurrentPage.Heading</h1>
        </div>
    </div>
</header>

<div class="practice-area__content">
    <div class="container">
        @Html.PropertyFor(x => x.CurrentPage.MainContentArea, new { CustomTag = "article", CssClass = "practice-area__frame" })
    </div>
</div>
```

Notes:

- `_ViewStart.cshtml` already sets the layout. Do not set it again.
- `_ViewImports.cshtml` already has the page and block namespaces, the tag helpers, `@inject ICurrentIdentity Identity`, and `@inject IAccountUrls AccountUrls`. Use `Identity.IsMember` for storefront chrome decisions and `AccountUrls.LoginUrl()` for sign-in links - never `User.Identity.IsAuthenticated`, which is true for signed-in CMS editors, and never a hardcoded `/login`.
- Conditional inline styles use the `<text>` block form shown above; a bare `@if` inside an attribute list will not compile.

## Base class reference

| Class | Inherits | Adds |
|---|---|---|
| `PageBase` | `PageData`, `ILayoutPageBase` | Nothing - the marker for layout-capable pages |
| `PageBasePublic` | `PageBase` | `ExcludeFromSitemap`, `RedirectToProductPage`, snippets (`InheritSnippetsFromSettings` defaults true), `BodyCss` |
| `PageBaseSeo` | `PageBasePublic` | `SeoBlock`, `OpenGraphBlock` on the SEO tab |
| `StartPageBase` | `PageBaseSeo` | Site root marker |
| `FunctionalPageBase` | `PageBasePublic` | Defaults `ExcludeFromSitemap` to true |
| `NotFoundPageBase` | `FunctionalPageBase` | `[AvailableContentTypes(Availability.None)]` plus 404 defaults |
| `ContainerBase` | `PageBase`, `IContainerPage` | Structural folder pages |
| `SiteSettingsPageBase` | `PageData`, `ISnippets` | Settings container, deliberately outside the public hierarchy |

## Marker interfaces

Check `Cms/Pages/Interfaces/` before adding a property that already has a contract:

| Interface | Contract |
|---|---|
| `IExcludeFromSitemap` | `bool ExcludeFromSitemap` |
| `ISearchable` | `bool ExcludeFromSearchResults`, `string? SearchSummaryText` |
| `IListable` | `int PageSize` |
| `ISortOptions` | Sort configuration for listing pages |
| `IContainerPage` | Structural container |
| `ISnippets` / `ISnippetsWithInheritance` | Snippet content area, optionally inheriting site settings |
| `ICookiesConsent` | Consent banner configuration |

## Availability attributes

```csharp
[AvailableContentTypes(Include = [typeof(PracticeAreaPage)])]   // only these under this parent
[AvailableContentTypes(Availability.None)]                      // leaf, takes no children
[Access(Roles = Roles.ContentAdmins)]                           // who may create the type
```

`Roles` comes from `using static EPiServer.OptimizelyIdentity.OptimizelyIdentityDefaults;` - see `StartPage`.
