using Commerce.Demo.Business.Features.CookieConsentAudit.Models;

namespace Commerce.Demo.Web.Models.Admin;

public class CookieConsentAuditViewModel
{
    public ConsentAuditQueryResult QueryResult { get; set; } = new();
    public ConsentAuditStatistics Statistics { get; set; } = new();
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? VisitorId { get; set; }
    public string? Action { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling((double)QueryResult.TotalCount / PageSize)
        : 0;
}
