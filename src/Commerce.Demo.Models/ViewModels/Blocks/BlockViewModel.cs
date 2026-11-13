using Commerce.Demo.Optimizely.ContentTypes.Cms.Blocks.Base;
using Commerce.Demo.Optimizely.Core.Helpers;
using System.Text.Json.Serialization;

namespace Commerce.Demo.Web.Models.Blocks;

public record BlockViewModel<TBlock> where TBlock : BlockBase
{
    public BlockViewModel(TBlock currentContent)
    {
        CurrentBlock = currentContent;
    }

    [JsonIgnore]
    public TBlock CurrentBlock { get; init; }
    public bool IsEditing => PageHelpers.IsInEditMode();
}
