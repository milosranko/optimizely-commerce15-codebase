using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Enums;
using Commerce.Demo.Web.Models;

namespace Commerce.Demo.Models.ViewModels;

public record SnippetBlockViewModel
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public SnippetType Type { get; init; }
    public SnippetUsage Usage { get; init; }
    public SnippetArea Placement { get; init; }
    public string? Content { get; init; }
    public bool RequiresVisitorConsent { get; init; } = false;
    public bool EnableInEditMode { get; init; } = false;
    public IEnumerable<CookieItemBlockViewModel>? Cookies { get; init; }
}
