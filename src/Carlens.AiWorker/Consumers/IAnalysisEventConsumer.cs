using Carlens.Contracts.Events;

namespace Carlens.AiWorker.Consumers;

public interface IAnalysisEventConsumer
{
    Task StartAsync(
        Func<AnalyzeListingRequestedEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
