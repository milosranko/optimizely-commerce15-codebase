namespace Commerce.Demo.Integrations.PaymentProviders.PayPal.Models;

public sealed record PayPalAppliedDiscountLine
{
    public string Name { get; init; } = "";
    public decimal Amount { get; init; }
}