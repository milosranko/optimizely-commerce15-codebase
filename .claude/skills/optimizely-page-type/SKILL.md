---
name: optimizely-page-type
description: Add a new page type to an Optimizely CMS 12 (.NET) site - the content type class, its base-class choice, the page view model, controller, Razor view, styles, editor thumbnail, and the availability rules that decide where editors can create it. Use whenever asked to create, add, or scaffold a page type, a landing page, a functional page, a settings page, or when a page exists but is missing its controller, template, layout chrome, or SEO fields.
---

# Adding an Optimizely page type

Four files move together: content type, view model, controller, view. Skip the view model's base and the page renders with no header, footer, or metadata. Skip the template and the page cannot be edited at all.

## Step 1: Learn this repo's conventions first

Never assume the layout from another project. Run this and read what comes back:

```bash
# Page types, their base classes and interfaces, controllers, view models
find . -type d \( -name Pages -o -name Controllers \) -not -path "*/node_modules/*" -not -path "*/obj/*"
find . -name "PageBase*.cs" -o -name "PageControllerBase.cs" -o -name "PageViewModel.cs"
find . -name "*ActionFilter.cs" -o -name "PageViewContextFactory.cs"
```

Then read **one existing page end to end** - all four files - and copy its shape. `references/templates.md` holds the full code for this family of projects; the repo you are in is the authority.

Reference layout (Wisbar, and projects derived from it):

| Part | Location |
|---|---|
| Content type | `Wisbar.Optimizely.ContentTypes/Cms/Pages/` |
| Base classes | `.../Cms/Pages/Base/` |
| Marker interfaces | `.../Cms/Pages/Interfaces/` |
| View model | `Wisbar.Web/Models/Pages/` |
| Controller | `Wisbar.Web/Controllers/` |
| View | `Wisbar.Web/Views/<PageTypeName>/Index.cshtml` |
| SCSS partial | `Wisbar.Frontend/src/styles/_name.scss` |
| Editor thumbnail | `Wisbar.Web/wwwroot/images/thumbs/` |

## Step 2: Choose the base class - the decision that matters most

The hierarchy is `PageData` → `PageBase` → `PageBasePublic` → `PageBaseSeo`, with `FunctionalPageBase` branching off `PageBasePublic`. Each level adds editor-facing properties, so picking the wrong one either strands the page without SEO fields or clutters a utility page with them.

| Base | Adds | Use for |
|---|---|---|
| `PageBaseSeo` | SEO block, Open Graph block (on top of everything below) | **Default for any public, indexable content page** |
| `PageBasePublic` | Exclude-from-sitemap, snippets, product redirect, `BodyCss` | Public pages that genuinely need no SEO fields |
| `FunctionalPageBase` | Same, but defaults `ExcludeFromSitemap` to true | Cart, checkout, confirmation, wishlist - utility pages that should never be indexed |
| `PageBase` | Nothing but `ILayoutPageBase` | Rare. Settings and container pages |
| An existing concrete page | Everything that page has | A variant of an existing page - see `ContactPage : GenericPage`, which only overrides `MainContentArea` to change its `[AllowedTypes]` |

Deriving from a concrete page and `override`-ing one property is the cheapest way to make a narrower variant. Check whether that fits before writing a new type from scratch.

Shared properties belong in the base, not copy-pasted across pages. If two page types would both need a new field, put it on the base class they share.

## Step 3: Write the content type

- Fresh, stable GUID: `[guid]::NewGuid()` in PowerShell. Never copy one, never change one - it is the identity existing content is bound to.
- **Every property `virtual`** (or `override` when narrowing an inherited one) - Optimizely's proxies require it.
- `GroupName` on `[ContentType]` picks the editor's creation category: `SystemTabNames.Content` for ordinary content pages, `Globals.Tabs.Specialized` for utility ones. Use the existing constants.
- `[SiteImageUrl("my-page.png")]` for the creation-dialog thumbnail, resolving to `wwwroot/images/thumbs/`.
- Every `ContentArea` and `ContentReference` needs `[AllowedTypes(...)]`. Cap single-item areas with `[ContentAreaMaxItemCount(1)]`.
- `[Display(Name, Description, GroupName, Order)]` on every property, `Order` in tens.
- `[CultureSpecific]` on editorial text that varies per language.
- Implement marker interfaces when the behavior applies: `IExcludeFromSitemap`, `ISearchable`, `IListable`, `ISortOptions`, `IContainerPage`. Check the interfaces folder before inventing a property that already has a contract.
- `SetDefaultValues(ContentType)` for sensible defaults on creation, always calling `base` first.
- `DisplayName` and every `Description` are editor-facing copy: **no em dashes, use hyphens**, written as instruction to an editor.

