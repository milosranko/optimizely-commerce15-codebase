using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace Commerce.Demo.Optimizely.Core.Extensions;

public static class ContentAreaItemExtensions
{
    public static TModel? GetBlock<TModel>(this ContentAreaItem contentAreaItem) where TModel : BlockData
    {
        if (contentAreaItem is null)
            return default;

        if (ServiceLocator.Current.GetInstance<IContentLoader>().TryGet<TModel>(contentAreaItem.ContentLink, LanguageSelector.AutoDetect(true), out var block))
            return block;

        return default;
    }
}
