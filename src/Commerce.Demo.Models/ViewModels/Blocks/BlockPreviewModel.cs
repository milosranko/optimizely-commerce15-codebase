using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Base;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Interfaces;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;
using Commerce.Demo.Optimizely.Core.Extensions;
using Commerce.Demo.Web.Models.Pages;
using EPiServer.Core;

namespace Commerce.Demo.Models.ViewModels.Blocks;

public record BlockPreviewModel : PageViewModel<PageBasePublic>, IPreviewCustomHtml
{
    public ContentArea ContentArea { get; set; }

    public BlockPreviewModel(PageBasePublic page, IContent content) : base(page)
    {
        Layout.PageTitle = $"Preview of {content.Name}";
        ContentArea = new ContentArea();
        ContentArea.Items.Add(new ContentAreaItem
        {
            ContentLink = content.ContentLink
        });

        var block = content.ContentLink.GetBlock<BlockBase>();

        if (block is IPreviewCustomHtml)
        {
            var previewCustomHtml = block as IPreviewCustomHtml;

            FirstLevelTag = previewCustomHtml?.FirstLevelTag;
            FirstLevelCssClass = previewCustomHtml?.FirstLevelCssClass;
            SecondLevelTag = previewCustomHtml?.SecondLevelTag;
            SecondLevelCssClass = previewCustomHtml?.SecondLevelCssClass;
        }
    }

    #region IPreviewCustomHtml properties

    public string? FirstLevelTag { get; }
    public string? FirstLevelCssClass { get; }
    public string? SecondLevelTag { get; }
    public string? SecondLevelCssClass { get; }

    #endregion
}
