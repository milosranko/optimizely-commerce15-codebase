---
name: optimizely-block
description: Add a new block content type to an Optimizely CMS 12 (.NET) site - the content type class, view model, view component, Razor view, styles, editor thumbnail, and the AllowedTypes wiring that makes it selectable. Use whenever asked to create, add, or scaffold a block, a container block, a card block, a CMS component editors drop into a content area, or when a block exists but is missing its template, thumbnail, or edit-mode support.
---

# Adding an Optimizely block

A block is not one file. A block that builds but has no template is invisible in On-Page Editing, and a block no content area allows is unreachable for editors. Ship all the parts.

## Step 1: Learn this repo's conventions first

Never assume the layout from another project. Run this and read what comes back:

```bash
# Where blocks, view models, components, views, and thumbnails live
find . -type d \( -name Blocks -o -name Components \) -not -path "*/node_modules/*" -not -path "*/obj/*"
find . -name "BlockBase.cs" -o -name "BlockComponentBase.cs" -o -name "BlockViewModel.cs"
find . -name "Globals.cs" -path "*Constant*"
```

Then read **two existing blocks end to end** - one simple, one with a component - and copy their shape exactly: namespaces, base classes, tab constants, property attribute ordering, comment density. The templates in `references/templates.md` are the shape this family of projects uses; the repo you are in is the authority.

Reference layout (Wisbar, and projects derived from it):

| Part | Location |
|---|---|
| Content type | `Wisbar.Optimizely.ContentTypes/Cms/Blocks/` |
| Nested/local blocks (LinkBlock, IconLinkBlock) | `.../Cms/Blocks/Local/` |
| View model | `Wisbar.Models/Blocks/` |
| View component | `Wisbar.Web/Components/` |
| Razor view | `Wisbar.Web/Views/Shared/Blocks/` |
| SCSS partial | `Wisbar.Frontend/src/styles/_name.scss` |
| Editor thumbnail | `Wisbar.Web/wwwroot/images/thumbs/` |

## Step 2: Pick simple partial or view component

**Simple partial** - the view binds straight to the block type (`@model MyBlock`). Use when the view needs nothing but the block's own properties. See `ContentSectionBlock` + `ContentSectionBlock.cshtml`.

**View component + view model** - use when the block needs injected services (`IContentLoader`, `IUrlResolver`, a search or pricing service), resolved URLs, or any computed data. Three files instead of one: block, `BlockViewModel<TBlock>` record, `BlockComponentBase<TBlock, TModel>` component. See `TodaysFeatureCardBlock`.

Do not reach for a component just to resolve one image URL if a helper already exists; check for a `ViewModelBuilderHelpers`-style class first.

## Step 3: Write the content type

- Derive from the project's `BlockBase`, not `BlockData` directly. In this family `BlockBase` is what `BlockPreviewController` binds to (`IRenderTemplate<BlockBase>`, `Inherited = true`), so deriving from it is what gives the block its editor preview for free.
- **Every property must be `virtual`** - Optimizely's proxies need it for lazy loading and change tracking.
- Fresh, stable GUID: `[guid]::NewGuid()` in PowerShell. Never copy one from another block, and never change it later - the GUID is the identity editors' existing content is bound to.
- `GroupName` comes from the project's tab constants (`Globals.Tabs.*`). Do not invent a tab string.
- Every `ContentReference`, `IList<ContentReference>`, and `ContentArea` property needs `[AllowedTypes(...)]`.
- `[CultureSpecific]` on editorial text that should vary per language.
- `[Display(Name, Description, GroupName, Order)]` on every property, `Order` in tens so later properties can slot between.
- `DisplayName` and every `Description` are editor-facing copy: **no em dashes, use hyphens**, and write them as instructions to an editor ("Add Today's Feature Card blocks here"), not as notes to a developer.

## Step 4: Template, or the block is not editable

A page or block with no rendering template cannot be edited in On-Page Editing, even if the site builds.

- View file name must match the block type name: `MyBlock.cshtml` in the blocks folder. The project's `SiteViewEngineLocationExpander` maps a component's `Components/MyBlockComponent` view name back to that folder, which is why component views live beside partial views rather than under `Views/Shared/Components/`.
- Use `epi-property="PropertyName"` (or `CurrentBlock.PropertyName` from a view model) for inline-editable text, and `@Html.EditAttributes(x => x.Prop)` when you need the edit hooks on an element you are writing yourself.
- Guard optional output with `|| Model.IsEditing` so an empty field still gives the editor something to click, while publishing nothing for visitors.
- Add `@Html.FullRefreshPropertiesMetaData([nameof(Model.CurrentBlock.Image)])` for properties whose change cannot be re-rendered in place (images, layout-affecting choices).
- Anything that would swallow editor clicks - a Bootstrap `stretched-link`, a full-card overlay - must be switched off when `Model.IsEditing`.

## Step 5: Styles

- One SCSS partial per block: `src/Wisbar.Frontend/src/styles/_block-name.scss`, then register it in `main.scss` with `@use 'styles/block-name';` in the existing alphabetical-ish position.
- BEM class names rooted on the block (`todays-feature__card-title`).
- Build with `npm run build` from `src/Wisbar.Frontend/`. Fix lint errors at the root; never add `eslint-disable`.

## Step 6: Thumbnail

`[SiteImageUrl("my-block.png")]` resolves to `wwwroot/images/thumbs/my-block.png`. Add the PNG, matching the naming of its neighbours (`todays-feature-card-block.png`). Without it the editor's block picker shows a generic placeholder. If you cannot produce artwork, say so explicitly rather than leaving a broken reference.

## Step 7: Make it reachable

A new block is invisible to editors until some content area accepts it. Add it to the `[AllowedTypes(...)]` of the pages or container blocks that should offer it - for example `GenericPage.MainContentArea`. Ask which surfaces should offer it if the request does not say.

If the block should be width-selectable, confirm the display option tags in `Globals.ContentAreaTags` (Full, Wide, Half, Narrow) cover the need. Do not introduce an ad-hoc tag string.

## Step 8: Verify

```bash
dotnet build src/Wisbar.sln
cd src/Wisbar.Frontend && npm run build
```

A green build proves nothing about the editor. Report clearly which of these you actually checked:

- Block appears in the picker, under the right tab, with its thumbnail.
- It can be added to the intended content area.
- Each property edits inline; images trigger a full refresh rather than a stale render.
- Empty optional fields render nothing on the published page.
- Front end renders at every display option width the block is offered at.

## Checklist

- [ ] Derives from `BlockBase`; all properties `virtual`
- [ ] Fresh GUID, `GroupName` from the tab constants, `[SiteImageUrl]` set
- [ ] `[AllowedTypes]` on every ContentReference / ContentArea property
- [ ] `[Display]` on every property with hyphens, not em dashes, in editor copy
- [ ] View exists, named after the block, in the blocks view folder
- [ ] Edit-mode paths handled: `IsEditing` guards, edit attributes, full-refresh metadata
- [ ] SCSS partial created and registered in `main.scss`
- [ ] Thumbnail PNG added
- [ ] Block added to the `[AllowedTypes]` of the content areas that should offer it
- [ ] Solution and frontend both build

Full code templates for both flavours: `references/templates.md`.
