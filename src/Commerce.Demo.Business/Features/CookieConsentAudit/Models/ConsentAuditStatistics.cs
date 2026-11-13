namespace Commerce.Demo.Business.Features.CookieConsentAudit.Models;

public record ConsentAuditStatistics
{
    public int TotalEntries { get; init; }
    public int AcceptedCount { get; init; }
    public int DeniedCount { get; init; }
    public int ModifiedCount { get; init; }
    public DateTime? OldestEntry { get; init; }
    public DateTime? NewestEntry { get; init; }
}
