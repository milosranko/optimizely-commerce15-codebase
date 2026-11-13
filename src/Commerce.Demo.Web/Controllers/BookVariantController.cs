using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants;
using Commerce.Demo.Web.Controllers.Base;
using Commerce.Demo.Web.Models.Commerce;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Demo.Web.Controllers;

public class BookVariantController : CommerceControllerBase<BookVariant>
{
    public IActionResult Index(BookVariant currentPage)
    {
        var model = new VariantViewModel<BookVariant>(currentPage);

        return View(model);
    }
}
