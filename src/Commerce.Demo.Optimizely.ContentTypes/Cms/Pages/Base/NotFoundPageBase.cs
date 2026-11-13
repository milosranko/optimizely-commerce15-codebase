using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Base;

[AvailableContentTypes(Availability.None)]
public abstract class NotFoundPageBase : PageBasePublic
{
    #region Public properties

    public override void SetDefaultValues(ContentType contentType)
    {
        base.SetDefaultValues(contentType);

        VisibleInMenu = false;
    }

    #endregion
}
