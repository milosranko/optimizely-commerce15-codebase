namespace Commerce.Demo.Integrations.Abstractions.PayPal.Results;

public enum PayPalCaptureStatus
{
    Unknown = 0,
    Completed,
    Declined,
    Pending,
    Failed
}