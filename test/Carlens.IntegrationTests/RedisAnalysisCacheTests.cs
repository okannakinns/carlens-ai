using Carlens.Infrastructure.Cache;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Carlens.IntegrationTests;

public sealed class RedisAnalysisCacheTests : IAsyncLifetime
{
    private readonly RedisContainer _redis = new RedisBuilder(
            "redis:8-alpine")
        .Build();

    private IConnectionMultiplexer? _connection;

    public async Task InitializeAsync()
    {
        await _redis.StartAsync();
        _connection = await ConnectionMultiplexer.ConnectAsync(
            _redis.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await _redis.DisposeAsync();
    }

    [Fact]
    public async Task Reservation_is_atomic_and_can_be_released()
    {
        var service = new RedisAnalysisCacheService(
            Assert.IsAssignableFrom<IConnectionMultiplexer>(_connection));
        var key = $"integration-analysis:{Guid.NewGuid():N}";

        var attempts = Enumerable.Range(0, 20)
            .Select(_ => service.TryReserveAsync(key, TimeSpan.FromMinutes(1)));
        var results = await Task.WhenAll(attempts);

        Assert.Single(results, reserved => reserved);

        await service.RemoveAsync(key);

        Assert.True(
            await service.TryReserveAsync(key, TimeSpan.FromMinutes(1)));
    }
}
