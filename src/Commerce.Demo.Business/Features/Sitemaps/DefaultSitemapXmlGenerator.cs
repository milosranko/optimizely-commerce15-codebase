using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;
using EPiServer;
using EPiServer.Applications;
using EPiServer.DataAbstraction;
using EPiServer.Web.Routing;
using Geta.Optimizely.Sitemaps;
using Geta.Optimizely.Sitemaps.Commerce;
using Geta.Optimizely.Sitemaps.Repositories;
using Geta.Optimizely.Sitemaps.Services;
using Geta.Optimizely.Sitemaps.Utils;
using Mediachase.Commerce.Catalog;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;

namespace Commerce.Demo.Business.Features.Sitemaps;

public class DefaultSitemapXmlGenerator : CommerceAndStandardSitemapXmlGenerator
{
    public DefaultSitemapXmlGenerator(
        ISitemapRepository sitemapRepository,
        IContentRepository contentRepository,
        IUrlResolver urlResolver,
        IApplicationRepository applicationRepository,
        ILanguageBranchRepository languageBranchRepository,
        ReferenceConverter referenceConverter,
        IContentFilter contentFilter,
        IUriAugmenterService uriAugmenterService,
        ILogger<DefaultSitemapXmlGenerator> logger)
        // Geta.Optimizely.Sitemaps 5.x: the base uses the CMS 13 Applications model (IApplicationRepository)
        // and no longer takes ISynchronizedObjectInstanceCache / IMemoryCache.
        : base(sitemapRepository, contentRepository, urlResolver, applicationRepository, languageBranchRepository, referenceConverter, contentFilter, uriAugmenterService, logger)
    { }

    protected override void AddFilteredContentElement(CurrentLanguageContent languageContentInfo, IList<XElement> xmlElements)
    {
        if (languageContentInfo.Content is IExcludeFromSitemap sitemapContent && sitemapContent.ExcludeFromSitemap)
        {
            return;
        }

        base.AddFilteredContentElement(languageContentInfo, xmlElements);
    }
}
