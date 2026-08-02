using System.Net;
using Carlens.Api.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Carlens.Tests.HealthChecks;

public sealed class ApiHealthCheckTests
{
    [Fact]
    public async Task Live_returns_ok_when_postgres_is_unavailable()
    {
        await using var factory = new ApiFactory(forcePostgresHealthy: false);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ready_returns_service_unavailable_when_postgres_is_unavailable()
    {
        await using var factory = new ApiFactory(forcePostgresHealthy: false);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Ready_returns_ok_when_required_checks_are_healthy()
    {
        await using var factory = new ApiFactory(forcePostgresHealthy: true);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed class ApiFactory(bool forcePostgresHealthy)
        : WebApplicationFactory<ListingAnalysesController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .UseEnvironment("Production")
                .UseSetting(
                    "ConnectionStrings:Postgres",
                    "Host=127.0.0.1;Port=1;Database=carlens_health;" +
                    "Username=test;Password=test;Timeout=1;Command Timeout=1")
                .UseSetting(
                    "Security:InternalApiKey",
                    "carlens-health-check-tests-internal-key");

            if (forcePostgresHealthy)
            {
                builder.ConfigureServices(services =>
                {
                    services.PostConfigure<HealthCheckServiceOptions>(options =>
                    {
                        var postgresRegistration = options.Registrations.Single(
                            registration => registration.Name == "postgres");

                        options.Registrations.Remove(postgresRegistration);
                        options.Registrations.Add(
                            new HealthCheckRegistration(
                                "postgres",
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
