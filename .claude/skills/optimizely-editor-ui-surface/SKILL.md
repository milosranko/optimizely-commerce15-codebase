---
name: optimizely-editor-ui-surface
description: Surface a custom page inside the Optimizely CMS 12 editor UI - an assets-pane gadget ([IFrameComponent]), a view-switcher button on a content or catalog type (ViewConfiguration<T>), or a global menu item (IMenuProvider) - together with the iframe controller, view model and layout it points at. Covers which project each class must live in and why module.config decides it, the base-class trap that leaves a gadget with no url, the id/uri query string the shell appends, role checks that must not redirect to a login page, duplicate buttons from base-type registration, and SortOrder defaults. Use whenever asked to add a gadget, an assets-pane widget, a Registrations-style button on a product or variant, a CMS admin screen behind a menu item, or when a gadget renders empty, a button appears twice, or startup fails with "Unable to find a module by assembly".
---

# Adding an editor UI surface

All three surfaces are the same thing underneath: **an iframe pointed at an ordinary MVC controller in `Wisbar.Web`**, rendered inside the CMS chrome. The differences are where the class that registers the iframe may live, and what the shell appends to the URL.

## Step 0: Pick the surface

| Surface | Editor sees | Registering class | Must live in |
|---|---|---|---|
| Assets-pane gadget | A panel in the right-hand pane, following the current selection | Plain class with `[IFrameComponent]` | **`Wisbar.Web`** |
| View-switcher button | A button beside Preview/Edit on one content type | `ViewConfiguration<T>` + `[ServiceConfiguration]` | Either project |
| Global menu item | An entry in the top CMS/Commerce menu | `IMenuProvider` + `[MenuProvider]` | Either project (house pattern: Business) |

Worked example in this repo, all three files worth reading before writing anything:

```bash
cat src/Wisbar.Web/Infrastructure/Cms/EventRegistrationsComponent.cs
cat src/Wisbar.Business/Features/EventCoordinatorNotifications/Optimizely/EventRegistrationsViewConfiguration.cs
cat src/Wisbar.Web/Controllers/Cms/EventRegistrationsController.cs
ls src/Wisbar.Business/*/Optimizely/*MenuProvider.cs src/Wisbar.Business/*/*/Optimizely/*MenuProvider.cs
```

Full code for every part is in `references/templates.md`.

## Step 1: The controller and views

Live in `Wisbar.Web` next to their siblings: `Controllers/Cms/`, `Models/Cms/`, `Views/<Name>/`.

- Route as `[Route("ui/[controller]")]`, matching `ImportsController`, `PaymentsController`, `PromotedResultsController`.
- The shell appends `?uri=...&id=[content link]` to whatever URL you register. `id` arrives as `id`, `id_workId` or `id_workId_provider` - `ContentReference.TryParse` handles all three, so no `ReferenceConverter` round trip.
- Every link you render inside the iframe must carry `id` forward (`<a href="?id=@Model.ContentId&page=@p">`); the shell only supplies it on the first load.
- Use a local `_Layout.cshtml` under the view folder, **not `_ShellLayout`**: the edit surface already draws the platform navigation, and `_ShellLayout` draws a second copy inside the iframe. Pull in `@ClientResources.RenderResources("ShellCore")` and `("ShellCoreLightTheme")` so the page matches the CMS.
- View models derive from `ShellLayoutModel` (`Wisbar.Web/Models/Layouts`), which supplies `Title`.

**Authorization.** A menu-opened screen can use `[Authorize(Roles = ...)]`, because the menu item hides itself (`IsAvailable` + `AuthorizationPolicy`). A gadget or a view button **cannot hide itself from a user who lacks the role**, and a role challenge there renders a login page inside the CMS chrome. So keep `[Authorize]` for authentication and check the role in code, returning a neutral view model:

```csharp
private bool IsCommerceAdmin => User.IsInRole(CommerceRoles.CommerceAdmins);
```

## Step 2: The gadget

```csharp
[IFrameComponent(Url = "/ui/eventregistrations/summary", Title = "Registrations", ...)]
public sealed class EventRegistrationsComponent
{
}
```

Two rules decide whether this works at all.

**Do not derive from `ComponentDefinitionBase`.** `ComponentAttribute.CreateComponentDefinition` branches on the attributed type, and the branch for a type implementing `IComponentDefinition` returns `Activator.CreateInstance(type)` and throws the attribute away. Verified by probing both shapes through the real attribute:

| Attributed type | Title | SortOrder | Categories | Settings |
|---|---|---|---|---|
| `: ComponentDefinitionBase` | *(empty)* | 100 | *(none)* | **no `urlTemplate`** |
| plain class | `Probe` | 600 | `commerce` | `urlTemplate=..., minHeight, maxHeight` |

A gadget in the first shape starts, registers, and renders an empty untitled panel - nothing errors. Derive from `ComponentDefinitionBase` only if you set `Title`, `Categories`, `SortOrder` and every `Settings` entry in the constructor yourself; there is no reason to prefer that. `IFrameComponentAttribute.WidgetType` is already `epi/shell/component/IFrameContextComponent`.

