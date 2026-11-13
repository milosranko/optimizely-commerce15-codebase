using Commerce.Demo.Integrations.Abstractions.PayPal.Results;

namespace Commerce.Demo.Integrations.PaymentProviders.PayPal.Models;

public sealed record PayPalOrderDetails
{
    public string OrderId { get; init; } = "";
    public PayPalOrderStatus Status { get; init; } = PayPalOrderStatus.Unknown;
    public PayPalPayerInfo? PayerInfo { get; init; }
    public PayPalAmountBreakdown? Amount { get; init; }
    public List<PayPalPaymentInfo> Payments { get; init; } = new();
}