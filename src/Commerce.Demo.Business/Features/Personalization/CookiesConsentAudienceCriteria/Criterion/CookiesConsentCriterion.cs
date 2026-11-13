using Commerce.Demo.Business.Features.Personalization.CookiesConsentAudienceCriteria.Enums;
using Commerce.Demo.Business.Features.Personalization.CookiesConsentAudienceCriteria.Models;
using Commerce.Demo.Business.Features.SiteSettings.Interfaces;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages;
using EPiServer.Globalization;
using EPiServer.Personalization.VisitorGroups;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Principal;

namespace Commerce.Demo.Business.Features.Personalization.CookiesConsentAudienceCriteria.Criterion;

[VisitorGroupCriterion(
    Category = "Technical Criteria",
    Description = "Check a cookies consent value",
    DisplayName = "Cookies consent value",
    LanguagePath = "/cookieaudiencecriteria/enums/cookievaluecondition")]
public class CookiesConsentCriteria : CriterionBase<CookiesConsentCriterionModel>
{
    public override bool IsMatch(IPrincipal principal, HttpContext httpContext)
    {
        var siteSettingsManager = httpContext.RequestServices.GetRequiredService<ISiteSettingsManager>();
        var siteSettings = siteSettingsManager.CurrentSiteSettings<SiteSettingsPage, dynamic>(ContentLanguage.PreferredCulture);

        if (httpContext?.Request?.Cookies is null ||
            siteSettings?.CookiesConsent?.CookieName is null ||
            !httpContext.Request.Cookies.ContainsKey(siteSettings.CookiesConsent.CookieName))
            return true;

        var cookieValue = httpContext.Request.Cookies[(string)siteSettings.CookiesConsent.CookieName];

        if (string.IsNullOrEmpty(cookieValue) || bool.TryParse(cookieValue, out bool isFalse) == false)
            return true;

        var cookieConsentValue = cookieValue.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return Model.Condition switch
        {
            CookieUsageCondition.Is => cookieConsentValue.Contains(Model.CookieUsage.ToString().ToLower()),
            CookieUsageCondition.IsNot => !cookieConsentValue.Contains(Model.CookieUsage.ToString().ToLower()),
            _ => true,
        };
    }
}
