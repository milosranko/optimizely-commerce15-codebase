using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;
using Commerce.Demo.Optimizely.Core.Attributes;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Pages;

[ContentType(
    GUID = "{005DBD8E-B8C6-4C0F-8066-44CFE01FD535}",
    DisplayName = "404 Not Found Page",
    GroupName = Globals.Tabs.Specialized)]
[SiteImageUrl("NotFoundPage.png")]
public class NotFoundPage : NotFoundPageBase
{
    #region Content tab

    [CultureSpecific]
    [Display(
        Name = "Heading",
        GroupName = SystemTabNames.Content,
        Order = 100)]
    public virtual string? Heading { get; set; }

    #endregion
}
