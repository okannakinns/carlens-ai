using Carlens.Application.Interfaces;
using StackExchange.Redis;

namespace Carlens.Infrastructure.Cache;

public sealed class RedisAnalysisCacheService : IAnalysisCacheService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisAnalysisCacheService(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<bool> TryReserveAsync(
        string key,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var database = _connectionMultiplexer.GetDatabase();

        return await database.StringSetAsync(
            key,
            "1",
            expiration,
            When.NotExists);
    }

    public async Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var database = _connectionMultiplexer.GetDatabase();
        await database.KeyDeleteAsync(key);
    }
}
