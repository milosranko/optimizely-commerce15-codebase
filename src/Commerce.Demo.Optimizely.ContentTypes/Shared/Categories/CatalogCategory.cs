using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace Commerce.Demo.Optimizely.ContentTypes.Shared.Categories;

[ContentType(
    DisplayName = "Catalog Category",
    AvailableInEditMode = true,
    GUID = "{07935682-F5E0-4BF5-B34F-C7B2A6B87D0B}")]
public class CatalogCategory : DefaultCategory
{
    [Display(
        Name = "Sort Order",
        Order = 100)]
    public virtual int? SortOrder { get; set; }
}
