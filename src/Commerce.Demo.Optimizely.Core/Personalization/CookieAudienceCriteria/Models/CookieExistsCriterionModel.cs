using CookieAudienceCriteria.Enums;
using EPiServer.Personalization.VisitorGroups;
using EPiServer.Web.Mvc.VisitorGroups;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.Core.Personalization.CookieAudienceCriteria.Models;

public class CookieExistsCriterionModel : CriterionModelBase
{
    [Required]
    public string CookieName { get; set; }

    [Required]
    [CriterionPropertyEditor(SelectionFactoryType = typeof(EnumSelectionFactory))]
    public CookieExistsCondition Condition { get; set; }

    public override ICriterionModel Copy()
    {
        return base.ShallowCopy();
    }
}
