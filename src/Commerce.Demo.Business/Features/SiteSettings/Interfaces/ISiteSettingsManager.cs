using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;
using EPiServer.Applications;
using System.Globalization;

namespace Commerce.Demo.Business.Features.SiteSettings.Interfaces;

public interface ISiteSettingsManager
{
    TOutputModel? CurrentSiteSettings<TSiteSettingsModel, TOutputModel>(CultureInfo? language)
        where TSiteSettingsModel : SiteSettingsPageBase
        where TOutputModel : class, new();

    TOutputModel? SpecificSiteSettings<TSiteSettingsModel, TOutputModel>(IRoutableApplication application, CultureInfo? language)
        where TSiteSettingsModel : SiteSettingsPageBase
        where TOutputModel : class, new();
}
