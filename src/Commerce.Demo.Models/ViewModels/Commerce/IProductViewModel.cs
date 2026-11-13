using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;

namespace Commerce.Demo.Web.Models.Commerce;

public interface IProductViewModel<out T> where T : ILayoutPageBase
{
    T CurrentPage { get; }
}
