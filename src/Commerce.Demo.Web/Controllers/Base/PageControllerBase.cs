using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;
using EPiServer.Web.Mvc;

namespace Commerce.Demo.Web.Controllers.Base;

public abstract class PageControllerBase<T> : PageController<T> where T : PageBase
{ }
