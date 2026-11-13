namespace Commerce.Demo.Business.Features.BackgroundJobs.Options;

public class HangfireOptions
{
    public const string SectionName = "Hangfire";

    public int WorkerCount { get; set; } = 2;
    public string[] Queues { get; set; } = ["default", "orders"];
    public TimeSpan ServerTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public int RetryAttempts { get; set; } = 5;
    public string DashboardPath { get; set; } = "/episerver/hangfire";
    public FailureNotificationOptions FailureNotification { get; set; } = new();
}

public class FailureNotificationOptions
{
    public bool Enabled { get; set; } = true;
    public string[] Recipients { get; set; } = [];
    public string FromAddress { get; set; } = "noreply@domain.com";
    public string FromDisplayName { get; set; } = "Background Jobs";
}
