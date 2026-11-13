namespace Commerce.Demo.Business.Features.CookieConsentAudit.Options;

public class CookieConsentAuditOptions
{
    public const string SectionName = "CookieConsentAudit";

    public int RetentionDays { get; set; } = 1095;
    public int DefaultPageSize { get; set; } = 50;
    public bool EnableAuditLogging { get; set; } = true;

    /// <summary>How long the dashboard statistics are cached, in seconds.</summary>
    public int StatisticsCacheSeconds { get; set; } = 120;

    /// <summary>Maximum number of rows included in a single CSV export.</summary>
    public int ExportMaxRows { get; set; } = 50000;
}
