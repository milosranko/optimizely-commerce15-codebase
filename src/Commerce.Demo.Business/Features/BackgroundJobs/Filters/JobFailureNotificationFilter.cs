using Commerce.Demo.Business.Features.BackgroundJobs.Options;
using Commerce.Demo.Business.Infrastructure.Email;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Commerce.Demo.Business.Features.BackgroundJobs.Filters;

public class JobFailureNotificationFilter : IElectStateFilter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobFailureNotificationFilter> _logger;

    public JobFailureNotificationFilter(IServiceProvider serviceProvider, ILogger<JobFailureNotificationFilter> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is not FailedState failedState)
            return;

        // Only notify on permanent failure (all retries exhausted)
        var retryCount = context.GetJobParameter<int>("RetryCount");
        if (retryCount < GetMaxRetryAttempts(context))
            return;

        _logger.LogError(failedState.Exception,
            "Background job {JobId} ({JobType}) has permanently failed after all retries",
            context.BackgroundJob.Id,
            context.BackgroundJob.Job?.Type.Name);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<HangfireOptions>>().Value;

            if (!options.FailureNotification.Enabled || options.FailureNotification.Recipients.Length == 0)
                return;

            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var jobType = context.BackgroundJob.Job?.Type.Name ?? "Unknown";
            var jobMethod = context.BackgroundJob.Job?.Method.Name ?? "Unknown";

            var subject = $"[Background Job Failed] {jobType}.{jobMethod}";
            var htmlBody = $"""
                <h2>Background Job Permanent Failure</h2>
                <p><strong>Job ID:</strong> {context.BackgroundJob.Id}</p>
                <p><strong>Job Type:</strong> {jobType}.{jobMethod}</p>
                <p><strong>Created At:</strong> {context.BackgroundJob.CreatedAt:u}</p>
                <p><strong>Error:</strong></p>
                <pre>{failedState.Exception?.Message}</pre>
                <p><strong>Stack Trace:</strong></p>
                <pre>{failedState.Exception?.StackTrace}</pre>
                """;

            emailService.SendAsync(
                options.FailureNotification.Recipients,
                subject,
                htmlBody).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send failure notification for job {JobId}", context.BackgroundJob.Id);
        }
    }

    private static int GetMaxRetryAttempts(ElectStateContext context)
    {
        var autoRetryAttr = context.BackgroundJob.Job?.Type
            .GetMethod(context.BackgroundJob.Job.Method.Name)?
            .GetCustomAttributes(typeof(Hangfire.AutomaticRetryAttribute), true)
            .OfType<Hangfire.AutomaticRetryAttribute>()
            .FirstOrDefault();

        return autoRetryAttr?.Attempts ?? 10; // Hangfire default is 10
    }
}
