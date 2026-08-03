using Carlens.AiWorker.Consumers;

namespace Carlens.Tests;

public sealed class InFlightMessageTrackerTests
{
    [Fact]
    public async Task WaitForDrainAsync_WhenNoMessagesAreActive_CompletesImmediately()
    {
        var tracker = new InFlightMessageTracker();

        await tracker.WaitForDrainAsync();
    }

    [Fact]
    public async Task WaitForDrainAsync_WaitsUntilEveryMessageCompletes()
    {
        var tracker = new InFlightMessageTracker();
        var firstMessage = tracker.Begin();
        var secondMessage = tracker.Begin();

        var drainTask = tracker.WaitForDrainAsync();

        Assert.False(drainTask.IsCompleted);

        firstMessage.Dispose();
        Assert.False(drainTask.IsCompleted);

        secondMessage.Dispose();
        await drainTask;
    }

    [Fact]
    public async Task WaitForDrainAsync_WhenDeadlineExpires_IsCancelled()
    {
        var tracker = new InFlightMessageTracker();
        using var message = tracker.Begin();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            tracker.WaitForDrainAsync(cancellation.Token));
    }
}
