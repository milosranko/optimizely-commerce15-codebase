using Commerce.Demo.Business.Features.SiteSettings.Interfaces;
using Commerce.Demo.Business.Infrastructure.Caching;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;
using EPiServer.Applications;
using System.Globalization;

namespace Commerce.Demo.Business.Features.SiteSettings.Services;

public class SiteSettingsManagerCacheDecorator : CacheDecoratorBase, ISiteSettingsManager
{
    public const string CacheKeyPrefix = "SiteSettings:";
    private readonly ISiteSettingsManager _siteSettingsManager;
    private readonly IApplicationResolver _applicationResolver;

    public SiteSettingsManagerCacheDecorator(
        ISiteSettingsManager siteSettingsManager,
        ICacheService cacheService,
        IApplicationResolver applicationResolver) : base(cacheService)
    {
        _siteSettingsManager = siteSettingsManager;
        _applicationResolver = applicationResolver;
    }

    public TOutputModel? CurrentSiteSettings<TSiteSettingsModel, TOutputModel>(CultureInfo? language)
        where TSiteSettingsModel : SiteSettingsPageBase
        where TOutputModel : class, new()
    {
        return CacheService.GetOrSet(CacheKey, () => _siteSettingsManager.CurrentSiteSettings<TSiteSettingsModel, TOutputModel>(language));
    }

    public TOutputModel? SpecificSiteSettings<TSiteSettingsModel, TOutputModel>(IRoutableApplication application, CultureInfo? language)
        where TSiteSettingsModel : SiteSettingsPageBase
        where TOutputModel : class, new()
    {
        ArgumentNullException.ThrowIfNull(application);

        // Keyed by the requested application (was the current site's key, which could return another site's settings).
        return CacheService.GetOrSet(
            BuildCacheKey((application as Application)?.Name ?? $"entry-{application.EntryPoint}"),
            () => _siteSettingsManager.SpecificSiteSettings<TSiteSettingsModel, TOutputModel>(application, language));
    }

    // CMS 13: applications are identified by Name (SiteDefinition.Current.Id is gone).
    protected override string CacheKey => BuildCacheKey(_applicationResolver.GetByContext()?.Name ?? "default");

    private static string BuildCacheKey(string applicationName) =>
        $"{CacheKeyPrefix}{applicationName}:{Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName}";
}
