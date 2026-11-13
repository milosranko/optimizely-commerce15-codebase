using Commerce.Demo.Business.Features.CookieConsentAudit.Interfaces;
using Commerce.Demo.Business.Features.CookieConsentAudit.Options;
using EPiServer.Scheduler;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Commerce.Demo.Business.ScheduledJobs;

[ScheduledJob(
    DisplayName = "Cookies Consent Audit Purge",
    Description = "Purges cookies consent audit entries older than the configured retention period",
    // Was the placeholder "A1B2C3D4-E5F6-7890-ABCD-EF1234567890", which EPiServer.Commerce 15 also uses for its
    // webhooks job: duplicate GUIDs make CMS skip the whole scheduled-job sync. Must stay unique.
    GUID = JobId,
    DefaultEnabled = true,
    Restartable = true)]
public class CookieConsentAuditPurgeJob : ScheduledJobBase
{
    public const string JobId = "DDA1D3ED-5B6D-45E2-98BF-F058DF430AAA";

    private readonly ICookieConsentAuditService _auditService;
    private readonly CookieConsentAuditOptions _options;
    private readonly ILogger<CookieConsentAuditPurgeJob> _logger;
    private bool _stopSignaled;

    public CookieConsentAuditPurgeJob(
        ICookieConsentAuditService auditService,
        IOptions<CookieConsentAuditOptions> options,
        ILogger<CookieConsentAuditPurgeJob> logger)
    {
        _auditService = auditService;
        _options = options.Value;
        _logger = logger;
        IsStoppable = true;
    }

    public override string Execute()
    {
        if (_stopSignaled)
            return "Job was stopped before execution.";

        _logger.LogInformation(
            "Starting cookie consent audit purge. Retention: {RetentionDays} days",
            _options.RetentionDays);

        // ScheduledJobBase.Execute is synchronous; the job runs on a scheduler thread with no sync context, so blocking is safe here.
        var purgedCount = _auditService.PurgeExpiredEntries(_options.RetentionDays).GetAwaiter().GetResult();
        var message = $"Purged {purgedCount} audit entries older than {_options.RetentionDays} days.";

        _logger.LogInformation(message);

        return message;
    }

    public override void Stop()
    {
        _stopSignaled = true;
        base.Stop();
    }
}
