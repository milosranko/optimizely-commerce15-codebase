using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Categories.Base;

public abstract class CategoryBase : NodeContent, ILayoutPageBase
{
    #region Content tab

    [Display(
        GroupName = SystemTabNames.Content,
        Order = 10)]
    [CultureSpecific]
    public virtual string? Heading { get; set; }

    [Display(
        GroupName = SystemTabNames.Content,
        Order = 30)]
    [CultureSpecific]
    public virtual XhtmlString? IntroText { get; set; }

    #endregion
}
