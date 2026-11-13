using EPiServer.DataAnnotations;

namespace Commerce.Demo.Optimizely.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class IconTreeContentTypeAttribute : ContentTypeAttribute
{
    public string? TreeIcon { get; set; }
}
