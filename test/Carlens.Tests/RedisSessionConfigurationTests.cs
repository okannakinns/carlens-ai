using Carlens.Web.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Carlens.Tests;

public sealed class RedisSessionConfigurationTests
{
    [Fact]
    public void Web_uses_redis_for_distributed_session_storage()
    {
        using var factory = new WebFactory(configureRedis: true);
        using var scope = factory.Services.CreateScope();

        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        var options = scope.ServiceProvider
            .GetRequiredService<IOptions<RedisCacheOptions>>()
            .Value;

        Assert.IsAssignableFrom<RedisCache>(cache);
        Assert.Equal("redis.test:6379", options.Configuration);
        Assert.Equal("carlens:web:test:", options.InstanceName);
    }

    [Fact]
    public void Web_fails_fast_when_redis_connection_is_missing()
    {
        using var factory = new WebFactory(configureRedis: false);

        var exception = Assert.Throws<InvalidOperationException>(
            factory.CreateClient);

        Assert.Equal(
            "Redis:ConnectionString configuration is missing.",
            exception.Message);
    }

    private sealed class WebFactory(bool configureRedis)
        : WebApplicationFactory<AnalysesGatewayController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .UseEnvironment("Production")
                .UseSetting("CarlensApi:BaseUrl", "http://localhost:5200")
                .UseSetting(
                    "Security:InternalApiKey",
                    "carlens-redis-session-tests-internal-key")
                .UseSetting("Redis:InstanceName", "carlens:web:test:");

            if (configureRedis)
            {
                builder.UseSetting(
                    "Redis:ConnectionString",
                    "redis.test:6379");
            }
        }
    }
}
