using EPiServer.DataAnnotations;
using Geta.Optimizely.Categories;

namespace Commerce.Demo.Optimizely.ContentTypes.Shared.Categories;

[ContentType(
    DisplayName = "Default Category",
    AvailableInEditMode = true,
    GUID = "{69401BFC-8031-49FB-87A1-AC4921E06A65}")]
public class DefaultCategory : CategoryData
{ }
