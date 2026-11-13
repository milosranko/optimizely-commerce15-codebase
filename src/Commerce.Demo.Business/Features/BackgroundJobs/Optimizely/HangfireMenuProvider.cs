using EPiServer.Authorization;
using EPiServer.Shell.Navigation;

namespace Commerce.Demo.Business.Features.BackgroundJobs.Optimizely;

[MenuProvider]
public class HangfireMenuProvider : IMenuProvider
{
    public IEnumerable<MenuItem> GetMenuItems()
    {
        var hangFireMenuItem = new UrlMenuItem("Hangfire", MenuPaths.Global + "/cms" + "/cmsMenuItem", "/HangfireCms/index")
        {
            IsAvailable = request => EPiServer.Security.PrincipalInfo.CurrentPrincipal.IsInRole(Roles.CmsAdmins),
            AuthorizationPolicy = CmsPolicyNames.CmsAdmin,
            SortIndex = SortIndex.First + 25
        };

        return
        [
            hangFireMenuItem
        ];
    }
}
