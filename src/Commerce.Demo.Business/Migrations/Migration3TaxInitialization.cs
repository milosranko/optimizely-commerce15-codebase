using Commerce.Demo.Business.Migrations.Data;
using EPiServer.Commerce.Internal.Migration.Steps;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog.Managers;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;
using Mediachase.Commerce.Shared;
using Microsoft.Extensions.Logging;

namespace Commerce.Demo.Business.Migrations;

[ServiceConfiguration(typeof(IMigrationStep))]
internal class Migration3TaxInitialization : IMigrationStep
{
    public int Order => 3;
    public string Name => "Wisconsin tax configuration";
    public string Description => "Seeds Wisconsin jurisdictions, jurisdiction groups and sales tax rates";

    private const string StateTaxName = "Wisconsin State Sales Tax";
    private const string CountyTaxName = "Wisconsin County Sales Tax";
    private const string StadiumTaxName = "Wisconsin Stadium Sales Tax";
    private const string TaxLanguageCode = "en";
    private const string CountryCode = "USA";
    private const string StateCode = "WI";
    private const string DefaultTaxCategoryName = "General Sales";
    private const JurisdictionManager.JurisdictionType TypeTax = JurisdictionManager.JurisdictionType.Tax;
    private static readonly DateTime DefaultEffectiveDate = new(2000, 1, 1);

    private readonly ILogger<Migration3TaxInitialization> _logger;

    public Migration3TaxInitialization(ILogger<Migration3TaxInitialization> logger)
    {
        _logger = logger;
    }

    public bool Execute(IProgressMessenger progressMessenger)
    {
        progressMessenger.AddProgressMessageText("Wisconsin tax initialization started", false, 5);

        try
        {
            EnsureTaxCategory(DefaultTaxCategoryName);

            var groupIdByCountyCode = EnsureJurisdictions(progressMessenger);

            progressMessenger.AddProgressMessageText("Populating Wisconsin tax rates", false, 80);

            var taxDto = TaxManager.GetTaxDto(TaxType.SalesTax);
            var stateTaxRow = EnsureTaxRow(taxDto, StateTaxName, sortOrder: 10);
            var countyTaxRow = EnsureTaxRow(taxDto, CountyTaxName, sortOrder: 20);
            var stadiumTaxRow = EnsureTaxRow(taxDto, StadiumTaxName, sortOrder: 30);

            foreach (var county in WisconsinTaxRates.All)
            {
                if (!groupIdByCountyCode.TryGetValue(county.Code, out var groupId)) continue;

                try
                {
                    UpsertTaxValue(taxDto, stateTaxRow, groupId, county.StateRate);
                    UpsertTaxValue(taxDto, countyTaxRow, groupId, county.CountyRate);
                    UpsertTaxValue(taxDto, stadiumTaxRow, groupId, county.StadiumRate);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to seed tax values for {CountyCode}", county.Code);
                }
            }

            TaxManager.SaveTax(taxDto);

            progressMessenger.AddProgressMessageText("Wisconsin tax initialization finished", false, 100);
            _logger.LogInformation("Wisconsin tax initialization completed for {JurisdictionCount} jurisdictions", groupIdByCountyCode.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wisconsin tax initialization failed");
            progressMessenger.AddProgressMessageText("Wisconsin tax initialization failed: " + ex.Message, true, 100);
            return false;
        }
    }

    private Dictionary<string, int> EnsureJurisdictions(IProgressMessenger progressMessenger)
    {
        var existingJurisdictions = JurisdictionManager.GetJurisdictions(TypeTax);
        var jurisdictionIdByCode = existingJurisdictions.Jurisdiction
            .Where(r => !string.IsNullOrEmpty(r.Code))
            .ToDictionary(r => r.Code, r => r.JurisdictionId, StringComparer.OrdinalIgnoreCase);

        var existingGroups = JurisdictionManager.GetJurisdictionGroups(TypeTax);
        var groupIdByCode = existingGroups.JurisdictionGroup
            .Where(r => !string.IsNullOrEmpty(r.Code))
            .ToDictionary(r => r.Code, r => r.JurisdictionGroupId, StringComparer.OrdinalIgnoreCase);

        var groupIdByCountyCode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var total = WisconsinTaxRates.All.Count;
        var processed = 0;

        foreach (var county in WisconsinTaxRates.All)
        {
            try
            {
                var groupCode = county.Code + "-GROUP";

                if (jurisdictionIdByCode.ContainsKey(county.Code) && groupIdByCode.TryGetValue(groupCode, out var existingGroupId))
                {
                    groupIdByCountyCode[county.Code] = existingGroupId;
                }
                else
                {
                    var (newJurisdictionId, newGroupId) = CreateJurisdictionWithGroup(county);
                    jurisdictionIdByCode[county.Code] = newJurisdictionId;
                    groupIdByCode[groupCode] = newGroupId;
                    groupIdByCountyCode[county.Code] = newGroupId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed jurisdiction for {CountyCode}", county.Code);
            }

            processed++;
            var percent = 5 + (int)(70.0 * processed / total);
            progressMessenger.AddProgressMessageText($"Jurisdiction {county.DisplayName}", false, percent);
        }

        return groupIdByCountyCode;
    }

    private static void EnsureTaxCategory(string name)
    {
        var existing = CatalogTaxManager.GetTaxCategoryByName(name);
        if (existing != null && existing.TaxCategory.Count > 0) return;
        CatalogTaxManager.CreateTaxCategory(name, false);
    }

    private static TaxDto.TaxRow EnsureTaxRow(TaxDto dto, string name, int sortOrder)
    {
        var taxRow = dto.Tax.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));

        if (taxRow == null)
        {
            taxRow = dto.Tax.NewTaxRow();
            taxRow.Name = name;
            taxRow.TaxType = (int)TaxType.SalesTax;
            taxRow.SortOrder = sortOrder;
            dto.Tax.AddTaxRow(taxRow);
        }

        if (!dto.TaxLanguage.Any(r => r.TaxId == taxRow.TaxId))
        {
            dto.TaxLanguage.AddTaxLanguageRow(name, TaxLanguageCode, taxRow);
        }

        return taxRow;
    }

