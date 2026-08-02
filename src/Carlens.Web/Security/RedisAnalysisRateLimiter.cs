using System.Security.Cryptography;
using System.Text;
using StackExchange.Redis;

namespace Carlens.Web.Security;

internal sealed class RedisAnalysisRateLimiter(
    IConnectionMultiplexer connection,
    AnalysisRateLimitOptions options)
    : IAnalysisRateLimiter
{
    private const string AcquireScript = """
        local requestCount = redis.call('INCR', KEYS[1])
        local remainingTtl = redis.call('PTTL', KEYS[1])

        if remainingTtl < 0 then
            redis.call('PEXPIRE', KEYS[1], ARGV[1])
            remainingTtl = tonumber(ARGV[1])
        end

        return { requestCount, remainingTtl }
        """;

    public async ValueTask<AnalysisRateLimitDecision> AcquireAsync(
        string clientIdentifier,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientIdentifier);
        cancellationToken.ThrowIfCancellationRequested();

        var partitionHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(clientIdentifier)))
            .ToLowerInvariant();
        RedisKey redisKey = $"{options.RedisKeyPrefix}:{partitionHash}";
        var windowMilliseconds = checked((long)options.Window.TotalMilliseconds);

        try
        {
            var response = await connection
                .GetDatabase()
                .ScriptEvaluateAsync(
                    AcquireScript,
                    [redisKey],
                    [windowMilliseconds])
                .WaitAsync(cancellationToken);
            var values = (RedisResult[]?)response;

            if (values is not { Length: 2 })
            {
                throw new InvalidOperationException(
                    "Redis returned an invalid rate-limit response.");
            }

            var requestCount = (long)values[0];
            var remainingTtlMilliseconds = (long)values[1];
            var retryAfter = requestCount > options.PermitLimit
                ? TimeSpan.FromMilliseconds(
                    remainingTtlMilliseconds > 0
                        ? remainingTtlMilliseconds
                        : windowMilliseconds)
                : TimeSpan.Zero;

            return new AnalysisRateLimitDecision(
                requestCount <= options.PermitLimit,
                retryAfter);
        }
        catch (RedisException exception)
        {
            throw new AnalysisRateLimitUnavailableException(
                "The distributed rate-limit store is unavailable.",
                exception);
        }
    }
}
