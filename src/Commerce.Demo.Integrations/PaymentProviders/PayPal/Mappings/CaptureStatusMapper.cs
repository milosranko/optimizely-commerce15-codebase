using Commerce.Demo.Integrations.Abstractions.PayPal.Results;

namespace Commerce.Demo.Integrations.PaymentProviders.PayPal.Mappings;

public static class CaptureStatusMapper
{
    public static PayPalCaptureStatus Map(string? sdkStatus)
    {
        return sdkStatus?.ToUpperInvariant() switch
        {
            "COMPLETED" => PayPalCaptureStatus.Completed,
            "DECLINED" => PayPalCaptureStatus.Declined,
            "PENDING" => PayPalCaptureStatus.Pending,
            "FAILED" => PayPalCaptureStatus.Failed,
            _ => PayPalCaptureStatus.Unknown
        };
    }
}
