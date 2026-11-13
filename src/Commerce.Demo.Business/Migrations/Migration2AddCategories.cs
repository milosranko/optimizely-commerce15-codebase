using CmsContentScaffolding.Optimizely.Interfaces;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Categories;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Products;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants;
using Commerce.Demo.Optimizely.ContentTypes.Shared.Categories;
using Commerce.Demo.Optimizely.Core.Helpers;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Commerce.Internal.Migration.Steps;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Geta.Optimizely.Categories.Extensions;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Shared;
using Mediachase.MetaDataPlus;
using Mediachase.MetaDataPlus.Configurator;

namespace Commerce.Demo.Business.Migrations;

[ServiceConfiguration(typeof(IMigrationStep))]
internal class Migration2AddCategories : IMigrationStep
{
    public int Order => 2;
    public string Name => "Site 2 initialization";
    public string Description => "Initialize site default settings";

    private readonly IContentBuilder _contentBuilder;
    private readonly IContentRepository _contentRepository;
    private readonly ReferenceConverter _referenceConverter;
    private readonly GroupDefinitionRepository<AssociationGroupDefinition> _groupDefinitionRepository;

    public Migration2AddCategories(
        IContentBuilder contentBuilder,
        IContentRepository contentRepository,
        ReferenceConverter referenceConverter,
        GroupDefinitionRepository<AssociationGroupDefinition> groupDefinitionRepository)
    {
        _contentBuilder = contentBuilder;
        _contentRepository = contentRepository;
        _referenceConverter = referenceConverter;
        _groupDefinitionRepository = groupDefinitionRepository;
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
            p.Name = "Another Page Test";
        });

        #endregion

        progressMessenger.AddProgressMessageText("Setting up site categories", false, 70);

        #region Categories initialization

        _contentBuilder.UseAssets(_contentRepository.GetOrCreateSiteCategoriesRoot())
            .WithContent<DefaultCategory>(x => x.Name = "Test Category 1", l1 =>
            l1.WithContent<DefaultCategory>(x => x.Name = "Child Category 11", l2 =>
                l2.WithContent<DefaultCategory>(x => x.Name = "Child Category 1.12"))
            .WithContent<DefaultCategory>(x => x.Name = "Child Category 2", l2 =>
                l2.WithContent<DefaultCategory>(x => x.Name = "Child Category 2.13")));

        #endregion

        #region COMMERCE Catalog

        var catalog = _contentRepository.GetChildren<CatalogContent>(_referenceConverter.GetRootLink()).First();

        _contentBuilder.UseAssets(catalog.ContentLink)
        .WithContent<GenericCategory>(x => x.Name = "Books", l2 =>
            l2.WithContent<BookProduct>(x =>
            {
                x.Name = "Reduce Your Risk: A Guide to Personal Safety and Security for the Legal Community";
                x.Code = "BookAK0441";
                x.Isbn = "978-1-946391-44-1";
            }, l3 =>
                l3
                .WithContent<BookVariant>(x =>
                {
                    x.Name = "AK0441 Reduce Your Risk: A Guide to Personal Safety and Security";
                    x.Code = "AK0441";
                }))
            .WithContent<BookProduct>(x =>
            {
                x.Name = "Wisconsin Trust Code and Powers of Appointment Act ";
                x.Code = "BookAK0418";
                x.Isbn = "978-1-946391-44-2";
            }, l3 =>
                l3
                .WithContent<BookVariant>(x =>
                {
                    x.Name = "AK0418E25 WI Trust Code 2025 (FY25)";
                    x.Code = "AK0418E25";
                })));

        #endregion

        #region Meta classes

        //Creates new Dictionary with predefined values, used as a property on content type as multiselect or single select
        var claimMetaField = Mediachase.MetaDataPlus.Configurator.MetaField.Load(MetaDataContext.Instance, "PurchaseClaims");
        if (claimMetaField is null)
        {
            claimMetaField = Mediachase.MetaDataPlus.Configurator.MetaField.Create(MetaDataContext.Instance, "", "PurchaseClaims", "Purchase Claims", "Member purchase claims", MetaDataType.DictionaryMultiValue, 200, false, false, false, false);
            claimMetaField.Dictionary.Add(new MetaDictionaryItem("FASTCASE"));
            claimMetaField.Dictionary.Add(new MetaDictionaryItem("FB1000_PRICE"));
            claimMetaField.Dictionary.Add(new MetaDictionaryItem("FB2000"));
        }

        var creditTypeMetaField = Mediachase.MetaDataPlus.Configurator.MetaField.Load(MetaDataContext.Instance, "CreditType");
        if (creditTypeMetaField is null)
        {
            creditTypeMetaField = Mediachase.MetaDataPlus.Configurator.MetaField.Create(MetaDataContext.Instance, "", "CreditType", "Credit Types", "", MetaDataType.DictionarySingleValue, 50, false, false, false, false);
            creditTypeMetaField.Dictionary.Add(new MetaDictionaryItem("GALDV"));
            creditTypeMetaField.Dictionary.Add(new MetaDictionaryItem("CCRB"));
            creditTypeMetaField.Dictionary.Add(new MetaDictionaryItem("CLE"));
            creditTypeMetaField.Dictionary.Add(new MetaDictionaryItem("EPR"));
            creditTypeMetaField.Dictionary.Add(new MetaDictionaryItem("LPM"));
            creditTypeMetaField.Dictionary.Add(new MetaDictionaryItem("GALAD"));
            creditTypeMetaField.Dictionary.Add(new MetaDictionaryItem("GALMIN"));
            creditTypeMetaField.Dictionary.Add(new MetaDictionaryItem("GALF"));
            creditTypeMetaField.Dictionary.Add(new MetaDictionaryItem("LAU"));
        }

        var donationMetaField = Mediachase.MetaDataPlus.Configurator.MetaField.Load(MetaDataContext.Instance, "Donation");
        if (donationMetaField is null)
        {
            donationMetaField = Mediachase.MetaDataPlus.Configurator.MetaField.Create(MetaDataContext.Instance, "", "Donation", "Donation Range", "", MetaDataType.DictionarySingleValue, 2, false, false, false, false);
            donationMetaField.Dictionary.Add(new MetaDictionaryItem("5"));
            donationMetaField.Dictionary.Add(new MetaDictionaryItem("1000"));
        }

        //Creates additional Customer Groups, thay are used on Contacts as a single selection
        var metaFieldType = DataContext.Current.MetaModel.RegisteredTypes["ContactGroup"];
        var metaEnumItems = MetaEnum.GetItems(metaFieldType);
        var hasMemberGroup = metaEnumItems.Any(item => string.Equals(item.Name, "Member", StringComparison.InvariantCultureIgnoreCase));
        if (!hasMemberGroup)
        {
            var lastIndex = metaEnumItems.Count();
            MetaEnum.AddItem(metaFieldType, "Member", ++lastIndex);
            MetaEnum.AddItem(metaFieldType, "Non-Member", ++lastIndex);
        }

        //Creates additional meta fields for Contacts
        var contactMetaClass = CommerceHelpers.GetOrCreateMetaClass(ContactEntity.ClassName);
        var metaFields = new List<Mediachase.BusinessFoundation.Data.Meta.Management.MetaField>
        {
            new("MemberId", "Member ID") { IsNullable = false, TypeName = MetaFieldType.Text, ReadOnly = true },
            new("UpassEndDate", "UPASS End Date") { IsNullable = true, TypeName = MetaFieldType.DateTime, ReadOnly = true },
            new("PurchaseClaims", "Purchase Claims") { IsNullable = true, TypeName = MetaFieldType.Text, ReadOnly = true },
            new("Phone", "Phone") { IsNullable = true, TypeName = MetaFieldType.Text, ReadOnly = true }
        };
        CommerceHelpers.CreateMetaFields(contactMetaClass, metaFields);

        //Creates additional related entries types
        if (!_groupDefinitionRepository.List().Any(x => x.Name.Equals("Tuition", StringComparison.InvariantCultureIgnoreCase)))
        {
            _groupDefinitionRepository.Add(new AssociationGroupDefinition { Name = "Recommended" }); //Add constants for Recommended, Tuition, Addon
            _groupDefinitionRepository.Add(new AssociationGroupDefinition { Name = "Tuition" });
            _groupDefinitionRepository.Add(new AssociationGroupDefinition { Name = "Addon" });
        }

        #endregion

        progressMessenger.AddProgressMessageText("Finished site initialization", false, 100);

        return true;
    }
}
