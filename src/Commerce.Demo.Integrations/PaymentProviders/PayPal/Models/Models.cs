using Commerce.Demo.Integrations.Abstractions.PayPal.Results;

namespace Commerce.Demo.Integrations.PaymentProviders.PayPal.Models;

public sealed record CreateOrderRequest
{
    public string CurrencyCode { get; set; } = "EUR";
    // Existing minimal requirement
    public decimal Total { get; set; }
    public string BrandName { get; set; } = "YourBrand";
    // New: legacy parity (optional)
    // Mirrors legacy PaymentDetails and AppliedDiscountLine concepts
    public decimal? ItemTotal { get; set; }
    public decimal? ShippingTotal { get; set; }
    public decimal? HandlingTotal { get; set; }
    public decimal? TaxTotal { get; set; }
    public List<PayPalAppliedDiscountLine>? AppliedDiscountLines { get; set; }
    // New: itemization (legacy added cart items to PayPal request)
    public List<PayPalOrderItem>? Items { get; set; }
}

public sealed record CreateOrderResult
{
    public string OrderId { get; set; } = "";
    public string ApprovalUrl { get; set; } = "";
}

public sealed record CaptureOrderResult
{
    public string Status { get; set; } = "";
    public PayPalCaptureStatus CaptureStatus { get; set; } = PayPalCaptureStatus.Unknown;
    public string? CaptureId { get; set; }
}
