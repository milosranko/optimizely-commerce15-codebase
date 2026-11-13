using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Base;
using Commerce.Demo.Web.Models.Blocks;
using EPiServer.ServiceLocation;
using EPiServer.Web.Mvc;
using EPiServer.Web.Routing;

namespace Commerce.Demo.Web.Components.Base;

public abstract class AsyncBlockComponentBase<TBlock, TModel> : AsyncBlockComponent<TBlock>
    where TBlock : BlockBase
    where TModel : BlockViewModel<TBlock>, new()
{
    // CMS 13: IPageRouteHelper is obsolete; IContentRouteHelper.Content is null-or-non-page for e.g. catalog routes.
    public Lazy<PageData?> CurrentPage => new(ServiceLocator.Current.GetInstance<IContentRouteHelper>().Content as PageData);
}
