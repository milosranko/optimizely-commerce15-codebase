using Commerce.Demo.Optimizely.ContentTypes.Cms.Media;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Mvc;
using static Commerce.Demo.Optimizely.Core.Constants.Globals;

namespace Commerce.Demo.Web.Infrastructure;

[ServiceConfiguration(typeof(IViewTemplateModelRegistrator))]
public class TemplateCoordinator : IViewTemplateModelRegistrator
{
    public void Register(TemplateModelCollection viewTemplateModelRegistrator)
    {
        viewTemplateModelRegistrator.Add(typeof(ImageFile), new TemplateModel
        {
            Name = "ImageFile",
            Tags = [UIHint.Image],
            AvailableWithoutTag = true,
            Path = "~/Views/Shared/DisplayTemplates/ImageFile.cshtml"
        });

        //viewTemplateModelRegistrator.Add(typeof(TeaserBlock), new TemplateModel
        //{
        //    Name = "TeaserBlockWide",
        //    Tags = new[] { Globals.ContentAreaTags.WideWidth, Globals.ContentAreaTags.FullWidth },
        //    AvailableWithoutTag = false,
        //});

        //viewTemplateModelRegistrator.Add(typeof(ArticlePage), new TemplateModel
        //{
        //    //Name = "ArticleCard",
        //    Tags = new[] { "ArticleCard" },
        //    //TemplateTypeCategory = TemplateTypeCategories.MvcPartialComponent,
        //    //AvailableWithoutTag = false,
        //});
    }

    public static void OnTemplateResolved(object? sender, TemplateResolverEventArgs args)
    {
        if (args.ItemToRender is IContainerPage && args.SelectedTemplate != null)
        {
            args.SelectedTemplate = null;
        }
    }

    private static string BlockPath(string fileName) => $"{ViewLocations.BlocksFolder}{fileName}";
    private static string PagePartialPath(string fileName) => $"{ViewLocations.PagePartialsFolder}{fileName}";
}
