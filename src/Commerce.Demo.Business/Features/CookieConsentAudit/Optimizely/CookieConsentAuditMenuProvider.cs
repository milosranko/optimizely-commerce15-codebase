using EPiServer.Authorization;
using EPiServer.Shell.Navigation;

namespace Commerce.Demo.Business.Features.CookieConsentAudit.Optimizely;

[MenuProvider]
public class CookieConsentAuditMenuProvider : IMenuProvider
{
    public IEnumerable<MenuItem> GetMenuItems()
    {
        var menuItem = new UrlMenuItem(
            "Cookies Consent Audit",
            MenuPaths.Global + "/cms" + "/cookieconsentaudit",
            "/cms/admin/cookieconsentaudit")
        {
            IsAvailable = request => EPiServer.Security.PrincipalInfo.CurrentPrincipal.IsInRole(Roles.CmsAdmins),
            AuthorizationPolicy = CmsPolicyNames.CmsAdmin,
            SortIndex = SortIndex.First + 30
        };

        return [menuItem];
    }
}
