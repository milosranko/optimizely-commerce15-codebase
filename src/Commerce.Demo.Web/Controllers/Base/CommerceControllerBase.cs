using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Web.Mvc;

namespace Commerce.Demo.Web.Controllers.Base;

public class CommerceControllerBase<T> : ContentController<T> where T : CatalogContentBase
{
    public T? CatalogContent { get; set; }
}
