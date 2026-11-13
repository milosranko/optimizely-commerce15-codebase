using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;
using Commerce.Demo.Web.Models.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Commerce.Demo.Web.Filters;

public class PageContextActionFilter : IResultFilter
{
    private readonly PageViewContextFactory _contextFactory;

    public PageContextActionFilter(PageViewContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Controller is Controller controller &&
            controller.ViewData.Model is IPageViewModel<PageBase> model)
        {
            model.Layout = _contextFactory.CreateLayout(model.CurrentPage);
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    { }
}
