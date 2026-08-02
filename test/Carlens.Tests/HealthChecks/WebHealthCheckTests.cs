using System.Net;
using Carlens.Web.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Carlens.Tests.HealthChecks;

public sealed class WebHealthCheckTests
{
    [Fact]
    public async Task Live_returns_ok_when_redis_is_unavailable()
    {
        await using var factory = new WebFactory(forceRedisHealthy: false);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ready_returns_service_unavailable_when_redis_is_unavailable()
    {
        await using var factory = new WebFactory(forceRedisHealthy: false);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Ready_returns_ok_when_redis_is_healthy()
    {
        await using var factory = new WebFactory(forceRedisHealthy: true);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed class WebFactory(bool forceRedisHealthy)
        : WebApplicationFactory<AnalysesGatewayController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .UseEnvironment("Production")
                .UseSetting("CarlensApi:BaseUrl", "http://localhost:5200")
                .UseSetting(
                    "Redis:ConnectionString",
                    "127.0.0.1:1,abortConnect=false,connectRetry=0," +
                    "connectTimeout=100,syncTimeout=100,asyncTimeout=100")
                .UseSetting(
                    "Security:InternalApiKey",
                    "carlens-health-check-tests-internal-key");

            if (forceRedisHealthy)
            {
                builder.ConfigureServices(services =>
                {
                    services.PostConfigure<HealthCheckServiceOptions>(options =>
                    {
                        var redisRegistration = options.Registrations.Single(
                            registration => registration.Name == "redis");

                        options.Registrations.Remove(redisRegistration);
                        options.Registrations.Add(
                            new HealthCheckRegistration(
                                "redis",
                                _ => new HealthyHealthCheck(),
                                HealthStatus.Unhealthy,
                                ["ready"]));
                    });
                });
            }
        }
    }

    private sealed class HealthyHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        }
    }
}
