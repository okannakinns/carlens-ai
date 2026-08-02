using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Carlens.Web.HealthChecks;

internal sealed class RedisHealthCheck(IConnectionMultiplexer connection)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var latency = await connection
            .GetDatabase()
            .PingAsync()
            .WaitAsync(cancellationToken);

        return HealthCheckResult.Healthy(
            $"Redis responded in {latency.TotalMilliseconds:F0} ms.");
    }
}
