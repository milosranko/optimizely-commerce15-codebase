using Commerce.Demo.Integrations.Abstractions.PayPal.Results;

namespace Commerce.Demo.Integrations.PaymentProviders.PayPal.Mappings;

public static class OrderStatusMapper
{
    public static PayPalOrderStatus Map(string? sdkStatus)
    {
        // Map known PayPal Orders statuses to normalized enum
        return sdkStatus?.ToUpperInvariant() switch
        {
            "CREATED" => PayPalOrderStatus.Created,
            "APPROVED" => PayPalOrderStatus.Approved,
            "COMPLETED" => PayPalOrderStatus.Captured,
            _ => PayPalOrderStatus.Unknown
        };
    }
}