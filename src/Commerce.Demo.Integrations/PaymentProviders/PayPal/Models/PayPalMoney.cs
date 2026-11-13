namespace Commerce.Demo.Integrations.PaymentProviders.PayPal.Models;

public sealed record PayPalMoney
{
    public string CurrencyCode { get; init; } = "EUR";
    public decimal Value { get; init; }
}