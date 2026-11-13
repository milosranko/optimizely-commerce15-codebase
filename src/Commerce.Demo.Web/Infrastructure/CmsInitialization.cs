using Commerce.Demo.Business.Features.SiteSettings.Services;
using Commerce.Demo.Business.Infrastructure.Caching;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

namespace Commerce.Demo.Web.Infrastructure;

[InitializableModule]
public class CmsInitialization : IInitializableModule
{
    private bool _eventsAttached = false;

    public void Initialize(InitializationEngine context)
    {
        if (!_eventsAttached)
        {
            // Attach event handler to when a page has been created
            context.Services.GetInstance<IContentEvents>().PublishedContent += PublishedContent;

            _eventsAttached = true;
        }
    }

    public void Uninitialize(InitializationEngine context)
    {
        if (_eventsAttached)
        {
            // Attach event handler to when a page has been created
            context.Services.GetInstance<IContentEvents>().PublishedContent -= PublishedContent;

            _eventsAttached = false;
        }
    }

    private void PublishedContent(object? sender, ContentEventArgs e)
    {
        #region SiteSettingsPageBase events

        if (e.Content is SiteSettingsPageBase)
        {
            ServiceLocator.Current.GetRequiredService<ICacheService>().RemoveStartsWith(SiteSettingsManagerCacheDecorator.CacheKeyPrefix);
        }

        #endregion
    }
}
