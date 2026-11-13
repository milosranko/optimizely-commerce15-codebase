using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Base;

public abstract class BlockBase : BlockData
{
    // CMS 13: IPageRouteHelper is obsolete; IContentRouteHelper.Content is null-or-non-page for e.g. catalog routes.
    private Lazy<PageData?> _currentPage = new(ServiceLocator.Current.GetInstance<IContentRouteHelper>().Content as PageData);

    public PageData? CurrentPage
    {
        get
        {
            try
            {
                return _currentPage.Value;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
