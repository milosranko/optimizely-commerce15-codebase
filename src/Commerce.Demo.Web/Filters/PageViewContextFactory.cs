using Commerce.Demo.Business.Features.SiteSettings.Interfaces;
using Commerce.Demo.Models.ViewModels;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;
using Commerce.Demo.Web.Models;
using Commerce.Demo.Optimizely.Core.Extensions;
using Commerce.Demo.Web.Models.Layouts;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Mapster;

namespace Commerce.Demo.Web.Filters;

[ServiceConfiguration]
public class PageViewContextFactory
{
    private readonly ISiteSettingsManager _siteSettingsManager;

    public PageViewContextFactory(ISiteSettingsManager siteSettingsManager)
    {
        _siteSettingsManager = siteSettingsManager;
    }

    public LayoutModel CreateLayout(PageBase page)
    {
        var siteSettings = _siteSettingsManager.CurrentSiteSettings<SiteSettingsPage, SiteSettingsViewModel>(ContentLanguage.PreferredCulture);
        var siteName = siteSettings?.SiteName;
        var snippets = CreateSnippets((PageBasePublic)page, siteSettings?.Snippets);

        return new LayoutModel
        {
            MetaData = CreateMetaData(page),
            OpenGraph = CreateOpenGraph(page, "siteName"),
            Header = CreateHeader(),
            Footer = CreateFooter(),
            PageTitle = page is StartPageBase ? "siteName" : page.PageName + " | " + "siteName",
            SiteName = "siteName",
            Snippets = snippets,
            CookiesConsent = CreateCookiesConsent(siteSettings?.CookiesConsent, snippets)
        };
    }

    private static CookiesConsentDialogViewModel? CreateCookiesConsent(CookiesConsentBlockViewModel? cookiesConsent, IEnumerable<SnippetBlockViewModel> snippets)
    {
        if (cookiesConsent is null || string.IsNullOrEmpty(cookiesConsent.CookieName))
            return default;

        var cookiesConsentViewModel = cookiesConsent.Adapt<CookiesConsentDialogViewModel>();
        cookiesConsentViewModel.Cookies ??= [];

        if (snippets is not null && snippets.Any(x => x.RequiresVisitorConsent || (x.Cookies != null && x.Cookies.Any())))
        {
            foreach (var snippet in snippets)
            {
                if (snippet.RequiresVisitorConsent)
                    cookiesConsentViewModel.Cookies.Add(snippet.Adapt<CookieItemBlockViewModel>());

                if (snippet.Cookies is not null && snippet.Cookies.Any())
                {
                    foreach (var cookie in snippet.Cookies)
                    {
                        if (cookiesConsentViewModel.Cookies.Where(c => c.Name == cookie.Name).Any())
                            continue;

                        cookiesConsentViewModel.Cookies.Add(cookie);
                    }
                }
            }
        }

        return cookiesConsentViewModel;
    }

    private HeaderModelBase CreateHeader()
    {
        return new HeaderModelBase
        {
            //LogoLink = 
        };
    }

    private MetaDataModel CreateMetaData(PageBase page)
    {
        if (page is not PageBaseSeo sitePage)
            return new MetaDataModel();

        return new MetaDataModel
        {
            Description = sitePage.SeoBlock?.MetaDescription,
            NoRobots = sitePage.SeoBlock?.MetaNoRobots ?? false,
        };
    }

    private OpenGraphModel CreateOpenGraph(PageBase page, string siteName)
    {
        if (page is not PageBaseSeo seoPage)
            return new OpenGraphModel();

        var imageUrl = default(string?);

        if (!ContentReference.IsNullOrEmpty(seoPage.OpenGraphBlock?.OpenGraphImage))
        {
            string url = UrlResolver.Current.GetUrl(seoPage.OpenGraphBlock?.OpenGraphImage);
            imageUrl = ServiceLocator.Current.GetInstance<IUriSupport>().AbsoluteUrlBySettings(url) + "?w=1200";
        }

        return new OpenGraphModel
        {
            ImageUrl = imageUrl,
            PageUrl = UrlResolver.Current.GetUrl(seoPage.ContentLink, null, new VirtualPathArguments { ForceAbsolute = true }),
            Title = seoPage is StartPage ? siteName : seoPage.Name,
            Description = seoPage.OpenGraphBlock?.OpenGraphDescription
        };
    }

    private FooterModelBase CreateFooter()
    {
        return new FooterModelBase
        {
            //Column1 = startPage.Column1,
            //Column2 = startPage.Column2,
            //Column3 = startPage.Column3
        };
    }

    private static IEnumerable<SnippetBlockViewModel> CreateSnippets(ISnippetsWithInheritance pageSnippets, IEnumerable<SnippetBlockViewModel>? globalSnippets)
    {
        if ((pageSnippets.Snippets is null || !pageSnippets.Snippets.GetFilteredItems().Any()) &&
            (globalSnippets is null || !globalSnippets.Any()))
            return [];

        var snippets = new List<SnippetBlockViewModel>();

        if (pageSnippets.Snippets is not null && pageSnippets.Snippets.GetFilteredItems().Any())
            snippets.AddRange(pageSnippets.Snippets.GetFilteredItems()
                .Where(x => x.LoadContent() != null)
                .Select(x => (x.LoadContent() as SnippetBlock).Adapt<SnippetBlockViewModel>()));

        if (pageSnippets.InheritSnippetsFromSettings && globalSnippets != null && globalSnippets.Any())
            return snippets.Concat(globalSnippets);

        return snippets;
    }
}
