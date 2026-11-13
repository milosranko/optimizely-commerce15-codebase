using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.EditorDescriptors;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Products.Interfaces;
using Commerce.Demo.Optimizely.ContentTypes.Commerce.Properties;
using Commerce.Demo.Optimizely.ContentTypes.Shared.Blocks;
using Commerce.Demo.Optimizely.Core.Constants;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.DataAnnotations;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Shell.ObjectEditing;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Products.Base;

public abstract class ProductBase : ProductContent, ILayoutPageBase, IExcludeFromSitemap, ITabs, IAccordion
{
    #region Content tab

    [Display(
        Name = "Tabs",
        Order = 10,
        GroupName = SystemTabNames.Content)]
    [BackingType(typeof(LabelXhtmlPropertyMetaData))]
    [EditorDescriptor(EditorDescriptorType = typeof(LabelXhtmlCollectionEditorDescriptor<LabelXhtmlProperty>))]
    public virtual IList<LabelXhtmlProperty>? Tabs { get; set; }

    [Display(
        Name = "Primary Accordion",
        Order = 20,
        GroupName = SystemTabNames.Content)]
    [BackingType(typeof(DropDownLabelXhtmlPropertyMetaData))]
    [EditorDescriptor(EditorDescriptorType = typeof(DropDownLabelXhtmlCollectionEditorDescriptor<DropDownLabelXhtmlProperty>))]
    public virtual IList<DropDownLabelXhtmlProperty>? PrimaryAccordion { get; set; }

    [Display(
        Name = "Secondary Accordion",
        Order = 30,
        GroupName = SystemTabNames.Content)]
    [BackingType(typeof(LabelXhtmlPropertyMetaData))]
    [EditorDescriptor(EditorDescriptorType = typeof(LabelXhtmlCollectionEditorDescriptor<LabelXhtmlProperty>))]
    public virtual IList<LabelXhtmlProperty>? SecondaryAccordion { get; set; }

    [Display(
        Name = "Allow Ordering",
        Order = 40,
        GroupName = SystemTabNames.Content)]
    public virtual bool AllowOrdering { get; set; }

    #endregion

    #region SEO tab

    [Display(
        Name = "No Robots",
        GroupName = TabNames.SEOInformation,
        Order = 20)]
    [CultureSpecific]
    public virtual bool MetaNoRobots { get; set; }

    #endregion

    #region Sharing tab

    [Display(
        GroupName = Globals.Tabs.Sharing,
        Order = 20)]
    public virtual OpenGraphBlock? OpenGraphBlock { get; set; }

    #endregion

    #region Settings tab

    [Display(
        Name = "Exclude From XML Sitemap",
        Description = "If selected the page will not appear in the XML sitemap.",
        GroupName = SystemTabNames.Settings,
        Order = 40)]
    [CultureSpecific]
    public virtual bool ExcludeFromSitemap { get; set; }

    [Display(
        Name = "Exclude From Site Search",
        Description = "If selected the page will not appear in the search results.",
        GroupName = SystemTabNames.Settings,
        Order = 50)]
    public virtual bool ExcludeFromSearch { get; set; }

    #endregion
}
