namespace Commerce.Demo.Integrations.PaymentProviders.PayPal.Models;

public sealed record PayPalPayerInfo
{
    public string? PayerId { get; init; }
    public string? Email { get; init; }
    public string? PayerName { get; init; }
    public PayPalAddress? Address { get; init; }
}