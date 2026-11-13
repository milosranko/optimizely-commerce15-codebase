using Commerce.Demo.Integrations.PaymentProviders.PayPal.Models;
using System.Text.Json;

namespace Commerce.Demo.Integrations.PaymentProviders.PayPal.Mappings;

public static class ExceptionToPayPalErrorsMapper
{
    public static (List<PayPalError> Errors, string? DebugId, bool IsRetryable) Map(Exception ex)
    {
        var statusCode = TryGetIntProp(ex, "StatusCode", "ResponseCode", "HttpStatusCode");
        var body = TryGetStringProp(ex, "ResponseBody", "Body", "HttpBody", "RawBody", "Content")
                   ?? ex.Message;

        // Default
        var retryable = statusCode is >= 500 or 429;

        if (!string.IsNullOrWhiteSpace(body) && body.TrimStart().StartsWith("{", StringComparison.Ordinal))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                var debugId = TryGetString(root, "debug_id");
                var name = TryGetString(root, "name") ?? "PayPalError";
                var message = TryGetString(root, "message") ?? ex.Message;

                // details[]: { issue, description }
                if (root.TryGetProperty("details", out var detailsEl) && detailsEl.ValueKind == JsonValueKind.Array)
                {
                    var errors = new List<PayPalError>();
                    foreach (var d in detailsEl.EnumerateArray())
                    {
                        var issue = TryGetString(d, "issue") ?? name;
                        var description = TryGetString(d, "description") ?? message;

                        errors.Add(new PayPalError
                        {
                            ErrorCode = issue,
                            ShortMessage = description,
                            LongMessage = body,
                            IsRetryable = retryable
                        });
                    }

                    if (errors.Count > 0)
                        return (errors, debugId, retryable);
                }

                return (
                    new List<PayPalError>
                    {
                        new PayPalError
                        {
                            ErrorCode = name,
                            ShortMessage = message,
                            LongMessage = body,
                            IsRetryable = retryable
                        }
                    },
                    debugId,
                    retryable
                );
            }
            catch
            {
                // Fall through to generic mapping
            }
        }

        return (
            new List<PayPalError>
            {
                new PayPalError
                {
                    ErrorCode = ex.GetType().Name,
                    ShortMessage = ex.Message,
                    LongMessage = ex.ToString(),
                    IsRetryable = retryable
                }
            },
            DebugId: null,
            IsRetryable: retryable
        );
    }

    private static string? TryGetString(JsonElement el, string prop)
    {
        if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String)
            return v.GetString();

        return null;
    }

    private static int? TryGetIntProp(object obj, params string[] names)
    {
        var type = obj.GetType();
        foreach (var name in names)
        {
            var prop = type.GetProperty(name);
            if (prop == null)
                continue;

            var value = prop.GetValue(obj);
            if (value == null)
                continue;

            if (value is int i)
                return i;

            if (value is long l)
                return unchecked((int)l);

            if (int.TryParse(value.ToString(), out var parsed))
                return parsed;
        }

        return null;
    }

    private static string? TryGetStringProp(object obj, params string[] names)
    {
        var type = obj.GetType();
        foreach (var name in names)
        {
            var prop = type.GetProperty(name);
            if (prop == null)
                continue;

            var value = prop.GetValue(obj);
            if (value == null)
                continue;

            return value.ToString();
        }

        return null;
    }
}
