using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;
using Commerce.Demo.Optimizely.Core.Attributes;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Pages;

[ContentType(
    DisplayName = "Cart Page",
    GUID = "{65182793-92A8-4398-926A-7AB50C62D57C}",
    Description = "Cart page",
    GroupName = Globals.Tabs.Commerce,
    AvailableInEditMode = true)]
[AvailableContentTypes(IncludeOn = new[] { typeof(StartPage) })]
[SiteImageUrl("cart-page.png")]
public class CartPage : PageBasePublic
{
    #region Content tab

    [Display(
        Name = "Empty Basket Text",
        Description = "The HTML to display to the user when their basket is empty",
        GroupName = SystemTabNames.Content,
        Order = 100)]
    [CultureSpecific]
    public virtual XhtmlString? EmptyBasketText { get; set; }

    #endregion
}
