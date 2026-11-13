namespace Commerce.Demo.Integrations.PaymentProviders.PayPal;

public sealed class PayPalOptions
{
    // "sandbox" or "production"
    public string Environment { get; set; } = "sandbox";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string ReturnUrl { get; set; } = "";
    public string CancelUrl { get; set; } = "";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Environment))
            throw new ArgumentException("PayPal.Environment is required (sandbox/production).");

        if (string.IsNullOrWhiteSpace(ClientId))
            throw new ArgumentException("PayPal.ClientId is required.");

        if (string.IsNullOrWhiteSpace(ClientSecret))
            throw new ArgumentException("PayPal.ClientSecret is required.");

        if (string.IsNullOrWhiteSpace(ReturnUrl))
            throw new ArgumentException("PayPal.ReturnUrl is required.");

        if (string.IsNullOrWhiteSpace(CancelUrl))
            throw new ArgumentException("PayPal.CancelUrl is required.");
    }

    public string ApiBaseUrl =>
        Environment.Equals("production", StringComparison.OrdinalIgnoreCase)
            ? "https://api-m.paypal.com"
            : "https://api-m.sandbox.paypal.com";
}
