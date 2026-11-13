namespace Commerce.Demo.Integrations.PaymentProviders.PayPal.Models;

public sealed record PayPalAmountBreakdown
{
    public PayPalMoney OrderTotal { get; init; } = new();
    public PayPalMoney? ItemTotal { get; init; }
    public PayPalMoney? ShippingTotal { get; init; }
    public PayPalMoney? HandlingTotal { get; init; }
    public PayPalMoney? TaxTotal { get; init; }
    public PayPalMoney? DiscountTotal { get; init; }
}