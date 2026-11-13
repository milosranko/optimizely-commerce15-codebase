using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;
using Commerce.Demo.Optimizely.Core.Attributes;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Pages;

[ContentType(
    DisplayName = "Checkout Page",
    GUID = "{DFD62191-F53E-4BCC-A0B5-EC8637CCD32E}",
    Description = "Checkout page",
    GroupName = Globals.Tabs.Commerce,
    AvailableInEditMode = true)]
[AvailableContentTypes(IncludeOn = new[] { typeof(StartPage) })]
[SiteImageUrl("checkout-page.png")]
public class CheckoutPage : PageBasePublic
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
