namespace Commerce.Demo.Integrations.PaymentProviders.PayPal.Models;

public sealed record PayPalError
{
    public string ErrorCode { get; init; } = "";
    public string ShortMessage { get; init; } = "";
    public string LongMessage { get; init; } = "";
    public bool IsRetryable { get; init; }
}