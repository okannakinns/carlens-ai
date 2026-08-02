using System.Net;
using Carlens.Web.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Carlens.Tests;

public sealed class AnalysisRateLimitFilterTests
{
    private static readonly AnalysisRateLimitOptions Options = new(
        PermitLimit: 5,
        Window: TimeSpan.FromMinutes(15),
        RedisKeyPrefix: "carlens:web:tests:rate-limit");

    [Fact]
    public async Task OnResourceExecutionAsync_WhenAllowed_ContinuesToAction()
    {
        var limiter = new StubAnalysisRateLimiter(
            new AnalysisRateLimitDecision(true, TimeSpan.Zero));
        var filter = CreateFilter(limiter);
        var (context, next) = CreateContext(
            IPAddress.Parse("::ffff:203.0.113.10"));

        await filter.OnResourceExecutionAsync(context, next.Invoke);

        Assert.True(next.WasCalled);
        Assert.Null(context.Result);
        Assert.Equal("203.0.113.10", limiter.ClientIdentifier);
    }

    [Fact]
    public async Task OnResourceExecutionAsync_WhenLimitIsExceeded_ReturnsTooManyRequests()
    {
        var limiter = new StubAnalysisRateLimiter(
            new AnalysisRateLimitDecision(
                false,
                TimeSpan.FromSeconds(61)));
        var filter = CreateFilter(limiter);
        var (context, next) = CreateContext(IPAddress.Loopback);

        await filter.OnResourceExecutionAsync(context, next.Invoke);

        var result = Assert.IsType<ObjectResult>(context.Result);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.False(next.WasCalled);
        Assert.Equal(StatusCodes.Status429TooManyRequests, result.StatusCode);
        Assert.Equal(StatusCodes.Status429TooManyRequests, problem.Status);
        Assert.Equal("61", context.HttpContext.Response.Headers.RetryAfter);
    }

    [Fact]
    public async Task OnResourceExecutionAsync_WhenStoreIsUnavailable_FailsClosed()
    {
        var limiter = new StubAnalysisRateLimiter(
            new AnalysisRateLimitUnavailableException(
                "Redis is unavailable.",
                new InvalidOperationException("Connection failed.")));
        var filter = CreateFilter(limiter);
        var (context, next) = CreateContext(IPAddress.Loopback);

        await filter.OnResourceExecutionAsync(context, next.Invoke);

        var result = Assert.IsType<ObjectResult>(context.Result);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.False(next.WasCalled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, problem.Status);
        Assert.Equal("30", context.HttpContext.Response.Headers.RetryAfter);
    }

    private static AnalysisRateLimitFilter CreateFilter(
        IAnalysisRateLimiter limiter)
    {
        return new AnalysisRateLimitFilter(
            limiter,
            Options,
            NullLogger<AnalysisRateLimitFilter>.Instance);
    }

    private static (
        ResourceExecutingContext Context,
        TestResourceExecutionDelegate Next) CreateContext(
            IPAddress remoteIpAddress)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = remoteIpAddress;

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());
        var context = new ResourceExecutingContext(
            actionContext,
            [],
            []);

        return (context, new TestResourceExecutionDelegate(actionContext));
    }

    private sealed class StubAnalysisRateLimiter : IAnalysisRateLimiter
    {
        private readonly AnalysisRateLimitDecision _decision;
        private readonly Exception? _exception;

        public StubAnalysisRateLimiter(AnalysisRateLimitDecision decision)
        {
            _decision = decision;
        }

        public StubAnalysisRateLimiter(Exception exception)
        {
            _exception = exception;
        }

        public string? ClientIdentifier { get; private set; }

        public ValueTask<AnalysisRateLimitDecision> AcquireAsync(
            string clientIdentifier,
            CancellationToken cancellationToken = default)
        {
            ClientIdentifier = clientIdentifier;

            return _exception is null
                ? ValueTask.FromResult(_decision)
                : ValueTask.FromException<AnalysisRateLimitDecision>(_exception);
        }
    }

    private sealed class TestResourceExecutionDelegate(
        ActionContext actionContext)
    {
        public bool WasCalled { get; private set; }

        public Task<ResourceExecutedContext> Invoke()
        {
            WasCalled = true;
            return Task.FromResult(
                new ResourceExecutedContext(actionContext, []));
        }
    }
}
