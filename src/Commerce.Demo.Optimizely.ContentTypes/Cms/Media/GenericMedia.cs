using EPiServer.Core;
using EPiServer.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Media;

[ContentType(
    GUID = "{EE3BD195-7CB0-4756-AB5F-E5E223CD9820}",
    DisplayName = "Generic Media")]
public class GenericMedia : MediaData
{
    public virtual string? Description { get; set; }
}
