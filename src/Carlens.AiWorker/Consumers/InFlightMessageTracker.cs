namespace Carlens.AiWorker.Consumers;

internal sealed class InFlightMessageTracker
{
    private readonly object _sync = new();
    private int _messageCount;
    private TaskCompletionSource? _drained;

    public IDisposable Begin()
    {
        lock (_sync)
        {
            if (_messageCount == 0)
            {
                _drained = new TaskCompletionSource(
                    TaskCreationOptions.RunContinuationsAsynchronously);
            }

            _messageCount++;
        }

        return new Lease(this);
    }

    public Task WaitForDrainAsync(CancellationToken cancellationToken = default)
    {
        Task drainTask;

        lock (_sync)
        {
            drainTask = _messageCount == 0
                ? Task.CompletedTask
                : _drained!.Task;
        }

        return drainTask.WaitAsync(cancellationToken);
    }

    private void End()
    {
        TaskCompletionSource? drained = null;

        lock (_sync)
        {
            if (_messageCount <= 0)
            {
                throw new InvalidOperationException(
                    "No in-flight message is available to complete.");
            }

            _messageCount--;

            if (_messageCount == 0)
            {
                drained = _drained;
                _drained = null;
            }
        }

        drained?.TrySetResult();
    }

    private sealed class Lease(InFlightMessageTracker owner) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                owner.End();
            }
        }
    }
}
