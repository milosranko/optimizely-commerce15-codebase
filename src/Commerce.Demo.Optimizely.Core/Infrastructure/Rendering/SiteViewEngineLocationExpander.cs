using Microsoft.AspNetCore.Mvc.Razor;
using static Commerce.Demo.Optimizely.Core.Constants.Globals;

namespace Commerce.Demo.Optimizely.Core.Infrastructure.Rendering;

public class SiteViewEngineLocationExpander : IViewLocationExpander
{
    private static readonly string[] AdditionalPartialViewFormats =
    [
        ViewLocations.BlocksFolder + "{0}.cshtml",
        ViewLocations.PagePartialsFolder + "{0}.cshtml"
    ];

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        var locations = AdditionalPartialViewFormats.Union(viewLocations);

        if (context.ViewName is not null &&
            context.ViewName.StartsWith("Components/", StringComparison.OrdinalIgnoreCase))
        {
            var segments = context.ViewName.Split('/');

            if (segments.Length >= 2)
            {
                var componentName = segments[1];
                locations = new[] { ViewLocations.BlocksFolder + componentName + ".cshtml" }
                    .Union(locations);
            }
        }

        return locations;
    }

    public void PopulateValues(ViewLocationExpanderContext context) { }
}
