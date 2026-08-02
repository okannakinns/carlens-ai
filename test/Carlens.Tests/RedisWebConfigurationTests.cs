using Carlens.Web.Controllers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Carlens.Tests;

public sealed class RedisWebConfigurationTests
{
    [Fact]
    public void Web_uses_redis_for_distributed_session_storage()
    {
        using var factory = new WebFactory();
        using var scope = factory.Services.CreateScope();

        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        var options = scope.ServiceProvider
            .GetRequiredService<IOptions<RedisCacheOptions>>()
            .Value;

        Assert.IsAssignableFrom<RedisCache>(cache);
        Assert.NotNull(options.ConnectionMultiplexerFactory);
        Assert.Null(options.Configuration);
        Assert.Equal("carlens:web:test:", options.InstanceName);
    }

    [Fact]
    public void Web_persists_data_protection_keys_to_redis()
    {
        using var factory = new WebFactory();
        using var scope = factory.Services.CreateScope();

        var dataProtectionOptions = scope.ServiceProvider
            .GetRequiredService<IOptions<DataProtectionOptions>>()
            .Value;
        var keyManagementOptions = scope.ServiceProvider
            .GetRequiredService<IOptions<KeyManagementOptions>>()
            .Value;

        Assert.Equal(
            "Carlens.Web.Tests:Production",
            dataProtectionOptions.ApplicationDiscriminator);
        Assert.IsType<RedisXmlRepository>(keyManagementOptions.XmlRepository);
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

    [Theory]
    [InlineData(
        "DataProtection:ApplicationName",
        "DataProtection:ApplicationName configuration is missing.")]
    [InlineData(
        "DataProtection:KeyRingKeyPrefix",
        "DataProtection:KeyRingKeyPrefix configuration is missing.")]
    public void Web_fails_fast_when_data_protection_configuration_is_missing(
        string setting,
        string expectedMessage)
    {
        using var factory = new WebFactory(emptySetting: setting);

        var exception = Assert.Throws<InvalidOperationException>(
            factory.CreateClient);

        Assert.Equal(expectedMessage, exception.Message);
    }

    private sealed class WebFactory(
        bool configureRedis = true,
        string? emptySetting = null)
        : WebApplicationFactory<AnalysesGatewayController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .UseEnvironment("Production")
                .UseSetting("CarlensApi:BaseUrl", "http://localhost:5200")
                .UseSetting(
                    "Security:InternalApiKey",
                    "carlens-redis-configuration-tests-internal-key")
                .UseSetting("Redis:InstanceName", "carlens:web:test:")
                .UseSetting(
                    "DataProtection:ApplicationName",
                    "Carlens.Web.Tests")
                .UseSetting(
                    "DataProtection:KeyRingKeyPrefix",
                    "carlens:web:tests:data-protection")
                .UseSetting(
                    "Redis:ConnectionString",
                    configureRedis
                        ? "127.0.0.1:1,abortConnect=false,connectRetry=0," +
                          "connectTimeout=100,syncTimeout=100,asyncTimeout=100"
                        : string.Empty);

            if (emptySetting is not null)
            {
                builder.UseSetting(emptySetting, string.Empty);
            }
        }
    }
}
