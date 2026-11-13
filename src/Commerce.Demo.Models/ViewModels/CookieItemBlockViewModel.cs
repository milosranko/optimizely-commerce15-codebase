using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Enums;

namespace Commerce.Demo.Web.Models;

public record CookieItemBlockViewModel(string Name, string? Description, SnippetUsage Usage);
