using EPiServer.Core;

namespace Commerce.Demo.Optimizely.ContentTypes.Commerce.Variants.Interfaces;

public interface IAddon : IContentData
{
    IList<ContentReference>? AddonRelations { get; set; }
    IList<ContentReference>? DependentAddons { get; set; }
    IList<ContentReference>? AddonConflicts { get; set; }
    string? AddonGroupName { get; set; }
    XhtmlString? AddOnDescription { get; set; }
}