using Commerce.Demo.Optimizely.Core.Personalization.CookieAudienceCriteria.Models;
using CookieAudienceCriteria.Enums;
using EPiServer.Personalization.VisitorGroups;
using Microsoft.AspNetCore.Http;
using System.Security.Principal;

namespace Commerce.Demo.Optimizely.Core.Personalization.CookieAudienceCriteria.Criterion;

[VisitorGroupCriterion(
    Category = "Technical Criteria",
    Description = "Check if cookie exist (or not)",
    DisplayName = "Cookie exists",
    LanguagePath = "/cookieaudiencecriteria/enums/cookieexistscondition")]
public class CookieExistsCriterion : CriterionBase<CookieExistsCriterionModel>
{
    public override bool IsMatch(IPrincipal principal, HttpContext httpContext)
    {
        var cookie = httpContext.Request.Cookies[Model.CookieName];

        return Model.Condition switch
        {
            CookieExistsCondition.Exists => cookie != null,
            CookieExistsCondition.DoesNotExist => cookie == null,
            _ => false,
        };
    }
}
