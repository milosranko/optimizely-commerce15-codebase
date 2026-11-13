namespace Commerce.Demo.Business.Features.BackgroundJobs.Interfaces;

/// <summary>
/// Marker interface for all background jobs.
/// </summary>
public interface IBackgroundJob;

/// <summary>
/// Background job that processes a typed payload.
/// </summary>
public interface IBackgroundJob<in TPayload> : IBackgroundJob
{
    Task ExecuteAsync(TPayload payload, CancellationToken ct);
}
