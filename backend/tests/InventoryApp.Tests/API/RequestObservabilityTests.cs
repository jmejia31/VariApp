using InventoryApp.API.Middleware;
using InventoryApp.API.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace InventoryApp.Tests.API;

public class RequestObservabilityTests
{
    [Fact]
    public void Record_TracksRequestsErrorsAndSlowSignals()
    {
        using var observability = new RequestObservability();

        observability.Record("GET", StatusCodes.Status200OK, 12.5, isSlow: false);
        observability.Record("POST", StatusCodes.Status503ServiceUnavailable, 2100, isSlow: true);

        var snapshot = observability.Snapshot();
        Assert.Equal(2, snapshot.RequestCount);
        Assert.Equal(1, snapshot.ErrorCount);
        Assert.Equal(1, snapshot.SlowRequestCount);
    }

    [Theory]
    [InlineData("GET", "GET")]
    [InlineData("post", "POST")]
    [InlineData("CUSTOM-UNBOUNDED", "OTHER")]
    [InlineData("", "OTHER")]
    public void NormalizeMethod_BoundsMetricCardinality(string method, string expected)
    {
        Assert.Equal(expected, RequestObservability.NormalizeMethod(method));
    }

    [Fact]
    public async Task Middleware_RecordsServerErrorWithoutExternalExporter()
    {
        using var observability = new RequestObservability();
        RequestDelegate next = context =>
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return Task.CompletedTask;
        };
        var middleware = new RequestObservabilityMiddleware(
            next,
            observability,
            Options.Create(new ObservabilityOptions
            {
                SlowRequestThresholdMs = 60_000,
                ErrorAlertStatusCode = 500,
                EnableAlertLogs = false
            }),
            NullLogger<RequestObservabilityMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;

        await middleware.InvokeAsync(context);

        var snapshot = observability.Snapshot();
        Assert.Equal(1, snapshot.RequestCount);
        Assert.Equal(1, snapshot.ErrorCount);
        Assert.Equal(0, snapshot.SlowRequestCount);
    }
}
