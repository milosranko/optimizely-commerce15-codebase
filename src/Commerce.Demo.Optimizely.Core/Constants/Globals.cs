using EPiServer.DataAnnotations;
using EPiServer.Security;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.Core.Constants;

public class Globals
{
    // CMS 13 rejects tab (group) names containing spaces: the values use underscores, the editor-facing
    // label stays in [Display(Name)]. Renaming a value creates a new tab identifier, so tab access rights
    // or ordering set in Admin mode for "Aside content", "Site Settings", "Cookies Consent" must be re-applied.
    [GroupDefinitions]
    public static class Tabs
    {
        [Display(Name = "Default", Order = 10)]
        public const string Default = "Default";

        [Display(Name = "Sharing", Order = 20)]
        public const string Sharing = "Sharing";

        [Display(Name = "Footer", Order = 30)]
        public const string Footer = "Footer";

        [Display(Name = "Aside content", Order = 40)]
        public const string AsideContent = "Aside_content";

        [Display(Name = "Site Settings", Order = 50)]
        public const string SiteSettings = "Site_Settings";

        [Display(Name = "Specialized", Order = 60)]
        public const string Specialized = "Specialized";

        [Display(Name = "Commerce", Order = 70)]
        public const string Commerce = "Commerce";

        [Display(Name = "Snippets", Order = 80)]
        public const string Snippets = "Snippets";

        [Display(Name = "Cookies Consent", Order = 85)]
        public const string CookiesConsent = "Cookies_Consent";

        [Display(Name = "Search", Order = 90)]
        public const string Search = "Search";

        [Display(Name = "Secrets", Order = 100)]
        [RequiredAccess(Access = AccessLevel.Administer)]
        public const string Secrets = "Secrets";
    }

    public static class GroupNames
    {
        public const string Products = "Products";
        public const string Fashion = "Fashion";
        public const string Event = "Event";
        public const string Book = "Book";
    }

    public static class MetaClassNames
    {
        public const string BookProduct = "Book_Product";
        public const string BookVariant = "Book_Variant";
        public const string EventProduct = "Event_Product";
        public const string EventVariant = "Event_Variant";
        public const string FashionProduct = "Fashion_Product";
        public const string FashionVariant = "Fashion_Variant";
    }

    public static class CartNames
    {
        public const string ShoppingCart = "ShoppingCart";
        public const string WishList = "WishList";
    }

    public static class SiteUIHints
    {
        public const string StringList = "StringList";
        public const string StringsCollection = "StringsCollection";
        public const string DisableBlocksInsideXhtml = "DisableBlocksInsideXhtml";
        public const string WideTextArea = "WideTextArea";
        public const string Snippets = "Snippets";
        public const string HexColor = "HexColor";
    }

    public static class Validation
    {
        /// <summary>A colour as #RRGGBB, e.g. #1A2B3C.</summary>
        public const string HexColorPattern = "^#[0-9A-Fa-f]{6}$";
    }

    public static class ContentAreaTags
    {
        public const string FullWidth = "full";
        public const string WideWidth = "wide";
        public const string HalfWidth = "half";
        public const string NarrowWidth = "narrow";
        public const string NoRenderer = "norenderer";
    }

    public static class ViewLocations
    {
        public const string BlocksFolder = "~/Views/Shared/Blocks/";
        public const string PagePartialsFolder = "~/Views/Shared/PagePartials/";
    }
}
