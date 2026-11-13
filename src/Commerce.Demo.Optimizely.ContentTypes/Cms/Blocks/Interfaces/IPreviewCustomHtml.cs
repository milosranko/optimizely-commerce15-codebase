namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Interfaces;

public interface IPreviewCustomHtml
{
    string FirstLevelTag { get; }
    string FirstLevelCssClass { get; }
    string SecondLevelTag { get; }
    string SecondLevelCssClass { get; }
}
