using Carlens.AiWorker.Consumers;
using Carlens.AiWorker.Services;
using Carlens.Contracts.Events;

namespace Carlens.AiWorker;

public sealed class Worker(
    IAnalysisEventConsumer consumer,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<Worker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await consumer.StartAsync(ProcessAsync, stoppingToken);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Worker shutdown requested. Stopping message intake before processing ends.");

        try
        {
            await consumer.StopAsync(cancellationToken);
        }
        finally
        {
            await base.StopAsync(CancellationToken.None);
        }
    }

    private async Task ProcessAsync(
        AnalyzeListingRequestedEvent message,
        CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var processor = scope.ServiceProvider
            .GetRequiredService<ListingAnalysisProcessor>();

        await processor.ProcessAsync(
            message.AnalysisId,
            cancellationToken);
    }
}

