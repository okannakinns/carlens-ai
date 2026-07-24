namespace Carlens.Application.Interfaces;

public interface IAnalysisCacheService
{
    Task<bool> TryReserveAsync(
        string key,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default);
}
