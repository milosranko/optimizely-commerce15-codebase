namespace Commerce.Demo.Business.Features.CookieConsentAudit.Models;

public record ConsentAuditQueryResult
{
    public IEnumerable<ConsentAuditItem> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public record ConsentAuditItem
{
    public string Id { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Action { get; init; } = string.Empty;
    public string CategoriesSelected { get; init; } = string.Empty;
    public string? VisitorId { get; init; }
    public bool IsAuthenticated { get; init; }
    public string? PolicyVersion { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
