using Commerce.Demo.Optimizely.Core.Personalization.CookieAudienceCriteria.Models;
using CookieAudienceCriteria.Enums;
using EPiServer.Personalization.VisitorGroups;
using Microsoft.AspNetCore.Http;
using System.Security.Principal;

namespace Commerce.Demo.Optimizely.Core.Personalization.CookieAudienceCriteria.Criterion;

[VisitorGroupCriterion(
    Category = "Technical Criteria",
    Description = "Check a cookie value",
    DisplayName = "Cookie value",
    LanguagePath = "/cookieaudiencecriteria/enums/cookievaluecondition")]
public class CookieValueCriteria : CriterionBase<CookieValueCriterionModel>
{
    public override bool IsMatch(IPrincipal principal, HttpContext httpContext)
    {
        if (httpContext?.Request?.Cookies is null || !httpContext.Request.Cookies.ContainsKey(Model.CookieName))
        {
            return false;
        }

        var cookie = httpContext.Request.Cookies[Model.CookieName];

        if (string.IsNullOrWhiteSpace(cookie))
        {
            return false;
        }

        return Model.Condition switch
        {
            CookieValueCondition.Contains => Model.CaseSensitive
                                    ? cookie.IndexOf(Model.CookieValue, 0, StringComparison.CurrentCulture) >= 0
                                    : cookie.IndexOf(Model.CookieValue, 0, StringComparison.CurrentCultureIgnoreCase) >= 0,
            CookieValueCondition.DoesNotContain => !Model.CaseSensitive
                                    ? cookie.IndexOf(Model.CookieValue, 0, StringComparison.CurrentCulture) >= 0
                                    : cookie.IndexOf(Model.CookieValue, 0, StringComparison.CurrentCultureIgnoreCase) >= 0,
            CookieValueCondition.Is => Model.CaseSensitive
                                    ? Model.CookieValue.Equals(cookie, StringComparison.CurrentCulture)
                                    : Model.CookieValue.Equals(cookie, StringComparison.CurrentCultureIgnoreCase),
            CookieValueCondition.IsNot => !(Model.CaseSensitive
                                    ? Model.CookieValue.Equals(cookie, StringComparison.CurrentCulture)
                                    : Model.CookieValue.Equals(cookie, StringComparison.CurrentCultureIgnoreCase)),
            CookieValueCondition.StartsWith => Model.CaseSensitive
                                    ? cookie.StartsWith(Model.CookieValue, StringComparison.CurrentCulture)
                                    : cookie.StartsWith(Model.CookieValue, StringComparison.CurrentCultureIgnoreCase),
            CookieValueCondition.EndsWith => Model.CaseSensitive
                                    ? cookie.EndsWith(Model.CookieValue, StringComparison.CurrentCulture)
                                    : cookie.EndsWith(Model.CookieValue, StringComparison.CurrentCultureIgnoreCase),
            _ => false,
        };
    }
}
