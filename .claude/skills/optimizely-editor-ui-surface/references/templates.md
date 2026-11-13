# Templates

Working code for each part, taken from the event registrations surfaces. Replace `EventRegistrations` / `MeetingVariant` with your own names.

## 1. Assets-pane gadget

`src/Wisbar.Web/Infrastructure/Cms/EventRegistrationsComponent.cs`

```csharp
using EPiServer.Authorization;
using EPiServer.Shell.ViewComposition;

namespace Wisbar.Web.Infrastructure.Cms;

/// <summary>
/// The assets-pane gadget. IFrameContextComponent reloads the iframe on every context change with
/// ?uri=...and id=[content link], so this follows the catalog selection.
/// </summary>
// Deliberately not derived from ComponentDefinitionBase: for a type that implements
// IComponentDefinition the attribute is discarded except AllowedRoles, leaving no urlTemplate.
//
// Must live in Wisbar.Web: IFrameComponentAttribute resolves a relative Url through the shell module
// that owns the declaring assembly, and module.config registers Wisbar.Web only.
[IFrameComponent(
    Url = "/ui/eventregistrations/summary",
    Title = "Registrations",
    Description = "Registrations logged for the selected event variant.",
    Categories = "commerce",
    AllowedRoles = CommerceRoles.CommerceAdmins,
    MinHeight = 120,
    MaxHeight = 400,
    SortOrder = 600)]
public sealed class EventRegistrationsComponent
{
}
```

| Property | Notes |
|---|---|
| `Url` | Relative paths are resolved against the declaring assembly's shell module. Absolute URIs skip that lookup entirely. |
| `Title` / `Description` | Shown in the panel heading and the gadget picker. |
| `Categories` | Comma-separated grouping in the picker, e.g. `commerce`, `content`. |
| `AllowedRoles` | Comma-separated. The only attribute value that survives even the `ComponentDefinitionBase` shape. |
| `MinHeight` / `MaxHeight` | Defaults 100 / 500. The pane is narrow, so design for a single column. |
| `ReloadOnContextChange` | Default true: the URL is re-fetched on every selection. |
| `KeepUrlOnContextChange` | Default false. |
| `PlugInAreas` | Optional placement, e.g. `PlugInArea.AssetsDefaultGroup`. |
| `SortOrder` | Ordering within the pane. |

## 2. View-switcher button

`src/Wisbar.Business/Features/EventCoordinatorNotifications/Optimizely/EventRegistrationsViewConfiguration.cs`

```csharp
using EPiServer.ServiceLocation;
using EPiServer.Shell;
using Wisbar.Optimizely.ContentTypes.Commerce.Variants;

namespace Wisbar.Business.Features.EventCoordinatorNotifications.Optimizely;

/// <summary>
/// Adds a Registrations button to the catalog view switcher for event variants. ViewType is loaded
/// as a literal URL by epi-cms/widget/IFrameController, which appends ?uri=...and id=[content link].
/// </summary>
// Registered against MeetingVariant only: EventVariant derives from it and the shell concatenates a
// base type's views onto the derived type's descriptor, so a second registration shows two buttons.
[ServiceConfiguration(typeof(ViewConfiguration))]
public sealed class EventRegistrationsViewConfiguration : ViewConfiguration<MeetingVariant>
{
    public EventRegistrationsViewConfiguration()
    {
        Key = "wisbareventregistrations";
        Name = "Registrations";
        Description = "Registration questions and answers logged for this event.";
        ControllerType = "epi-cms/widget/IFrameController";
        ViewType = "/ui/eventregistrations";
        IconClass = "epi-iconList";
        // The base constructor defaults this to 20000, which would sort it away from the catalog views.
        SortOrder = 600;
    }
}
```

## 3. Global menu item

`src/Wisbar.Business/Features/BackgroundJobs/Optimizely/HangfireMenuProvider.cs`

```csharp
using EPiServer.Authorization;
using EPiServer.Shell.Navigation;

namespace Wisbar.Business.Features.BackgroundJobs.Optimizely;

[MenuProvider]
public class HangfireMenuProvider : IMenuProvider
{
    public IEnumerable<MenuItem> GetMenuItems()
    {
        var menuItem = new UrlMenuItem("Hangfire", MenuPaths.Global + "/cms" + "/hangfire", "/ui/hangfirecms/index")
        {
            IsAvailable = request => EPiServer.Security.PrincipalInfo.CurrentPrincipal.IsInRole(Roles.CmsAdmins),
            AuthorizationPolicy = CmsPolicyNames.CmsAdmin,
            SortIndex = SortIndex.Normal
        };

        return [menuItem];
    }
}
```

## 4. Controller

`src/Wisbar.Web/Controllers/Cms/EventRegistrationsController.cs`

