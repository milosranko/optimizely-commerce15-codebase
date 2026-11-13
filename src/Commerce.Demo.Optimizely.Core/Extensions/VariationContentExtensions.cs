using EPiServer.Commerce.Catalog.ContentTypes;

namespace Commerce.Demo.Optimizely.Core.Extensions;

public static class VariationContentExtensions
{
    public static string? GetProductId(this VariationContent variant)
    {
        if (variant is null)
            return string.Empty;

        return variant.Code.Split('-').LastOrDefault();
    }
}
