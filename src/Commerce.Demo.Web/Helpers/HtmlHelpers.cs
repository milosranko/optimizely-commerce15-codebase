using Commerce.Demo.Business.Infrastructure.Cookies;
using Commerce.Demo.Models.ViewModels;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Base;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Enums;
using Commerce.Demo.Optimizely.ContentTypes.Cms.Media.Base;
using Commerce.Demo.Optimizely.Core.Extensions;
using Commerce.Demo.Optimizely.Core.Helpers;
using EPiServer.Core.Internal;
using EPiServer.Framework.Web;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Mvc.Html;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Linq.Expressions;
using System.Text;
using System.Text.Encodings.Web;
using System.Web;

namespace Commerce.Demo.Web.Helpers;

public static class HtmlHelpers
{
    #region Snippets

    public static async Task RenderSnippetsAsync(
        this IHtmlHelper helper,
        SnippetArea placement,
        IEnumerable<SnippetBlockViewModel>? snippets,
        CookiesConsentDialogViewModel? cookiesConsent)
    {
        if (snippets is null || !snippets.Any(x => x.Placement.Equals(placement)) || helper is null)
            return;

        var consentCookieValue = Array.Empty<string>();
        var cookieService = helper.ViewContext.HttpContext.RequestServices.GetInstance<ICookiesService>();
        var cookiesConsentCookieValue = cookieService.Get(cookiesConsent?.CookieName);

        if (cookiesConsent != null &&
            !string.IsNullOrEmpty(cookiesConsent.CookieName) &&
            !string.IsNullOrEmpty(cookiesConsentCookieValue))
        {
            consentCookieValue = cookiesConsentCookieValue.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        await WriteSnippetsAsync(snippets.Where(x => x.Placement.Equals(placement)), cookiesConsent?.CookieName, consentCookieValue, helper.ViewContext);
    }

    private static bool ShouldRenderSnippet(SnippetBlockViewModel snippet, string[] consentCookieValue, string? cookieName)
    {
        if (PageHelpers.IsInEditMode() && !snippet.EnableInEditMode)
            return false;

        // Essential (strictly necessary) snippets are always allowed, regardless of the client-supplied consent value.
        if (snippet.Usage == SnippetUsage.Essential)
            return true;

        if (!snippet.RequiresVisitorConsent || string.IsNullOrEmpty(cookieName))
            return true;

        return consentCookieValue.Length != 0 && consentCookieValue.Contains(snippet.Usage.ToString().ToLower());
    }

    private static async Task WriteSnippetsAsync(IEnumerable<SnippetBlockViewModel> snippets, string? cookieName, string[] consentCookieValue, ViewContext context)
    {
        if (!snippets.Any())
            return;

        foreach (var snippet in snippets)
        {
            if (ShouldRenderSnippet(snippet, consentCookieValue, cookieName))
            {
                await context.Writer.WriteLineAsync($"<!-- {snippet.Name} -->");
                await context.Writer.WriteLineAsync(snippet.Content);
            }
        }
    }

    #endregion

    #region Meta helper

    public static void MetaTag(this IHtmlHelper helper, string? name, string? value)
    {
        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
        {
            helper.ViewContext.Writer.WriteLine("<meta name=\"{0}\" content=\"{1}\" />", name, value);
        }
    }

    public static void OpenGraphTag(this IHtmlHelper helper, string? name, string? value)
    {
        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
        {
            helper.ViewContext.Writer.WriteLine("<meta property=\"{0}\" content=\"{1}\" />", name, value);
        }
    }

    #endregion

    #region Menu helper

    /// <summary>
    /// Returns an element for each child page of the rootLink using the itemTemplate.
    /// </summary>
    /// <param name="helper">The html helper in whose context the list should be created</param>
    /// <param name="rootLink">A reference to the root whose children should be listed</param>
    /// <param name="itemTemplate">A template for each page which will be used to produce the return value. Can be either a delegate or a Razor helper.</param>
    /// <param name="includeRoot">Wether an element for the root page should be returned</param>
    /// <param name="requireVisibleInMenu">Wether pages that do not have the "Display in navigation" checkbox checked should be excluded</param>
    /// <param name="requirePageTemplate">Wether page that do not have a template (i.e. container pages) should be excluded</param>
    /// <remarks>
    /// Filter by access rights and publication status.
    /// </remarks>
    public static IHtmlContent MenuList(
        this IHtmlHelper helper,
        ContentReference rootLink,
        Func<MenuItem, HelperResult>? itemTemplate = null,
        bool includeRoot = false,
        bool requireVisibleInMenu = true,
        bool requirePageTemplate = true)
    {
        itemTemplate = itemTemplate ?? GetDefaultItemTemplate(helper);
        var currentContentLink = helper.ViewContext.HttpContext.GetContentLink();
        var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();

        Func<IEnumerable<PageData>, IEnumerable<PageData>> filter =
            pages => pages.FilterForDisplay(requirePageTemplate, requireVisibleInMenu);

        var pagePath = contentLoader.GetAncestors(currentContentLink)
            .Reverse()
            .Select(x => x.ContentLink)
            .SkipWhile(x => !x.CompareToIgnoreWorkID(rootLink))
            .ToList();

        var menuItems = contentLoader.GetChildren<PageData>(rootLink)
            .FilterForDisplay(requirePageTemplate, requireVisibleInMenu)
            .Select(x => CreateMenuItem(x, currentContentLink, pagePath, contentLoader, filter))
            .ToList();

        if (includeRoot)
        {
            menuItems.Insert(0, CreateMenuItem(contentLoader.Get<PageData>(rootLink), currentContentLink, pagePath, contentLoader, filter));
        }

        var buffer = new StringBuilder();
        var writer = new StringWriter(buffer);
        foreach (var menuItem in menuItems)
        {
            itemTemplate(menuItem).WriteTo(writer, HtmlEncoder.Default);
        }

        return new HtmlString(buffer.ToString());
    }

    private static MenuItem CreateMenuItem(
        PageData page,
        ContentReference currentContentLink,
        List<ContentReference> pagePath,
        IContentLoader contentLoader,
        Func<IEnumerable<PageData>,
            IEnumerable<PageData>> filter)
    {
        var menuItem = new MenuItem(page)
        {
            Selected = page.ContentLink.CompareToIgnoreWorkID(currentContentLink) ||
                       pagePath.Contains(page.ContentLink),
            HasChildren = new Lazy<bool>(() => filter(contentLoader.GetChildren<PageData>(page.ContentLink)).Any())
        };
        return menuItem;
    }

    private static Func<MenuItem, HelperResult> GetDefaultItemTemplate(IHtmlHelper helper)
    {
        return x => new HelperResult(writer =>
        {
            helper.PageLink(x.Page).WriteTo(writer, HtmlEncoder.Default);
            return Task.CompletedTask;
        });
    }

    public class MenuItem
    {
        public MenuItem(PageData page)
        {
            Page = page;
        }
        public PageData Page { get; set; }
        public bool Selected { get; set; }
        public Lazy<bool> HasChildren { get; set; }
    }

    #endregion

    #region EditableField

    public static async Task<IHtmlContent> EditableField<TModel, TResult>(
        this IHtmlHelper<TModel> model,
        Expression<Func<TModel, TResult>> expression,
        string tagName,
        object? htmlAttributes = null,
        bool addPlaceholder = false,
        string? tags = null)
    {
        var expressionProvider = new ModelExpressionProvider(model.MetadataProvider);
        var metadata = expressionProvider.CreateModelExpression(model.ViewData, expression);
        var hasValue = metadata.Model != null;

        // edit mode (with or without value)
        if (PageHelpers.IsInEditMode())
        {
            return await RenderPropertyForEditMode<TModel, TResult>(tagName, htmlAttributes, metadata, model, addPlaceholder, tags);
        }
        // view mode with value
        if (hasValue)
        {
            return await RenderPropertyForViewMode<TModel, TResult>(tagName, htmlAttributes, metadata, model, tags);
        }
        // view mode without value
        return HtmlString.Empty;
    }

    private static async Task<IHtmlContent> RenderPropertyForEditMode<TModel, TResult>(
        string tagName,
        object htmlAttributes,
        ModelExpression metadata,
        IHtmlHelper htmlHelper,
        bool addPlaceholder,
        string tags)
    {
        var imageInlineEditTag = new TagBuilder("div");
        var tagBuilder = new TagBuilder(tagName);
        var cssClass = string.Empty;
        RouteValueDictionary? attributes = null;

        if (htmlAttributes != null)
        {
            attributes = new RouteValueDictionary(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        if (!addPlaceholder)
        {
            tagBuilder.MergeAttribute("data-epi-property-name", metadata.Metadata.PropertyName);
            tagBuilder.MergeAttribute("data-epi-use-mvc", "True");
        }
        else
        {
            imageInlineEditTag.MergeAttribute("class", $"epi-editContainer editContainerImage {cssClass}");
            imageInlineEditTag.MergeAttribute("data-epi-property-name", metadata.Metadata.PropertyName);
            imageInlineEditTag.MergeAttribute("data-epi-overlay-z-index", "-1");
            imageInlineEditTag.MergeAttribute("data-epi-use-mvc", "True");
        }

        if (metadata.Model != null && !tagName.Equals("img"))
        {
            if (metadata.ModelExplorer.ModelType == typeof(string))
            {
                var text = metadata.Model.ToString();

                if (metadata.Metadata.TemplateHint == UIHint.Textarea)
                {
                    tagBuilder.InnerHtml.SetHtmlContent(text.ConvertNewLineToBR());
                }
                else
                {
                    tagBuilder.InnerHtml.SetContent(text);
                }
            }
            else if (metadata.ModelExplorer.ModelType == typeof(ContentReference) && metadata.Metadata.TemplateHint == UIHint.Block)
            {
                tagBuilder.InnerHtml.SetHtmlContent(await RenderBlockByContentReference(htmlHelper, metadata.Model as ContentReference, tags));
            }
            else
            {
                tagBuilder.InnerHtml.SetContent(metadata.Model.ToString());
            }
        }
        else if (tagName.Equals("img"))
        {
            tagBuilder.TagRenderMode = TagRenderMode.SelfClosing;

            if (metadata.Model != null)
            {
                SetImageAttributes((ContentReference)metadata.Model, ref attributes);
            }
        }

        tagBuilder.MergeAttributes(attributes);

        if (tagName.Equals("img"))
        {
            if (addPlaceholder && metadata.Model != null)
            {
                imageInlineEditTag.InnerHtml.SetHtmlContent(tagBuilder);
                return imageInlineEditTag;
            }
        }

        return tagBuilder.ToHtmlString();
    }

    private static async Task<IHtmlContent> RenderPropertyForViewMode<TModel, TResult>(
        string tagName,
        object htmlAttributes,
        ModelExpression metadata,
        IHtmlHelper htmlHelper,
        string tags)
    {
        RouteValueDictionary? attributes = null;
        var tagBuilder = new TagBuilder(tagName);

        if (htmlAttributes != null)
        {
            attributes = new RouteValueDictionary(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        if (!tagName.Equals("img"))
        {
            if (metadata.ModelExplorer.ModelType == typeof(string) && metadata.Metadata.TemplateHint == UIHint.Textarea)
            {
                tagBuilder.InnerHtml.SetHtmlContent(metadata.Model.ToString().ConvertNewLineToBR());
            }
            else if (metadata.ModelExplorer.ModelType == typeof(ContentReference) && metadata.Metadata.TemplateHint == UIHint.Block)
            {
                tagBuilder.InnerHtml.SetHtmlContent(await RenderBlockByContentReference(htmlHelper, metadata.Model as ContentReference, tags));
            }
            else
            {
                tagBuilder.InnerHtml.SetContent(metadata.Model.ToString());
            }
        }
        else if (metadata.ModelExplorer.ModelType == typeof(ContentReference) && !ContentReference.IsNullOrEmpty((ContentReference)metadata.Model))
        {
            tagBuilder.TagRenderMode = TagRenderMode.SelfClosing;
            SetImageAttributes((ContentReference)metadata.Model, ref attributes);
        }

        tagBuilder.MergeAttributes(attributes);

        return tagBuilder.ToHtmlString();
    }

    private static IHtmlContent ToHtmlString(this TagBuilder tagBuilder)
    {
        using (var writer = new StringWriter())
        {
            tagBuilder.WriteTo(writer, HtmlEncoder.Default);
            return new HtmlString(HttpUtility.HtmlDecode(writer.ToString()));
        }
    }

    private static void SetImageAttributes(
        ContentReference imageRef,
        ref RouteValueDictionary attributes)
    {
        var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
        var urlResolver = ServiceLocator.Current.GetInstance<UrlResolver>();
        var image = contentLoader.Get<ImageFileBase>(imageRef);
        var imageAlt = image.AlternateText ?? string.Empty;
        var imageUrl = urlResolver.GetUrl(imageRef);
        var src = imageUrl;

        if (attributes.ContainsKey("srcset"))
        {
            var srcSet = (string?)attributes["srcset"];
            src = imageUrl.GetSrc(srcSet?.Split(['|'], StringSplitOptions.RemoveEmptyEntries).First(), (string?)attributes["mode"]);

            if (attributes["sizes"] != null)
            {
                attributes["srcset"] = imageUrl.GetSrcSet(srcSet, (string?)attributes["mode"]);
            }
            else
            {
                attributes.Remove("srcset");
            }
        }

        attributes.Add("src", src);
        attributes.Add("alt", imageAlt);

        if (attributes["mode"] != null)
        {
            attributes.Remove("mode");
        }
    }

    private static async Task<IHtmlContent?> RenderBlockByContentReference(
        this IHtmlHelper htmlHelper,
        ContentReference blockContentReference,
        string tags)
    {
        if (blockContentReference == ContentReference.EmptyReference)
        {
            return null;
        }

        var contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
        var block = contentRepository.Get<BlockBase>(blockContentReference);

        if (block == null)
        {
            return null;
        }

        var templateResolver = ServiceLocator.Current.GetInstance<TemplateResolver>();
        var httpContext = ServiceLocator.Current.GetInstance<IHttpContextAccessor>().HttpContext;
        var template = templateResolver.Resolve(httpContext, block, TemplateTypeCategories.Request, tags);

        return await HtmlHelperPartialExtensions.PartialAsync(htmlHelper, template.Name, block);
    }

    #endregion

    #region Colored heading

    public static IHtmlContent ApplyStyle(this string text, string tagName = "span", string className = "")
    {
        var parsed = string.Empty;

        if (!string.IsNullOrEmpty(text))
        {
            if (text.Contains("*-") && text.Contains("-*"))
            {
                var cssClass = !string.IsNullOrEmpty(className) ? $" class=\"{className}\"" : "";
                parsed = text.Replace("*-", $"<{tagName}{cssClass}>").Replace("-*", $"</{tagName}>");
            }
            else
            {
                parsed = text;
            }
        }

        return ApplyNewLine(parsed);
    }

    public static IHtmlContent ApplyNewLine(this string text)
    {
        return text.ConvertNewLineToBR();
    }

    #endregion

    #region Cloudflare image resize

    private const string CloudFlareCdnPattern = "/cdn-cgi/image/w={0},q={1},g=auto,fit=cover,f=auto{2}";

    public static IHtmlContent ImageTagFor<TModel, TResult>(
        this IHtmlHelper<TModel> helper,
        Expression<Func<TModel, TResult>> expression,
        string[] sizes,
        object? htmlAttributes = null)
    {
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentNullException.ThrowIfNull(expression);

        var func = expression.Compile();
        var value = func(helper.ViewData.Model);
        var contentRef = value as ContentReference;

        if (ContentReference.IsNullOrEmpty(contentRef))
            return HtmlString.Empty;

        var services = helper.ViewContext.HttpContext.RequestServices;
        var contentRepository = services.GetInstance<IContentRepository>();
        var image = contentRepository.Get<ImageData>(contentRef);

        if (image == null)
            return HtmlString.Empty;

        var tagBuilder = new TagBuilder("img");
        var srcSet = new StringBuilder();
        var urlResolver = services.GetInstance<IUrlResolver>();
        var webHostEnvironment = services.GetInstance<IWebHostEnvironment>();

        foreach (var size in sizes)
        {
            var resolutionDim = size.Split('x');
            var url = GetUrl(contentRef, resolutionDim, urlResolver, webHostEnvironment);

            srcSet.Append($"{url} {resolutionDim[0]}w");

            if (sizes.Last() != size)
                srcSet.Append(", ");
        }

        tagBuilder.Attributes.Add("srcset", srcSet.ToString());

        var srcsetValue = srcSet.ToString();

        if (!string.IsNullOrEmpty(srcsetValue))
        {
            var firstUrl = srcsetValue.Split(' ')[0];
            tagBuilder.Attributes.Add("src", firstUrl);
        }

        tagBuilder.Attributes.Add("loading", "lazy");
        tagBuilder.Attributes.Add("decoding", "async");

        if (htmlAttributes != null)
        {
            var attributes = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);

            foreach (var attr in attributes)
            {
                if (attr.Key.Equals("class", StringComparison.CurrentCultureIgnoreCase))
                    tagBuilder.AddCssClass(attr.Value?.ToString() ?? string.Empty);
                else
                    tagBuilder.Attributes[attr.Key] = attr.Value?.ToString();
            }
        }

        return tagBuilder;
    }

    public static IHtmlContent ImageUrlFor<TModel, TResult>(
        this IHtmlHelper<TModel> helper,
        Expression<Func<TModel, TResult>> expression,
        string? size = null)
    {
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentNullException.ThrowIfNull(expression);

        var func = expression.Compile();
        var value = func(helper.ViewData.Model);
        var contentRef = value as ContentReference;

        if (ContentReference.IsNullOrEmpty(contentRef))
            return HtmlString.Empty;

        var services = helper.ViewContext.HttpContext.RequestServices;
        var contentRepository = services.GetInstance<IContentRepository>();
        var urlResolver = services.GetInstance<IUrlResolver>();
        var webHostEnvironment = services.GetInstance<IWebHostEnvironment>();

        if (!string.IsNullOrEmpty(size))
        {
            var sizeDim = size.Split('x');

            return new HtmlString(GetUrl(contentRef, sizeDim, urlResolver, webHostEnvironment));
        }

        return new HtmlString(urlResolver.GetUrl(contentRef));
    }

    private static string GetUrl(ContentReference imageRef, string[] resolutionDim, IUrlResolver urlResolver, IWebHostEnvironment webHostEnvironment)
    {
        if (ContentReference.IsNullOrEmpty(imageRef))
            return string.Empty;

        if (!webHostEnvironment.IsDevelopment())
            return string.Format(CloudFlareCdnPattern, resolutionDim[0], 85, urlResolver.GetUrl(imageRef));

        return urlResolver.GetUrl(imageRef);
    }

    #endregion
}
