using Commerce.Demo.Optimizely.ContentTypes.Commerce.Products.Base;
using Commerce.Demo.Web.Components.Base;
using EPiServer.Framework.DataAnnotations;
using EPiServer.Framework.Web;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Demo.Web.Components;

[TemplateDescriptor(Inherited = true, TemplateTypeCategory = TemplateTypeCategories.MvcPartialComponent)]
public class ProductPartialComponent : PartialContentComponentBase<ProductBase>
{
    protected override IViewComponentResult InvokeComponent(ProductBase currentContent)
    {
        return View("~/Views/Shared/_Product.cshtml", currentContent);
    }
}
