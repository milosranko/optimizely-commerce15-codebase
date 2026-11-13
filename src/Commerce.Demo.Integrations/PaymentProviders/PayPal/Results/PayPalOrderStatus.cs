namespace Commerce.Demo.Integrations.Abstractions.PayPal.Results;

public enum PayPalOrderStatus
{
    Unknown = 0,
    Created,
    Approved,
    Captured,
    Failed
}