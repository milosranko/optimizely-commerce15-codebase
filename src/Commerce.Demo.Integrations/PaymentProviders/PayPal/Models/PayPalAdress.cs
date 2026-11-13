namespace Commerce.Demo.Integrations.PaymentProviders.PayPal.Models;

public sealed record PayPalAddress
{
    public string? Name { get; init; }
    public string? Street1 { get; init; }
    public string? Street2 { get; init; }
    public string? City { get; init; }
    public string? StateOrProvince { get; init; }
    public string? PostalCode { get; init; }
    public string? CountryCode { get; init; }
    public string? Phone { get; init; }
}