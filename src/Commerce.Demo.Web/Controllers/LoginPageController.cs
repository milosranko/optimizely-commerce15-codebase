using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages;
using Commerce.Demo.Web.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Demo.Web.Controllers;

public class LoginPageController : PageControllerBase<LoginPage>
{
    public IActionResult Index(LoginPage currentPage)
    {
        return View(currentPage);
    }
}
