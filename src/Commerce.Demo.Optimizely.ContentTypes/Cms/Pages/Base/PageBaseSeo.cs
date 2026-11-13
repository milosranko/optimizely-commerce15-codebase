using Commerce.Demo.Optimizely.ContentTypes.Shared.Blocks;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Commerce.Catalog.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;

public abstract class PageBaseSeo : PageBasePublic
{
    #region MetaData tab

    [Display(
        Name = "SEO",
        GroupName = TabNames.SEOInformation,
        Order = 100)]
    public virtual SeoBlock? SeoBlock { get; set; }

    [Display(
        Name = "Open Graph",
        GroupName = Globals.Tabs.Sharing,
        Order = 110)]
    public virtual OpenGraphBlock? OpenGraphBlock { get; set; }

    #endregion
}
