using EPiServer.Authorization;
using Hangfire.Dashboard;

namespace Commerce.Demo.Web.Filters;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly IWebHostEnvironment _environment;

    public HangfireDashboardAuthorizationFilter(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public bool Authorize(DashboardContext context)
    {
        if (_environment.IsDevelopment())
            return true;

        var httpContext = context.GetHttpContext();
        var user = httpContext.User;

        return user.Identity?.IsAuthenticated == true && (user.IsInRole(Roles.WebAdmins) || user.IsInRole(Roles.CmsAdmins));
    }
}
