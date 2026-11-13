using EPiServer.DataAnnotations;

namespace Commerce.Demo.Optimizely.Core.Attributes;

public class SiteImageUrlAttribute : ImageUrlAttribute
{
    public SiteImageUrlAttribute(string path) : base("~/img/thumbs/" + path)
    { }
}
