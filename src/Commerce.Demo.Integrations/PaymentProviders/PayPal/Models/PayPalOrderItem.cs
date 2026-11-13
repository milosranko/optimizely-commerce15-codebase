namespace Commerce.Demo.Integrations.PaymentProviders.PayPal.Models;

public sealed record PayPalOrderItem
{
    public string Name { get; init; } = "";
    public string? Sku { get; init; }
    public int Quantity { get; init; } = 1;
    public decimal UnitAmount { get; init; }
    public string? Description { get; init; }
    public string? Url { get; init; }
    public string Category { get; init; } = "PHYSICAL"; // "PHYSICAL" or "DIGITAL"
}
