using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Base;
using Commerce.Demo.Web.Models.Pages;

namespace Commerce.Demo.Web.Models.Commerce;

public record VariantViewModel<T> : PageViewModel<T> where T : VariantBase, ILayoutPageBase
{
    public VariantViewModel(T currentPage) : base(currentPage)
    {
        Quantity = 1;
        Code = currentPage.Code;
        //ListPrice = currentPage.GetListPrice();
    }

    public IEnumerable<VariantsViewModel> VariantsModel { get; set; }
    public IEnumerable<VariantsViewModel> RelatedProducts { get; set; }
    public string Code { get; set; }
    public int Quantity { get; set; }
    public string BannerText { get; set; }

    //public List<VariantDropdownInfo> AddToCartDropdown { get; set; }
}
