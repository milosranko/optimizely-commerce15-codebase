using Commerce.Demo.Business.Features.BackgroundJobs.Jobs;
using Commerce.Demo.Business.Features.BackgroundJobs.Models;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using Hangfire;
using Mediachase.Commerce.Orders;
using Microsoft.Extensions.Logging;

namespace Commerce.Demo.Web.Infrastructure;

[InitializableModule]
[ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
public class OrderEventInitialization : IInitializableModule
{
    private ILogger<OrderEventInitialization>? _logger;
    private bool _eventsAttached;

    public void Initialize(InitializationEngine context)
    {
        if (_eventsAttached) return;

        _logger = context.Services.GetRequiredService<ILogger<OrderEventInitialization>>();

        OrderContext.Current.OrderGroupUpdated += OnOrderGroupUpdated;
        _eventsAttached = true;

        _logger.LogInformation("Order event initialization completed — listening for order events");
    }

    private void OnOrderGroupUpdated(object? sender, OrderGroupEventArgs e)
    {
        try
        {
            var orderGroup = sender as OrderGroup;
            if (orderGroup is not PurchaseOrder purchaseOrder)
                return;

            var orderNumber = purchaseOrder.TrackingNumber;
            var orderGroupId = purchaseOrder.OrderGroupId;

            _logger?.LogInformation("Order {OrderNumber} updated — enqueuing SendOrderJob", orderNumber);

            var payload = new SendOrderPayload(orderNumber, orderGroupId);
            BackgroundJob.Enqueue<SendOrderJob>(job => job.ExecuteAsync(payload, CancellationToken.None));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to enqueue SendOrderJob for order event");
        }
    }

    public void Uninitialize(InitializationEngine context)
    {
        if (!_eventsAttached) return;

        OrderContext.Current.OrderGroupUpdated -= OnOrderGroupUpdated;
        _eventsAttached = false;
    }
}
