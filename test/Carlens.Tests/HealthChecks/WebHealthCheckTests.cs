using System.Net;
using Carlens.Web.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Carlens.Tests.HealthChecks;

public sealed class WebHealthCheckTests
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_endpoint_returns_ok_without_internal_api_key(
        string endpoint)
    {
        await using var factory = new WebFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(endpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed class WebFactory
        : WebApplicationFactory<AnalysesGatewayController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .UseEnvironment("Production")
                .UseSetting("CarlensApi:BaseUrl", "http://localhost:5200")
                .UseSetting(
                    "Security:InternalApiKey",
                    "carlens-health-check-tests-internal-key");
        }
    }
}
