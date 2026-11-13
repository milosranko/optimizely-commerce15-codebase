using Commerce.Demo.Optimizely.ContentTypes.Commerce.Products;
using Commerce.Demo.Web.Controllers.Base;
using Commerce.Demo.Web.Models.Commerce;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Demo.Web.Controllers;

public class BookProductController : CommerceControllerBase<BookProduct>
{
    public IActionResult Index(BookProduct currentPage)
    {
        var model = new ProductViewModel<BookProduct>(currentPage);

        return View(model);
    }
}
