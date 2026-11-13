using Commerce.Demo.Business.Features.BackgroundJobs.Interfaces;
using Commerce.Demo.Business.Features.BackgroundJobs.Models;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Commerce.Demo.Business.Features.BackgroundJobs.Jobs;

[Queue("orders")]
[AutomaticRetry(Attempts = 5)]
public class SendOrderJob : IBackgroundJob<SendOrderPayload>
{
    private readonly ILogger<SendOrderJob> _logger;

    public SendOrderJob(ILogger<SendOrderJob> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(SendOrderPayload payload, CancellationToken ct)
    {
        _logger.LogInformation("Processing order {OrderNumber} (GroupId: {OrderGroupId})",
            payload.OrderNumber, payload.OrderGroupId);

        try
        {
            // TODO: Load order from Mediachase.Commerce, transform, and send to external system
            // var orderRepository = _orderRepository.Load<IPurchaseOrder>(payload.OrderGroupId);

            await Task.CompletedTask;

            _logger.LogInformation("Successfully processed order {OrderNumber}", payload.OrderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process order {OrderNumber} (GroupId: {OrderGroupId})",
                payload.OrderNumber, payload.OrderGroupId);
            throw; // Re-throw so Hangfire retries
        }
    }
}
