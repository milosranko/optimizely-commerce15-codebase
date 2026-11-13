using Commerce.Demo.Business.Features.Personalization.CookiesConsentAudienceCriteria.Enums;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Enums;
using EPiServer.Personalization.VisitorGroups;
using EPiServer.Web.Mvc.VisitorGroups;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Business.Features.Personalization.CookiesConsentAudienceCriteria.Models;

public class CookiesConsentCriterionModel : CriterionModelBase
{
    /// <summary>
    /// The condition to apply when checking the cookie value
    /// </summary>
    [Required]
    [CriterionPropertyEditor(SelectionFactoryType = typeof(EnumSelectionFactory))]
    public CookieUsageCondition Condition { get; set; }

    /// <summary>
    /// The value to check for in the cookie
    /// </summary>
    [Required]
    [CriterionPropertyEditor(SelectionFactoryType = typeof(EnumSelectionFactory))]
    public SnippetUsage CookieUsage { get; set; }

    public override ICriterionModel Copy()
    {
        return base.ShallowCopy();
    }
}
