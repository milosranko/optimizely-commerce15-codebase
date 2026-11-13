using Commerce.Demo.Business.Features.CookieConsentAudit.Interfaces;
using Commerce.Demo.Business.Features.CookieConsentAudit.Options;
using Commerce.Demo.Web.Models.Admin;
using EPiServer.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text;

namespace Commerce.Demo.Web.Controllers;

[Authorize(Roles = $"{Roles.Administrators},{Roles.CmsAdmins},{Roles.WebAdmins}")]
[Route("cms/admin/[controller]")]
public class CookieConsentAuditController : Controller
{
    private readonly ICookieConsentAuditService _auditService;
    private readonly CookieConsentAuditOptions _options;
    private readonly ILogger<CookieConsentAuditController> _logger;

    public CookieConsentAuditController(
        ICookieConsentAuditService auditService,
        IOptions<CookieConsentAuditOptions> options,
        ILogger<CookieConsentAuditController> logger)
    {
        _auditService = auditService;
        _options = options.Value;
        _logger = logger;
    }

    [Route("")]
    [Route("[action]")]
    public async Task<IActionResult> Index(
        int page = 1,
        DateTime? from = null,
        DateTime? to = null,
        string? visitorId = null,
        string? visitorAction = null)
    {
        var pageSize = _options.DefaultPageSize;
        var queryResult = await _auditService.GetAuditEntries(page, pageSize, from, to, visitorId, visitorAction);
        var statistics = await _auditService.GetStatistics(from, to);
        var viewModel = new CookieConsentAuditViewModel
        {
            QueryResult = queryResult,
            Statistics = statistics,
            FromDate = from,
            ToDate = to,
            VisitorId = visitorId,
            Action = visitorAction,
            Page = page,
            PageSize = pageSize
        };

        return View(viewModel);
    }

    [HttpGet("[action]")]
    public async Task<IActionResult> Export(DateTime? from = null, DateTime? to = null)
    {
        var maxRows = _options.ExportMaxRows;
        var result = await _auditService.GetAuditEntries(1, maxRows, from, to);

        if (result.TotalCount > maxRows)
            _logger.LogWarning(
                "Cookie consent audit export truncated to {Returned} of {Total} matching entries (cap {Cap}).",
                maxRows, result.TotalCount, maxRows);

        var csv = new StringBuilder();

        csv.AppendLine("Timestamp,Action,Categories,VisitorId,Authenticated,PolicyVersion,IpAddress,UserAgent");

        foreach (var item in result.Items)
        {
            csv.AppendLine(string.Join(",",
                EscapeCsv(item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsv(item.Action),
                EscapeCsv(item.CategoriesSelected),
                EscapeCsv(item.VisitorId ?? ""),
                item.IsAuthenticated.ToString(),
                EscapeCsv(item.PolicyVersion ?? ""),
                EscapeCsv(item.IpAddress ?? ""),
                EscapeCsv(item.UserAgent ?? "")));
        }

        var fileName = $"cookie-consent-audit-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
    }

    [HttpPost("[action]")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Purge(int? retentionDays = null)
    {
        var days = retentionDays ?? _options.RetentionDays;
        var purgedCount = await _auditService.PurgeExpiredEntries(days);
        _logger.LogInformation("Manually purged {Count} audit entries older than {Days} days", purgedCount, days);

        TempData["PurgeMessage"] = $"Successfully purged {purgedCount} entries older than {days} days.";
        return RedirectToAction(nameof(Index));
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Neutralize spreadsheet formula injection: prefix cells that begin with a formula trigger character.
        if ("=+-@\t\r".IndexOf(value[0]) >= 0)
            value = "'" + value;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}
