using EPiServer.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Demo.Web.Controllers;

[Authorize(Roles = $"{Roles.Administrators},{Roles.CmsAdmins},{Roles.WebAdmins}")]
[Route("[controller]")]
public class HangfireCmsController : Controller
{
    [Route("[action]")]
    public ActionResult Index()
    {
        return View();
    }
}
