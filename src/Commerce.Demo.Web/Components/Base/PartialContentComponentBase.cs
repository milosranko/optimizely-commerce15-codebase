using EPiServer.Web.Mvc;

namespace Commerce.Demo.Web.Components.Base;

public abstract class PartialContentComponentBase<TContentData> : PartialContentComponent<TContentData>
    where TContentData : IContentData
{ }
