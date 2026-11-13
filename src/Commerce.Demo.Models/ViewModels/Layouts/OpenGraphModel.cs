namespace Commerce.Demo.Web.Models.Layouts;

public record OpenGraphModel
{
    public string? PageUrl { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
}