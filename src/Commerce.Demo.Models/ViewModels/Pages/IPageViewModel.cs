using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;
using Commerce.Demo.Web.Models.Layouts;

namespace Commerce.Demo.Web.Models.Pages;

public interface IPageViewModel<out T> where T : ILayoutPageBase
{
    T CurrentPage { get; }
    LayoutModel Layout { get; set; }
}
