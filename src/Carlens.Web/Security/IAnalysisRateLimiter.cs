namespace Carlens.Web.Security;

public interface IAnalysisRateLimiter
{
    ValueTask<AnalysisRateLimitDecision> AcquireAsync(
        string clientIdentifier,
        CancellationToken cancellationToken = default);
}

public readonly record struct AnalysisRateLimitDecision(
    bool IsAllowed,
    TimeSpan RetryAfter);

public sealed class AnalysisRateLimitUnavailableException(
    string message,
    Exception innerException)
    : Exception(message, innerException);
