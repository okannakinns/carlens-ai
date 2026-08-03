using Carlens.AiWorker;
using Carlens.AiWorker.Consumers;
using Carlens.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Carlens.Tests;

public sealed class WorkerGracefulShutdownTests
{
    [Fact]
    public async Task StopAsync_DrainsConsumerBeforeCancellingWorkerExecution()
    {
        var consumer = new BlockingAnalysisEventConsumer();
        using var worker = new Worker(
            consumer,
            new UnusedScopeFactory(),
            NullLogger<Worker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await consumer.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));

        using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var stopTask = worker.StopAsync(shutdown.Token);

        await consumer.StopRequested.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.False(consumer.WorkerExecutionToken.IsCancellationRequested);
        Assert.False(stopTask.IsCompleted);
        Assert.Equal(shutdown.Token, consumer.ShutdownToken);

        consumer.AllowStop.TrySetResult();
        await stopTask;

        Assert.True(consumer.WorkerExecutionToken.IsCancellationRequested);
    }

    private sealed class BlockingAnalysisEventConsumer : IAnalysisEventConsumer
    {
        public TaskCompletionSource Started { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource StopRequested { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource AllowStop { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public CancellationToken WorkerExecutionToken { get; private set; }
        public CancellationToken ShutdownToken { get; private set; }

        public Task StartAsync(
            Func<AnalyzeListingRequestedEvent, CancellationToken, Task> handler,
            CancellationToken cancellationToken = default)
        {
            WorkerExecutionToken = cancellationToken;
            Started.TrySetResult();
            return Task.CompletedTask;
        }

        public async Task StopAsync(
            CancellationToken cancellationToken = default)
        {
            ShutdownToken = cancellationToken;
            StopRequested.TrySetResult();
            await AllowStop.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class UnusedScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope()
        {
            throw new InvalidOperationException(
                "No analysis message should be processed in this test.");
        }
    }
}
