namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;

public interface ISeoMeta
{
    string? MetaTitle { get; set; }
    bool MetaNoRobots { get; set; }
    string? MetaKeywords { get; set; }
    string? MetaDescription { get; set; }
}
