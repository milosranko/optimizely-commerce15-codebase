namespace Commerce.Demo.Business.Features.CookieConsentAudit.Models;

public record RecordConsentRequest
{
    public required string Action { get; init; }
    public required string CategoriesSelected { get; init; }
    public string? CookieName { get; init; }
    public string? PolicyVersion { get; init; }
}
