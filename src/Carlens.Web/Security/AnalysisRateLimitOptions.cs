namespace Carlens.Web.Security;

public sealed record AnalysisRateLimitOptions(
    int PermitLimit,
    TimeSpan Window,
    string RedisKeyPrefix);
