using Carlens.Api.Middlewares;
using Carlens.Api.Security;
using Carlens.Contracts.Security;
using Microsoft.AspNetCore.Http;

namespace Carlens.Tests;

public sealed class InternalApiKeyMiddlewareTests
{
    private const string ValidKey =
        "carlens-test-internal-key-with-more-than-32-characters";

    [Fact]
    public async Task InvokeAsync_WhenKeyIsMissing_ReturnsUnauthorized()
    {
        var nextCalled = false;
        var middleware = new InternalApiKeyMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(
            context,
            new InternalApiSecurityOptions(ValidKey));

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenKeyMatches_ContinuesPipeline()
    {
        var nextCalled = false;
        var middleware = new InternalApiKeyMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Headers[InternalApiHeaders.ApiKey] = ValidKey;

        await middleware.InvokeAsync(
            context,
            new InternalApiSecurityOptions(ValidKey));

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenProtectionIsDisabled_ContinuesPipeline()
    {
        var nextCalled = false;
        var middleware = new InternalApiKeyMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(
            new DefaultHttpContext(),
            new InternalApiSecurityOptions(null));

        Assert.True(nextCalled);
    }
}
