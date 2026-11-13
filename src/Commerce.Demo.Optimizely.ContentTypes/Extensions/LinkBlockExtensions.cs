using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Local;

namespace Commerce.Demo.Optimizely.ContentTypes.Extensions;

public static class LinkBlockExtensions
{
    public static bool IsNullOrEmpty(this LinkBlock block)
    {
        return (block == null) || (block != null && (string.IsNullOrEmpty(block.LinkText) || block.LinkUrl == null));
    }
}
