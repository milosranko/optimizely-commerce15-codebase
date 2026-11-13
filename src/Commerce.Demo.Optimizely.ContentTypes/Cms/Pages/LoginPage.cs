using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;
using Commerce.Demo.Optimizely.Core.Attributes;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Pages;

[ContentType(
    DisplayName = "Login Page",
    GUID = "{B1C9F3B6-7E5A-4F8B-9C2D-1A2B3C4D5E6F}",
    Description = "Page for user login",
    GroupName = Globals.Tabs.Specialized)]
[SiteImageUrl("login-page.png")]
public class LoginPage : PageBasePublic
{ }
