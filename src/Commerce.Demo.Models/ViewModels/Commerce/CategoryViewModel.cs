using Commerce.Demo.Optimizely.ContentTypes.Commerce.Categories.Base;
using Commerce.Demo.Web.Models.Pages;

namespace Commerce.Demo.Web.Models.Commerce;

public record CategoryViewModel<T> : PageViewModel<T> where T : CategoryBase
{
    public CategoryViewModel(T currentPage) : base(currentPage)
    { }
}
