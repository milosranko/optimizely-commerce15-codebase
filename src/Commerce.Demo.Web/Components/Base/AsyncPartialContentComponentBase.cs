using EPiServer.Web.Mvc;

namespace Commerce.Demo.Web.Components.Base;

public abstract class AsyncPartialContentComponentBase<TContentData> : AsyncPartialContentComponent<TContentData>
    where TContentData : IContentData
{ }
