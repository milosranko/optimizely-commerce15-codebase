using EPiServer.Core;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Interfaces;

public interface ICommonProperties : IContent
{
    string Heading { get; set; }
    XhtmlString IntroText { get; set; }
}
