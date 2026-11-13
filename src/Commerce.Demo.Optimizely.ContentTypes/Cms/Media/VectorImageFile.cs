using Commerce.Demo.Optimizely.ContentTypes.Cms.Media.Base;
using EPiServer.DataAnnotations;
using EPiServer.Framework.Blobs;
using EPiServer.Framework.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Media;

[ContentType(
    GUID = "{42A72199-0846-4405-B3BE-A929B7DCDE97}",
    DisplayName = "Vector Image File")]
[MediaDescriptor(ExtensionString = "svg")]
public class VectorImageFile : ImageFileBase
{
    public override Blob? Thumbnail => BinaryData;
}
