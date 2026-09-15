using System.Diagnostics;
using InventoryApp.API.Observability;
using Microsoft.Extensions.Options;

namespace InventoryApp.API.Middleware;

public sealed class RequestObservabilityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestObservability _observability;
    private readonly ILogger<RequestObservabilityMiddleware> _logger;
    private readonly ObservabilityOptions _options;

    public RequestObservabilityMiddleware(
        RequestDelegate next,
        RequestObservability observability,
        IOptions<ObservabilityOptions> options,
        ILogger<RequestObservabilityMiddleware> logger)
    {
        _next = next;
        _observability = observability;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var activity = Activity.Current;
        activity?.SetTag("service.name", "InventoryApp.API");

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var thresholdMs = Math.Clamp(_options.SlowRequestThresholdMs, 100, 60_000);
            var errorAlertStatusCode = Math.Clamp(_options.ErrorAlertStatusCode, 500, 599);
            var isSlow = stopwatch.Elapsed.TotalMilliseconds >= thresholdMs;
            var endpoint = context.GetEndpoint()?.DisplayName ?? "unmatched";

            _observability.Record(context.Request.Method, statusCode, stopwatch.Elapsed.TotalMilliseconds, isSlow);

            activity?.SetTag("http.response.status_code", statusCode);
            activity?.SetTag("app.request.slow", isSlow);

            if (!_options.EnableAlertLogs)
            {
                return;
            }

            if (statusCode >= errorAlertStatusCode)
            {
                _logger.LogError(
                    "ObservabilityAlert RequestError Method={Method} Endpoint={Endpoint} StatusCode={StatusCode} DurationMs={DurationMs:F1}",
                    RequestObservability.NormalizeMethod(context.Request.Method), endpoint, statusCode, stopwatch.Elapsed.TotalMilliseconds);
            }
            else if (isSlow)
            {
                _logger.LogWarning(
                    "ObservabilityAlert SlowRequest Method={Method} Endpoint={Endpoint} StatusCode={StatusCode} DurationMs={DurationMs:F1} ThresholdMs={ThresholdMs}",
                    RequestObservability.NormalizeMethod(context.Request.Method), endpoint, statusCode, stopwatch.Elapsed.TotalMilliseconds, thresholdMs);
            }
        }
    }
}
