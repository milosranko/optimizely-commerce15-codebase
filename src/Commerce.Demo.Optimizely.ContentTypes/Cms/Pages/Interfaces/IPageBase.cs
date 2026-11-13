using EPiServer.Core;
using EPiServer.Security;

namespace Commerce.Demo.Optimizely.ContentTypes.Cms.Pages.Interfaces;

public interface ILayoutPageBase : IContent, IContentData, IVersionable, ISecurable, ILocalizable
{ }
