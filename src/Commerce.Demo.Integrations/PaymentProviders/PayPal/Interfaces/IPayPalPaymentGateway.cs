using Commerce.Demo.Integrations.Abstractions.PayPal.Results;
using Commerce.Demo.Integrations.PaymentProviders.PayPal.Models;

namespace Commerce.Demo.Integrations.PaymentProviders.PayPal.Interfaces;

public interface IPayPalPaymentGateway
{
    Task<PayPalOperationResult<CreateOrderResult>> CreateOrderAsync(CreateOrderRequest request);
    Task<PayPalOperationResult<CaptureOrderResult>> CaptureOrderAsync(string orderId);
    Task<PayPalOperationResult<PayPalOrderDetails>> GetOrderDetailsAsync(string orderId);
}