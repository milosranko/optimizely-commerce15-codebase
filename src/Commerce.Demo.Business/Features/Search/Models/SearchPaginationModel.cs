namespace Commerce.Demo.Business.Features.Search.Models;

public record SearchPaginationModel(int TotalPages, int PageSize, int CurrentPage, string QueryString);
