using EPiServer.Core;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Media.Base;

public abstract class ImageFileBase : ImageData
{
    public virtual string? Copyright { get; set; }
    public virtual string? AlternateText { get; set; }
}
