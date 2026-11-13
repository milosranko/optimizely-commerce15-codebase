using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks;
using Commerce.Demo.Web.Components.Base;
using Commerce.Demo.Web.Models.Blocks;
using EPiServer.Framework.DataAnnotations;
using EPiServer.Framework.Web;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Demo.Web.Components;

[TemplateDescriptor(Inherited = true, TemplateTypeCategory = TemplateTypeCategories.MvcPartialComponent)]
public class CarouselItemBlockComponent : PartialContentComponentBase<CarouselItemBlock>
{
    protected override IViewComponentResult InvokeComponent(CarouselItemBlock currentContent)
    {
        var viewModel = new BlockViewModel<CarouselItemBlock>(currentContent);

        return View(viewModel);
    }
}
