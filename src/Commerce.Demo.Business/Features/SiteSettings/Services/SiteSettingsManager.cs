using Commerce.Demo.Business.Features.SiteSettings.Interfaces;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;
using EPiServer;
using EPiServer.Applications;
using EPiServer.Core;
using EPiServer.Globalization;
using Mapster;
using System.Globalization;

namespace Commerce.Demo.Business.Features.SiteSettings.Services;

public class SiteSettingsManager : ISiteSettingsManager
{
    private readonly IContentLoader _contentLoader;

    public SiteSettingsManager(IContentLoader contentLoader)
    {
        _contentLoader = contentLoader;
    }

    public TOutputModel? CurrentSiteSettings<TSiteSettingsModel, TOutputModel>(CultureInfo? language)
        where TSiteSettingsModel : SiteSettingsPageBase
        where TOutputModel : class, new()
    {
        return GetSiteSettings<TSiteSettingsModel, TOutputModel>(ContentReference.StartPage, language);
    }

    public TOutputModel? SpecificSiteSettings<TSiteSettingsModel, TOutputModel>(IRoutableApplication application, CultureInfo? language)
        where TSiteSettingsModel : SiteSettingsPageBase
        where TOutputModel : class, new()
    {
        ArgumentNullException.ThrowIfNull(application);

        // CMS 13: an application's EntryPoint is what SiteDefinition.StartPage was.
        return GetSiteSettings<TSiteSettingsModel, TOutputModel>(application.EntryPoint, language);
    }

    private TOutputModel? GetSiteSettings<TInputModel, TOutputModel>(ContentReference siteStartPageRef, CultureInfo? language)
        where TInputModel : SiteSettingsPageBase
        where TOutputModel : class, new()
    {
        if (_contentLoader.TryGet<StartPageBase>(
            siteStartPageRef,
            language ?? ContentLanguage.PreferredCulture,
            out var startPage))
        {
            var siteSettings = _contentLoader.GetChildren<TInputModel>(startPage.ContentLink, LanguageSelector.AutoDetect()).SingleOrDefault();

            if (siteSettings is not null)
                return siteSettings.Adapt<TOutputModel>();
        }

        return default;
    }
}
