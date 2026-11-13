namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;

public interface ISnippetsWithInheritance : ISnippets
{
    //bool InheritSnippetsFromParent { get; set; }
    bool InheritSnippetsFromSettings { get; set; }
}
