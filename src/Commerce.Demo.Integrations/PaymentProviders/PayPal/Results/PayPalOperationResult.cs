using Commerce.Demo.Integrations.PaymentProviders.PayPal.Models;

namespace Commerce.Demo.Integrations.Abstractions.PayPal.Results;

public sealed record PayPalOperationResult<T>
{
    public bool IsSuccess { get; init; }
    // Legacy ResponseBase.Result was a string ack; console needs a typed status
    public string? RawStatus { get; init; }
    public List<PayPalError> Errors { get; init; } = new();
    // Optional diagnostics (safe to add even if not populated immediately)
    public string? DebugId { get; init; }
    public string? CorrelationId { get; init; }
    public T? Value { get; init; }
}