```csharp
// The role is checked in code rather than by [Authorize(Roles = ...)]: neither ViewConfiguration nor
// IFrameComponent can hide itself from an editor who lacks the role, so a plain challenge would show
// up as a login page inside the CMS chrome. Authentication is still required by [Authorize].
[Authorize]
[Route("ui/[controller]")]
public class EventRegistrationsController : Controller
{
    private readonly IEventRegistrationReport _report;
    private readonly IContentLoader _contentLoader;

    public EventRegistrationsController(IEventRegistrationReport report, IContentLoader contentLoader)
    {
        _report = report;
        _contentLoader = contentLoader;
    }

    // Mapped at the route root, not /index: ViewType is the literal path the shell iframe loads and
    // IFrameController only appends a query string to it.
    [HttpGet("")]
    public IActionResult Index(string? id, int page = 1)
    {
        if (!IsCommerceAdmin)
            return View(EventRegistrationsViewModel.Unavailable(forbidden: true));

        var variant = ResolveVariant(id);
        if (variant is null)
            return View(EventRegistrationsViewModel.Unavailable());

        ...
    }

    // The gadget renders on every content type, so anything that is not an event variant gets a
    // short neutral line rather than a warning.
    [HttpGet("summary")]
    public IActionResult Summary(string? id)
    {
        var variant = IsCommerceAdmin ? ResolveVariant(id) : null;
        if (variant is null)
            return View(new EventRegistrationsSummaryViewModel { IsEventVariant = false });

        ...
    }

    private bool IsCommerceAdmin => User.IsInRole(CommerceRoles.CommerceAdmins);

    // The shell sends the content link as "id", "id_workId" or "id_workId_provider"; TryParse
    // understands all three, so there is no ReferenceConverter round trip to do.
    private MeetingVariant? ResolveVariant(string? id)
    {
        if (string.IsNullOrEmpty(id)
            || !ContentReference.TryParse(id, out var link)
            || ContentReference.IsNullOrEmpty(link))
            return null;

        return _contentLoader.TryGet<MeetingVariant>(link, out var variant) ? variant : null;
    }
}
```

## 5. View model

`src/Wisbar.Web/Models/Cms/EventRegistrationsViewModel.cs`

```csharp
public class EventRegistrationsViewModel : ShellLayoutModel
{
    public override string Title => "Registrations";

    public string EventName { get; set; } = string.Empty;
    public string? ContentId { get; set; }

    /// <summary>Set when the shell did not pass a content link we could resolve to an event variant.</summary>
    public bool Unresolved { get; set; }

    /// <summary>Set when the signed-in user may not read registrations.</summary>
    public bool Forbidden { get; set; }

    public static EventRegistrationsViewModel Unavailable(bool forbidden = false) =>
        new() { Unresolved = !forbidden, Forbidden = forbidden };
}
```

## 6. Layout

`src/Wisbar.Web/Views/EventRegistrations/_Layout.cshtml` (full view) and `_SummaryLayout.cshtml` (gadget) share this shape; only the CSS differs.

```cshtml
@using EPiServer.Framework.Web.Resources

@*
    Hosted inside the CMS edit surface by epi-cms/widget/IFrameController, which already supplies the
    breadcrumb toolbar and sizes this iframe. Deliberately not _ShellLayout: that renders the global
    platform navigation, which would appear a second time inside the edit surface.
*@
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta content='width=device-width, initial-scale=1' name='viewport'>
    <title>Optimizely - @Model.Title</title>
    @ClientResources.RenderResources("ShellCore")
    @ClientResources.RenderResources("ShellCoreLightTheme")
    <style>
        /* Gadget layout: the assets pane is a narrow column capped by the component's MaxHeight,
           so no tables and nothing that scrolls in two directions. */
        body { margin: 0; font-family: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif; }
    </style>
</head>
<body>
    @RenderBody()
</body>
</html>
```

Links inside the iframe must carry the content link forward:

```cshtml
<a href="?id=@Model.ContentId&page=@p">@p</a>
```

## 7. Probe for a component definition

Drop this in `Wisbar.Tests`, run it, read the message, then delete it. It answers "did my attribute actually take effect" without clicking through the CMS. Keep the probe `Url` absolute so no configured `ServiceLocator` is needed.

```csharp
[IFrameComponent(Url = "https://example.org/probe", Title = "Probe", Categories = "commerce", SortOrder = 600)]
public sealed class ProbePlain
{
}

[TestClass]
public class TempComponentProbe
{
    [TestMethod]
    public void Probe()
    {
        var type = typeof(ProbePlain);
        var attr = (ComponentAttribute)type.GetCustomAttributes(typeof(ComponentAttribute), true)[0];
        var def = attr.CreateComponentDefinition(type);
        var comp = def.CreateComponent();

        Assert.Fail($"title='{def.Title}' sortOrder={comp.SortOrder} " +
                    $"categories='{string.Join("|", def.Categories ?? [])}' " +
                    $"settings='{string.Join(", ", comp.Settings.Select(s => $"{s.Key}={s.Value}"))}'");
    }
}
```

Run it directly, which supports filtering:

```
./src/Wisbar.Tests/bin/Debug/net8.0/Wisbar.Tests.exe --filter "FullyQualifiedName~TempComponentProbe"
```

A healthy plain-class gadget prints `urlTemplate=...`, the title, and the sizes. A class deriving from `ComponentDefinitionBase` prints an empty title, `sortOrder=100`, and no `urlTemplate`.
