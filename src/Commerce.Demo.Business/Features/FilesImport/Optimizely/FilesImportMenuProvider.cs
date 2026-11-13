using EPiServer.Authorization;
using EPiServer.Shell.Navigation;

namespace Commerce.Demo.Business.Features.FilesImport.Optimizely;

[MenuProvider]
public class FilesImportMenuProvider : IMenuProvider
{
    public IEnumerable<MenuItem> GetMenuItems()
    {
        var menuItem = new UrlMenuItem(
            "Files Import",
            MenuPaths.Global + "/cms" + "/filesimport",
            "/cms/admin/filesimport")
        {
            IsAvailable = request => EPiServer.Security.PrincipalInfo.CurrentPrincipal.IsInRole(Roles.CmsAdmins),
            AuthorizationPolicy = CmsPolicyNames.CmsAdmin,
            SortIndex = SortIndex.First + 31
        };

        return [menuItem];
    }
}
