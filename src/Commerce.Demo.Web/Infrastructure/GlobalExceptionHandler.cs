using Hangfire;
using Microsoft.AspNetCore.Diagnostics;
using System.Collections.Concurrent;
using System.Net;

namespace Commerce.Demo.Web.Infrastructure;

/// <summary>
/// Observes unhandled request exceptions: logs them, enqueues an out-of-band email alert
/// (throttled per exception type), and returns a structured JSON error for API callers.
/// Browser requests fall through to <c>UseExceptionHandler</c>'s re-execute to <c>/500.html</c>.
/// Registered as a singleton via <c>AddExceptionHandler</c>, so throttle state is shared across requests.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private static readonly TimeSpan ThrottleWindow = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, DateTime> _lastAlertUtc = new();
    private readonly IBackgroundJobClient _jobClient;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IBackgroundJobClient jobClient, ILogger<GlobalExceptionHandler> logger)
    {
        _jobClient = jobClient;
        _logger = logger;
    }

    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        if (ShouldAlert(exception))
            EnqueueAlert(httpContext, exception);

        // API callers get a structured JSON error and we own the response.
        if (httpContext.Request.Path.StartsWithSegments("/api"))
            return WriteApiResponseAsync(httpContext, exception, cancellationToken);

        // Browser requests: let UseExceptionHandler re-execute to the static /500.html page.
        return ValueTask.FromResult(false);
    }

    private void EnqueueAlert(HttpContext httpContext, Exception exception)
    {
        //try
        //{
        //    var payload = new ExceptionAlertPayload(
        //        exception.GetType().FullName ?? exception.GetType().Name,
        //        exception.Message,
        //        exception.StackTrace,
        //        $"{httpContext.Request.Method} {httpContext.Request.Path}{httpContext.Request.QueryString}",
        //        httpContext.TraceIdentifier);

        //    _jobClient.Enqueue<ExceptionAlertJob>(job => job.ExecuteAsync(payload, CancellationToken.None));
        //}
        //catch (Exception ex)
        //{
        //    // Alerting must never mask the original failure.
        //    _logger.LogError(ex, "Failed to enqueue unhandled-exception alert email.");
        //}
    }

    private bool ShouldAlert(Exception exception)
    {
        // Client disconnects / request cancellations aren't actionable failures.
        if (exception is OperationCanceledException)
            return false;

        // Throttle per exception type so a hot failure can't trigger an email storm.
        var key = exception.GetType().FullName ?? exception.GetType().Name;
        var now = DateTime.UtcNow;
        var last = _lastAlertUtc.GetOrAdd(key, DateTime.MinValue);
        if (now - last < ThrottleWindow)
            return false;

        _lastAlertUtc[key] = now;
        return true;
    }

    private static async ValueTask<bool> WriteApiResponseAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var code = exception switch
        {
            KeyNotFoundException => HttpStatusCode.NotFound,
            UnauthorizedAccessException => HttpStatusCode.Forbidden,
            ArgumentException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError,
        };

        // Don't leak internal details for server faults; surface the message only for client errors.
        var message = code == HttpStatusCode.InternalServerError
            ? "An unexpected error occurred."
            : exception.Message;

        httpContext.Response.StatusCode = (int)code;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(new { Code = (int)code, Message = message }, ct);
        return true;
    }
}
