using Commerce.Demo.Models.ViewModels;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;
using Commerce.Demo.Optimizely.Core.Extensions;
using Commerce.Demo.Web.Models.Layouts;
using Mapster;

namespace Commerce.Demo.Web.Infrastructure;

public static class MapsterConfig
{
    public static void Initialize()
    {
        TypeAdapterConfig<SiteSettingsPageBase, SiteSettingsViewModel>.NewConfig()
            .MapWith(x => new SiteSettingsViewModel
            {
                SiteName = x.SiteName,
                CookiesConsent = x.CookiesConsent.Adapt<CookiesConsentBlockViewModel>(),
                Snippets = x.Snippets != null && !x.Snippets.IsEmpty && x.Snippets.GetFilteredItems().Any()
                    ? x.Snippets.GetFilteredItems().Select(y => y.GetBlock<SnippetBlock>().Adapt<SnippetBlockViewModel>())
                    : Enumerable.Empty<SnippetBlockViewModel>()
            });

        TypeAdapterConfig<SnippetBlock, SnippetBlockViewModel>.NewConfig()
            .Map(dest => dest.Name, src => string.IsNullOrEmpty(src.Name) ? ((IContent)src).Name : src.Name);

        TypeAdapterConfig<XhtmlString, XhtmlString>.NewConfig()
            .MapWith(x => x != null ? new XhtmlString(x.ToInternalString()) : new XhtmlString());

        TypeAdapterConfig<XhtmlString, string>.NewConfig()
            .MapWith(x => x.ToFriendlyUrlHtmlString() ?? string.Empty); //TODO MilosR fix it so it respects personalization and language versions
    }
}