**It must be declared in `Wisbar.Web`.** For a `Url` that is not an absolute URI, the attribute resolves it through the shell module owning the declaring assembly:

```csharp
Settings["urlTemplate"] = new Uri(Url, UriKind.RelativeOrAbsolute).IsAbsoluteUri
    ? Url : Paths.ToResource(attributedType.Assembly, Url);   // throws if the assembly is in no module
```

`src/Wisbar.Web/module.config` lists `Wisbar.Web` only, so the same class in `Wisbar.Business` fails startup with `ArgumentException: Unable to find a module by assembly 'Wisbar.Business, ...'`, surfacing as an `InitializationException` on Commerce's `MigrationInitializationModule` - a misleading stack that says nothing about your gadget. A `Url` starting with `/` is returned unchanged once the lookup succeeds, so putting the class in `Wisbar.Web` costs nothing. Adding the assembly to `module.config` also fixes it, but pulls that assembly into shell route, localization and view-location scanning; prefer the move.

Other attribute properties worth knowing: `AllowedRoles` (the one value that survives either shape) keeps the gadget out of other editors' panes; `Categories` groups it in the gadget picker; `ReloadOnContextChange` defaults to **true**, so the endpoint is hit on every selection - keep that endpoint cheap; there is no `ForType`, so the gadget renders on every content type and the action must degrade quietly when the selection is not your type.

## Step 3: The view-switcher button

```csharp
[ServiceConfiguration(typeof(ViewConfiguration))]
public sealed class EventRegistrationsViewConfiguration : ViewConfiguration<MeetingVariant>
```

- `ViewType` is loaded as a **literal URL** by `epi-cms/widget/IFrameController`, which appends the same `?uri=...&id=...`. No module resolution happens here, which is why this class may stay in `Wisbar.Business`.
- `Key` must be unique across all view configurations; prefix it (`wisbareventregistrations`).
- `ControllerType = "epi-cms/widget/IFrameController"` is what makes it an iframe view rather than a dojo widget.
- **`SortOrder` defaults to 20000** in `ViewConfiguration<T>`, which parks the button far to the right of the built-in views. Set it explicitly (600 in the example).
- **Register against the base type only.** The shell concatenates a base type's views onto a derived type's descriptor, so registering the same view for both `MeetingVariant` and its derived `EventVariant` shows two identical buttons.

## Step 4: The menu item variant

`IMenuProvider` builds `UrlMenuItem` directly and never resolves a module, so it works from `Wisbar.Business` - see `HangfireMenuProvider`, `ImportsMenuProvider`, `PaymentsMenuProvider`. Set both `IsAvailable` and `AuthorizationPolicy`; the first hides it, the second enforces it. (The attribute form, `[MenuItem]`, does resolve a module, but only for a URL that does not start with `/`.)

## Step 5: Registration and verification

Attributed classes need no DI entry - `[IFrameComponent]`, `[ServiceConfiguration]` and `[MenuProvider]` are all found by scanning. Only the services your controller injects go in `Startup.ConfigureServices`.

A successful build proves nothing here, and neither does a successful startup. Check all of:

```bash
dotnet build src/Wisbar.sln
dotnet run --project src/Wisbar.Web/Wisbar.Web.csproj   # must reach "Application started" with no InitializationException
```

- Open the catalog, select an instance of the target type, confirm the button appears in the view switcher and opens without nested CMS chrome.
- Confirm the gadget shows a **title and content**, not an empty panel (the symptom of the base-class trap).
- Select a content type the gadget does not support and confirm the neutral message, not an error.
- Sign in as an editor without the role and confirm the surface degrades instead of showing a login page.
- Page or click through a link inside the iframe and confirm the content link survives.

To check a component definition without clicking through the CMS, instantiate the attribute against the type in a scratch test and print the resulting definition's `Title`, `SortOrder`, `Categories` and the created component's `Settings`; `references/templates.md` has the probe. Use an absolute `Url` in the probe so it does not need a configured `ServiceLocator`.

## Troubleshooting

| Symptom | Cause |
|---|---|
| Startup throws `Unable to find a module by assembly` (reported on Commerce's migration module) | `[IFrameComponent]` class outside `Wisbar.Web` with a relative `Url` |
| Gadget renders empty, no title, ignores `SortOrder` and sizes | Class derives from `ComponentDefinitionBase`, so the attribute was discarded |
| Two identical buttons on one type | View registered for both a base type and a derived type |
| Button sits far right, after the built-in views | `SortOrder` left at the `ViewConfiguration<T>` default of 20000 |
| Login page inside the CMS chrome | `[Authorize(Roles = ...)]` on a surface that cannot hide itself; check the role in code |
| Platform navigation appears twice | View used `_ShellLayout` instead of a local layout |
| Panel or view goes blank after clicking inside it | A link dropped the `id` query parameter |
| Editor selection does not update the panel | `ReloadOnContextChange` set to false, or the action ignores `id` |
