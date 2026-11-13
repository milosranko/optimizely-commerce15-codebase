using Commerce.Demo.Optimizely.ContentTypes.Commerce.Products;
using Commerce.Demo.Web.Controllers.Base;
using Commerce.Demo.Web.Models.Commerce;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Demo.Web.Controllers;

public class FashionProductController : CommerceControllerBase<FashionProduct>
{
    public IActionResult Index(FashionProduct currentPage)
    {
        var model = new ProductViewModel<FashionProduct>(currentPage);

        return View(model);
    }
}
