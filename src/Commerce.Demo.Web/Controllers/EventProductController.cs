using Commerce.Demo.Optimizely.ContentTypes.Commerce.Products;
using Commerce.Demo.Web.Controllers.Base;
using Commerce.Demo.Web.Models.Commerce;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Demo.Web.Controllers;

public class EventProductController : CommerceControllerBase<EventProduct>
{
    public IActionResult Index(EventProduct currentPage)
    {
        var model = new ProductViewModel<EventProduct>(currentPage);

        return View(model);
    }
}
