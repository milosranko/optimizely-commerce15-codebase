using Commerce.Demo.Integrations.Abstractions.PayPal.Results;

namespace Commerce.Demo.Integrations.PaymentProviders.PayPal.Models;

public sealed record PayPalPaymentInfo
{
    public string? TransactionId { get; init; }
    public string? PaymentStatus { get; init; }
    public PayPalCaptureStatus CaptureStatus { get; init; } = PayPalCaptureStatus.Unknown;
    // Keep as string initially for minimal SDK coupling
    public string? PaymentDate { get; init; }
    public PayPalError? PaymentError { get; init; }
}