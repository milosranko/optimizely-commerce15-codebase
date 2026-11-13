namespace Commerce.Demo.Business.Features.Search.Models;

public record FacetModel(string Name, IEnumerable<FacetValue> Values);
public record FacetValue(string Text, string Value, bool Selected);
