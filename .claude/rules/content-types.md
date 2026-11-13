---
globs: src/Wisbar.Optimizely.ContentTypes/**,src/Wisbar.Optimizely.Core/**
---

# Optimizely content types & rendering rules

## Content modeling

- Every new content type must use `[ContentType]` with a stable GUID.
- All public properties in `[ContentType]` classes must be `virtual` (required for Optimizely's proxy-based lazy loading).
- Shared page properties belong in the existing base page type — check for it before adding a property directly to a page model.
- Do not rename or move content-type classes without considering GUID continuity and editor impact.
- All `IList<ContentReference>` and `ContentReference` properties must have `[AllowedTypes(...)]` to restrict editor choices to appropriate content types.
- Keep display metadata, grouping, and ordering aligned with existing patterns for editor-facing models.

## Rendering

- New page types require a rendering template — pages without one are not editable in On-Page Editing.
- Use the existing `PageController<T>` + Razor view pattern for pages.
- For blocks: use a partial view for simple rendering; use `BlockComponent<T>` when the block needs constructor-injected services or controller-like logic.
- If `TemplateDescriptor`, tags, or display options are involved, update all dependent templates and registration points together.

## Display options

- Content area tags are defined in `Globals.ContentAreaTags`: Full, Wide, Half, Narrow.
- Always check this class before adding a new tag — don't introduce ad-hoc tag strings.

## Editor-facing changes

- Verify both rendering behavior and On-Page Editing behavior after every content type change — a successful build does not mean the editor works.

## Debugging checklist

- **Editor issue:** check content type definition → GUID continuity → template existence → display options → environment config.
- **Rendering issue:** check template resolution → tags → `SiteViewEngineLocationExpander` → content-area context.
