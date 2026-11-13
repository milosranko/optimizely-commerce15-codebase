using CmsContentScaffolding.Optimizely.Interfaces;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Categories;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Products;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants;
using Commerce.Demo.Optimizely.ContentTypes.Shared.Categories;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Internal.Migration.Steps;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Geta.Optimizely.Categories.Extensions;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Shared;

namespace Commerce.Demo.Business.Migrations;

[ServiceConfiguration(typeof(IMigrationStep))]
internal class Migration1Siteinitialization : IMigrationStep
{
    public int Order => 1;
    public string Name => "Site initialization";
    public string Description => "Initialize site default settings";

    private readonly IContentBuilder _contentBuilder;
    private readonly IContentRepository _contentRepository;
    private readonly ReferenceConverter _referenceConverter;

    public Migration1Siteinitialization(
        IContentBuilder contentBuilder,
        IContentRepository contentRepository,
        ReferenceConverter referenceConverter)
    {
        _contentBuilder = contentBuilder;
        _contentRepository = contentRepository;
        _referenceConverter = referenceConverter;
    }

    public bool Execute(IProgressMessenger progressMessenger)
    {
        progressMessenger.AddProgressMessageText("Site initialization started", false, 10);
        _contentBuilder.Init();

        #region CMS Pages

        progressMessenger.AddProgressMessageText("Setting up site pages", false, 80);
        var startPage = _contentRepository.GetChildren<StartPage>(ContentReference.RootPage).Single();

        _contentBuilder.UsePages(startPage.ContentLink)
        .WithPage<LoginPage>(p =>
        {
            p.Name = "Login Page Test";
        });

        #endregion

        progressMessenger.AddProgressMessageText("Setting up site categories", false, 70);

        #region Categories initialization

        _contentBuilder.UseAssets(_contentRepository.GetOrCreateSiteCategoriesRoot())
            .WithContent<DefaultCategory>(x => x.Name = "Test Category", l1 =>
            l1.WithContent<DefaultCategory>(x => x.Name = "Child Category 1", l2 =>
                l2.WithContent<DefaultCategory>(x => x.Name = "Child Category 1.1"))
            .WithContent<DefaultCategory>(x => x.Name = "Child Category 2", l2 =>
                l2.WithContent<DefaultCategory>(x => x.Name = "Child Category 2.1")));

        #endregion

        #region COMMERCE Catalog

        progressMessenger.AddProgressMessageText("Setting up site pages", false, 90);

        var catalog = _contentRepository.GetChildren<CatalogContent>(_referenceConverter.GetRootLink()).First();

        _contentBuilder.UseAssets(catalog.ContentLink)
        .WithContent<GenericCategory>(x => x.Name = "CLE Seminars", l2 =>
        l2.WithContent<EventProduct>(x =>
        {
            x.Name = "The Nuts and Bolts of Bringing and Defending an Employment Discrimination Case";
            x.Code = "EventSA9425";
        }, l3 =>
            l3
            .WithContent<EventTuitionVariant>(x =>
            {
                x.Name = "Attending Tuition";
                x.Code = "SA9425/TUITION";
            })
            .WithContent<EventVariant>(x =>
            {
                x.Name = "Friday, September 12, 2025";
                x.Code = "SA9425";
            }))
        .WithContent<EventProduct>(x =>
        {
            x.Name = "Advanced Skills and Techniques for Family Law Practice 2025";
            x.Code = "EventCA3809";
        }, l3 =>
            l3
            .WithContent<EventVariant>(x =>
            {
                x.Name = "Monday, October 12, 2025";
                x.Code = "CA3809M";
            })
            .WithContent<EventTuitionVariant>(x =>
            {
                x.Name = "Tuition 1";
                x.Code = "CA3809M/TUITION1";
            })
            .WithContent<EventTuitionVariant>(x =>
            {
                x.Name = "Tuition 2";
                x.Code = "CA3809M/TUITION2";
            })
            .WithContent<EventAddonVariant>(x =>
            {
                x.Name = "Addon 1";
                x.Code = "CA3809M/Addon1";
            })
            .WithContent<EventAddonVariant>(x =>
            {
                x.Name = "Addon 2";
                x.Code = "CA3809M/Addon2";
            })));

        #endregion

        progressMessenger.AddProgressMessageText("Finished site initialization", false, 100);

        return true;
    }
}
