using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages;
using Commerce.Demo.Optimizely.Core.Helpers;
using Commerce.Demo.Web.Controllers.Base;
using Commerce.Demo.Web.Models.Pages;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Net;

namespace Commerce.Demo.Web.Controllers;

public class NotFoundPageController : PageControllerBase<NotFoundPage>
{
    private readonly IContentLoader _contentLoader;

    public NotFoundPageController(IContentLoader contentLoader)
    {
        _contentLoader = contentLoader;
    }

    public IActionResult Index(NotFoundPage currentPage)
    {
        var model = PageViewModel.Create(currentPage);

        if (!PageHelpers.IsInEditMode())
        {
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            // get the language from the URL by splitting it on '/' character
            var lang = Request.Path.Value?.Split('/');

            if (lang != null && lang.Length > 2)//if it has language prefix in the URL it will have at least 2 members of the split array. second member is a good candidate for language
            {
                //get all cultures and from that list get the one that matches current language
                var cultureInfo = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(x => x.Name == lang[1]);
                var alternativeErrorPage = cultureInfo != null ? _contentLoader.Get<NotFoundPage>(currentPage.ContentLink, new LanguageSelector(cultureInfo.Name)) : null;
                currentPage = alternativeErrorPage ?? currentPage;//if language was present and 404 page exists in current language continue to display that page else display fallback page
            }
        }

        return View(model);
    }
}
