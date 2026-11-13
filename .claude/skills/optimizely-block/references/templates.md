# Block templates

Namespaces below use the Wisbar layout. Substitute the host project's namespaces and base classes - the repo you are working in is the authority, these are the shape.

## A. Simple block (partial view, no component)

### Content type

```csharp
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Web;
using System.ComponentModel.DataAnnotations;
using Wisbar.Optimizely.ContentTypes.Cms.Blocks.Base;
using Wisbar.Optimizely.Core.Attributes;
using Wisbar.Optimizely.Core.Constants;

namespace Wisbar.Optimizely.ContentTypes.Cms.Blocks;

[ContentType(
    DisplayName = "Callout",
    GUID = "{PASTE-A-FRESH-GUID-HERE}",
    GroupName = Globals.Tabs.Default,
    Description = "Short highlighted message with an optional link. Intended for the page's Main Content Area.")]
[SiteImageUrl("callout-block.png")]
public class CalloutBlock : BlockBase
{
    [CultureSpecific]
    [Display(
        Name = "Heading",
        GroupName = SystemTabNames.Content,
        Order = 10)]
    public virtual string? Heading { get; set; }

    [CultureSpecific]
    [Display(
        Name = "Body",
        Description = "Message shown under the heading.",
        GroupName = SystemTabNames.Content,
        Order = 20)]
    public virtual XhtmlString? Body { get; set; }
}
```

### View - `Views/Shared/Blocks/CalloutBlock.cshtml`

```cshtml
@using Wisbar.Optimizely.Core.Helpers
@using Wisbar.Optimizely.ContentTypes.Cms.Blocks
@model CalloutBlock

@if (!string.IsNullOrWhiteSpace(Model.Heading) || PageHelpers.IsInEditMode())
{
    <h2 epi-property="Heading" class="callout__heading" />
}

<div class="callout__body">
    @Html.PropertyFor(x => x.Body)
</div>
```

## B. Block with a view component

Use when the block needs injected services or computed data.

### Content type

Same shape as A. Properties that need resolving (images, `LinkBlock`, references to other content) are what pull you here.

```csharp
[Display(Name = "Image", GroupName = SystemTabNames.Content, Order = 10)]
[UIHint(UIHint.Image)]
[Required]
public virtual ContentReference? Image { get; set; }

[Display(
    Name = "CTA Link",
    Description = "Button label and destination URL.",
    GroupName = SystemTabNames.Content,
    Order = 20)]
public virtual LinkBlock? CtaLink { get; set; }
```

### View model - `Wisbar.Models/Blocks/CalloutBlockViewModel.cs`

A record deriving from `BlockViewModel<TBlock>`, which supplies `CurrentBlock` and `IsEditing`.

```csharp
using Wisbar.Optimizely.ContentTypes.Cms.Blocks;

namespace Wisbar.Models.Blocks;

public record CalloutBlockViewModel : BlockViewModel<CalloutBlock>
{
    public CalloutBlockViewModel(CalloutBlock block) : base(block) { }

    public ImageFileViewModel? Image { get; init; }
    public LinkBlockViewModel? Link { get; init; }
}
```

### Component - `Wisbar.Web/Components/CalloutBlockComponent.cs`

```csharp
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Mvc;
using Wisbar.Models.Blocks;
using Wisbar.Optimizely.ContentTypes.Cms.Blocks;
using Wisbar.Web.Components.Base;
using Wisbar.Web.Helpers;

namespace Wisbar.Web.Components;

public class CalloutBlockComponent : BlockComponentBase<CalloutBlock, CalloutBlockViewModel>
{
    private readonly IContentLoader _contentLoader;
    private readonly IUrlResolver _urlResolver;

    public CalloutBlockComponent(IContentLoader contentLoader, IUrlResolver urlResolver)
    {
        _contentLoader = contentLoader;
        _urlResolver = urlResolver;
    }

    protected override IViewComponentResult InvokeComponent(CalloutBlock block)
    {
        var viewModel = new CalloutBlockViewModel(block)
        {
            Image = ViewModelBuilderHelpers.BuildImageViewModel(block.Image, _urlResolver, _contentLoader),
            Link = ViewModelBuilderHelpers.BuildLinkBlockViewModel(block.CtaLink, _urlResolver),
        };

        return View(viewModel);
    }
}
```

Use `AsyncBlockComponentBase<TBlock, TModel>` with `InvokeComponentAsync` when the data source is async (search, catalog, HTTP).

### View - still `Views/Shared/Blocks/CalloutBlock.cshtml`

The view location expander rewrites the component's `Components/CalloutBlockComponent` view name to this folder, so component views sit beside partial views.

```cshtml
@using Wisbar.Models.Blocks
@model CalloutBlockViewModel
@Html.FullRefreshPropertiesMetaData([nameof(Model.CurrentBlock.Image)])

<article class="callout">
    @if (Model.Image != null || Model.IsEditing)
    {
        <div epi-property="CurrentBlock.Image"
             class="callout__image"
             role="img"
             aria-label="@Model.Image?.AltText"
             style="background-image: url('@Model.Image?.ImageUrl')">
        </div>
    }

    @if (!string.IsNullOrWhiteSpace(Model.CurrentBlock.Heading) || Model.IsEditing)
    {
        <h2 epi-property="CurrentBlock.Heading" class="callout__heading" />
    }

    @if (Model.Link != null || Model.IsEditing)
    {
        @* stretched-link is left off in edit mode, where it would cover the other
           properties and swallow the editor's clicks on them. *@
        <a href="@Model.Link?.Url"
           class="callout__btn@(Model.IsEditing ? "" : " stretched-link")"
           @(Model.Link != null && Model.Link.IsExternal ? "target=\"_blank\" rel=\"noopener\"" : "")
           @Html.EditAttributes(x => x.CurrentBlock.CtaLink)>
            @Model.Link?.Text
        </a>
    }
</article>
```

## C. Container block

A block whose children are other blocks. The container holds a `ContentArea` restricted to the child type; each child is its own block content type with its own view.

```csharp
[Display(
    Name = "Cards",
    Description = "Add Callout Card blocks here. Two are visible at a time on desktop.",
    GroupName = SystemTabNames.Content,
    Order = 20)]
[AllowedTypes(typeof(CalloutCardBlock))]
public virtual ContentArea? Items { get; set; }
```

Container view renders the area and lets each child render itself:

```cshtml
@Html.PropertyFor(x => x.CurrentBlock.Items)
```

For a repeating list of plain links, prefer `LinkItemCollection` over a child block type - fewer content items for editors to manage. See `PracticeAreaGridBlock`.

## D. Optional interfaces

Implement on the block when the content-area renderer should wrap children in specific markup:

- `IContentAreaCustomHtml` - `ChildrenCustomTagName`, `ChildrenCssClass`
- `IPreviewCustomHtml` - first/second level tag and CSS class in preview

## Display options

Registered in `Startup` against `Globals.ContentAreaTags`; `DisplayOptionContentAreaRenderer` turns the editor's choice into a grid class:

| Tag | Class |
|---|---|
| `full` | `col-12` |
| `wide` | `col-8` |
| `half` | `col-6` |
| `narrow` | `col-4` |

Style the block so it holds up at every width it will be offered at.
