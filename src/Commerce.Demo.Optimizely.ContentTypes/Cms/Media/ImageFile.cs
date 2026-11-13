using Commerce.Demo.Optimizely.ContentTypes.Cms.Media.Base;
using EPiServer.DataAnnotations;
using EPiServer.Framework.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Media;

[ContentType(
    GUID = "{7E8390E0-8415-4BF8-B91B-1B2517406EA9}",
    DisplayName = "Image File")]
[MediaDescriptor(ExtensionString = "jpg,jpeg,jpe,ico,gif,bmp,png")]
public class ImageFile : ImageFileBase
{ }
