using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;

public abstract class PageBasePublic : PageBase, IExcludeFromSitemap, ISnippetsWithInheritance
{
    #region Settings tab

    [Display(
        Name = "Exclude From Sitemap",
        Description = "If selected the page will not appear in the sitemap.",
        GroupName = SystemTabNames.Settings,
        Order = 10)]
    public virtual bool ExcludeFromSitemap { get; set; }

    #endregion

    #region Snippets tab

    [Display(
        Name = "Use Snippets From Site Settings",
        GroupName = Globals.Tabs.Snippets,
        Order = 110)]
    public virtual bool InheritSnippetsFromSettings { get; set; }

    [Display(
        Name = "Snippets Collection",
        GroupName = Globals.Tabs.Snippets,
        Order = 120)]
    [AllowedTypes(typeof(SnippetBlock))]
    [UIHint(Globals.SiteUIHints.Snippets)]
    public virtual ContentArea? Snippets { get; set; }

    #endregion

    #region Public methods

    public override void SetDefaultValues(ContentType contentType)
    {
        base.SetDefaultValues(contentType);

        InheritSnippetsFromSettings = true;
    }

    #endregion

    #region Public properties

    public virtual string BodyCss => string.Empty;

    #endregion
}
