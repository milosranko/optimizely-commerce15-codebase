using Commerce.Demo.Business.Features.CookieConsentAudit.Models;

namespace Commerce.Demo.Business.Features.CookieConsentAudit.Interfaces;

public interface ICookieConsentAuditService
{
    Task RecordConsent(
        RecordConsentRequest request,
        string? visitorId,
        bool isAuthenticated,
        string? ipAddress,
        string? userAgent);

    Task<ConsentAuditQueryResult> GetAuditEntries(
        int page,
        int pageSize,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? visitorId = null,
        string? action = null);

    Task<int> PurgeExpiredEntries(int retentionDays);

    Task<ConsentAuditStatistics> GetStatistics(
        DateTime? fromDate = null,
        DateTime? toDate = null);
}
