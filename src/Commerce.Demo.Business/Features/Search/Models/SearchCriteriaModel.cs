using Commerce.Demo.Business.Features.Search.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Demo.Business.Features.Search.Models;

public record SearchCriteriaModel
{
    [FromQuery(Name = "q")]
    public string Query { get; set; }

    [FromQuery(Name = "p")]
    public int Page { get; set; } = 1;

    [FromQuery(Name = "ps")]
    public int? PageSize { get; set; }

    [FromQuery(Name = "o")]
    public SortType? Order { get; set; }

    [FromQuery(Name = "f")]
    public string[] Facets { get; set; } = [];
}
