using Commerce.Demo.Optimizely.ContentTypes.Commerce.Categories;
using Commerce.Demo.Web.Controllers.Base;
using Commerce.Demo.Web.Models.Commerce;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Demo.Web.Controllers;

public class GenericCategoryController : CommerceControllerBase<GenericCategory>
{
    public IActionResult Index(GenericCategory currentPage)
    {
        var model = new CategoryViewModel<GenericCategory>(currentPage);

        return View(model);
    }
}