## Step 4: View model - or the page loses its chrome

Derive from `PageViewModel<TPage>`. This is not cosmetic. `PageContextActionFilter` populates `Layout` only for models matching `IPageViewModel<PageBase>`; a hand-rolled model silently renders with no header, footer, metadata, or snippets, and `IsMember` stays false.

```csharp
public record MyPageViewModel : PageViewModel<MyPage>
{
    public MyPageViewModel(MyPage currentPage) : base(currentPage) { }

    public string? ResolvedThing { get; init; }
}
```

A page with no computed data can use `PageViewModel.Create(currentPage)` and needs no model class of its own.

Compute in the controller, not the view: resolved URLs, filtered lists, flags. The view model is where that lands.

## Step 5: Controller

- `MyPageController : PageControllerBase<MyPage>` with an `Index(MyPage currentPage)` action. No route registration - Optimizely routes pages by content type.
- Constructor-inject services. Never `ServiceLocator` in new code.
- Naming is the convention that wires it up: `<PageType>Controller` + `Views/<PageType>/Index.cshtml`.
- Returning `NotFound()` re-executes into the 404 page via `UseStatusCodePagesWithReExecute`. Guard those checks with `!PageHelpers.IsInEditMode()` so editors can still open unfinished pages.

## Step 6: View

`Views/<PageTypeName>/Index.cshtml`, typed to the view model. A page type with no template cannot be edited in On-Page Editing even though the site builds.

- The layout comes from `_ViewStart.cshtml`; do not set `Layout` unless this page genuinely needs a different one.
- `_ViewImports.cshtml` already brings in the page/block namespaces, tag helpers, `ICurrentIdentity`, and `IAccountUrls`. Do not re-import, and do not resolve sign-in URLs or authentication state by hand - use the injected services. `User.Identity.IsAuthenticated` is true for a signed-in CMS editor, which is why `ICurrentIdentity` exists.
- `@Html.EditAttributes(x => x.CurrentPage.Prop)` on elements you write yourself; `@Html.PropertyFor(x => x.CurrentPage.Area, new { CustomTag = "article", CssClass = "..." })` for content areas.
- Guard optional markup with `|| Model.IsInEditMode` so empty fields stay clickable for editors while publishing nothing.

## Step 7: Styles and thumbnail

- SCSS partial `src/Wisbar.Frontend/src/styles/_my-page.scss`, registered in `main.scss` with `@use 'styles/my-page';`. BEM rooted on the page (`generic-page__cover-frame`).
- Add the thumbnail PNG matching neighbours' naming (`generic-page.png`). If you cannot produce artwork, say so rather than leaving a broken `[SiteImageUrl]` reference.
- Build the frontend with `npm run build` from `src/Wisbar.Frontend/`. Fix lint at the root; never add `eslint-disable`.

## Step 8: Control where it can be created

A new page type is offered everywhere by default. Decide deliberately:

- `[AvailableContentTypes(Include = [typeof(ChildPage)])]` on the parent restricts what can be created under it.
- `[AvailableContentTypes(Availability.None)]` marks a leaf that takes no children.
- `[Access(Roles = Roles.ContentAdmins)]` restricts who may create the type at all - used on `StartPage`.

Ask where the page should be creatable if the request does not say.

## Step 9: Verify

```bash
dotnet build src/Wisbar.sln
cd src/Wisbar.Frontend && npm run build
```

A green build proves nothing about the editor. Report clearly which of these you actually checked:

- Type appears in the creation dialog, in the right group, with its thumbnail, only under intended parents.
- Page renders with full chrome - header, footer, title, metadata. Empty chrome means the view model base is wrong.
- Properties edit inline in On-Page Editing; content areas accept only their allowed types.
- SEO and Open Graph fields present on a public page; sitemap inclusion matches intent.
- Publishing, then viewing as an anonymous visitor, gives the intended result.

## Checklist

- [ ] Base class chosen deliberately from the table in Step 2
- [ ] Fresh GUID, `GroupName`, `[SiteImageUrl]` set
- [ ] All properties `virtual`/`override`; `[AllowedTypes]` on every area and reference
- [ ] `[Display]` on every property, hyphens not em dashes in editor copy
- [ ] View model derives from `PageViewModel<TPage>`
- [ ] Controller derives from `PageControllerBase<TPage>`, services injected
- [ ] View at `Views/<PageType>/Index.cshtml` with edit attributes
- [ ] SCSS partial created and registered in `main.scss`
- [ ] Thumbnail PNG added
- [ ] Creation availability and access roles decided
- [ ] Solution and frontend both build

Full code templates: `references/templates.md`.