    private static (int JurisdictionId, int GroupId) CreateJurisdictionWithGroup(WisconsinTaxRates.CountyTaxRate county)
    {
        var dto = new JurisdictionDto();
        var jurisdictionRow = dto.Jurisdiction.NewJurisdictionRow();

        jurisdictionRow.Code = county.Code;
        jurisdictionRow.DisplayName = county.DisplayName;
        jurisdictionRow.CountryCode = CountryCode;
        jurisdictionRow.StateProvinceCode = StateCode;
        jurisdictionRow.JurisdictionType = (int)TypeTax;
        jurisdictionRow.City = county.City ?? string.Empty;
        jurisdictionRow.District = string.Empty;
        jurisdictionRow.County = county.County ?? string.Empty;
        jurisdictionRow.GeoCode = string.Empty;
        jurisdictionRow.ZipPostalCodeStart = string.Empty;
        jurisdictionRow.ZipPostalCodeEnd = string.Empty;
        dto.Jurisdiction.AddJurisdictionRow(jurisdictionRow);

        var groupRow = dto.JurisdictionGroup.NewJurisdictionGroupRow();

        groupRow.Code = county.Code + "-GROUP";
        groupRow.DisplayName = county.DisplayName;
        groupRow.JurisdictionType = (int)TypeTax;
        dto.JurisdictionGroup.AddJurisdictionGroupRow(groupRow);
        dto.JurisdictionRelation.AddJurisdictionRelationRow(jurisdictionRow, groupRow);

        JurisdictionManager.SaveJurisdiction(dto);

        return (jurisdictionRow.JurisdictionId, groupRow.JurisdictionGroupId);
    }

    private static void UpsertTaxValue(TaxDto dto, TaxDto.TaxRow taxRow, int jurisdictionGroupId, decimal percentage)
    {
        var existing = dto.TaxValue.FirstOrDefault(r =>
            r.TaxId == taxRow.TaxId
            && r.JurisdictionGroupId == jurisdictionGroupId
            && string.Equals(r.TaxCategory, DefaultTaxCategoryName, StringComparison.OrdinalIgnoreCase));

        if (percentage <= 0m)
        {
            existing?.Delete();
            return;
        }

        if (existing != null)
        {
            existing.Percentage = (double)percentage;
            existing.AffectiveDate = DefaultEffectiveDate;
            return;
        }

        // Commerce 15 removed the per-site SiteId column from TaxValue (was Guid.Empty = all sites).
        dto.TaxValue.AddTaxValueRow(
            (double)percentage,
            taxRow,
            DefaultTaxCategoryName,
            jurisdictionGroupId,
            DefaultEffectiveDate);
    }
}
