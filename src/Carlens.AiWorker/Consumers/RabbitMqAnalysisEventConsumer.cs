using System.Text;
using System.Text.Json;
using Carlens.Contracts.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Carlens.AiWorker.Consumers;

internal sealed class RabbitMqAnalysisEventConsumer(
    IConfiguration configuration,
    ILogger<RabbitMqAnalysisEventConsumer> logger)
    : IAnalysisEventConsumer, IAsyncDisposable
{
    private const string QueueName = "listing-analysis-requested";
    private const ushort PrefetchCount = 1;

    private readonly CancellationTokenSource _processingCancellation = new();
    private readonly InFlightMessageTracker _inFlightMessages = new();
    private readonly SemaphoreSlim _channelOperations = new(1, 1);

    private Func<AnalyzeListingRequestedEvent, CancellationToken, Task>? _handler;
    private IConnection? _connection;
    private IChannel? _channel;
    private string? _consumerTag;
    private int _startRequested;
    private int _stopRequested;
    private int _disposed;

    public async Task StartAsync(
        Func<AnalyzeListingRequestedEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (Interlocked.Exchange(ref _startRequested, 1) != 0)
        {
            throw new InvalidOperationException(
                "The RabbitMQ analysis consumer has already been started.");
        }

        _handler = handler;

        var rabbitMqPort = int.TryParse(
            configuration["RabbitMQ:Port"],
            out var configuredPort)
            ? configuredPort
            : 5672;
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = rabbitMqPort,
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest",
            ConsumerDispatchConcurrency = 1
        };

        IConnection? connection = null;
        IChannel? channel = null;

        try
        {
            connection = await factory.CreateConnectionAsync(cancellationToken);
            channel = await connection.CreateChannelAsync(
                cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);
            await channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: PrefetchCount,
                global: false,
                cancellationToken: cancellationToken);

            var rabbitConsumer = new AsyncEventingBasicConsumer(channel);
            rabbitConsumer.ReceivedAsync += (_, eventArgs) =>
                HandleDeliveryAsync(channel, eventArgs);

            var consumerTag = await channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: rabbitConsumer,
                cancellationToken: cancellationToken);

            _connection = connection;
            _channel = channel;
            _consumerTag = consumerTag;

            logger.LogInformation(
                "RabbitMQ analysis consumer started with prefetch count {PrefetchCount}.",
                PrefetchCount);
        }
        catch
        {
            if (channel is not null)
            {
                await channel.DisposeAsync();
            }

            if (connection is not null)
            {
                await connection.DisposeAsync();
            }

            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _stopRequested, 1) != 0)
        {
            return;
        }

        logger.LogInformation(
            "Stopping RabbitMQ intake before draining in-flight analysis messages.");

        try
        {
            await CancelConsumerAsync(cancellationToken);
            await _inFlightMessages.WaitForDrainAsync(cancellationToken);

            logger.LogInformation(
                "RabbitMQ consumer drained all in-flight analysis messages.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            _processingCancellation.Cancel();
            logger.LogWarning(
                "Worker shutdown timeout elapsed. Cancelling in-flight analysis; " +
                "unacknowledged messages will be requeued when the channel closes.");
        }
        catch (AlreadyClosedException exception)
        {
            _processingCancellation.Cancel();
            logger.LogWarning(
                exception,
                "RabbitMQ channel closed while stopping the consumer. " +
                "Unacknowledged messages will be requeued by the broker.");
        }
        catch (Exception exception)
        {
            _processingCancellation.Cancel();
            logger.LogWarning(
                exception,
                "RabbitMQ consumer could not drain cleanly. " +
                "Unacknowledged messages will be requeued when the channel closes.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _processingCancellation.Cancel();

        if (_channel is not null)
        {
            try
            {
                await _channel.DisposeAsync();
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "RabbitMQ channel could not be disposed cleanly.");
            }
        }

        if (_connection is not null)
        {
            try
            {
                await _connection.DisposeAsync();
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "RabbitMQ connection could not be disposed cleanly.");
            }
        }
    }

    private async Task HandleDeliveryAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs)
    {
        using var inFlightMessage = _inFlightMessages.Begin();
        AnalyzeListingRequestedEvent? message;

        try
        {
            var json = Encoding.UTF8.GetString(eventArgs.Body.Span);
            message = JsonSerializer.Deserialize<AnalyzeListingRequestedEvent>(json);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "Discarding malformed analysis event with delivery tag {DeliveryTag}.",
                eventArgs.DeliveryTag);
            await TryNackAsync(channel, eventArgs.DeliveryTag, requeue: false);
            return;
        }

        if (message is null)
        {
            logger.LogWarning(
                "Discarding empty analysis event with delivery tag {DeliveryTag}.",
                eventArgs.DeliveryTag);
            await TryNackAsync(channel, eventArgs.DeliveryTag, requeue: false);
            return;
        }

        try
        {
            var handler = _handler ?? throw new InvalidOperationException(
                "The analysis event handler has not been configured.");

            await handler(message, _processingCancellation.Token);
            await AcknowledgeAsync(channel, eventArgs.DeliveryTag);
        }
        catch (OperationCanceledException)
            when (_processingCancellation.IsCancellationRequested)
        {
            logger.LogWarning(
                "Analysis event {AnalysisId} was interrupted by forced shutdown " +
                "and will be requeued.",
                message.AnalysisId);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "An error occurred while processing analysis event {AnalysisId}.",
                message.AnalysisId);
            await TryNackAsync(channel, eventArgs.DeliveryTag, requeue: true);
        }
    }

    private async Task CancelConsumerAsync(CancellationToken cancellationToken)
    {
        var channel = _channel;
        var consumerTag = _consumerTag;

        if (channel is not { IsOpen: true } ||
            string.IsNullOrWhiteSpace(consumerTag))
        {
            return;
        }

        await _channelOperations.WaitAsync(cancellationToken);

        try
        {
            await channel.BasicCancelAsync(
                consumerTag,
                noWait: false,
                cancellationToken: cancellationToken);
        }
        finally
        {
            _channelOperations.Release();
        }
    }

    private async Task AcknowledgeAsync(IChannel channel, ulong deliveryTag)
    {
        var cancellationToken = _processingCancellation.Token;
        await _channelOperations.WaitAsync(cancellationToken);

        try
        {
            await channel.BasicAckAsync(
                deliveryTag,
                multiple: false,
                cancellationToken: cancellationToken);
        }
        finally
        {
            _channelOperations.Release();
        }
    }

    private async Task TryNackAsync(
        IChannel channel,
        ulong deliveryTag,
        bool requeue)
    {
        try
        {
            var cancellationToken = _processingCancellation.Token;
            await _channelOperations.WaitAsync(cancellationToken);

            try
            {
                await channel.BasicNackAsync(
                    deliveryTag,
                    multiple: false,
                    requeue: requeue,
                    cancellationToken: cancellationToken);
            }
            finally
            {
                _channelOperations.Release();
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "RabbitMQ delivery {DeliveryTag} could not be rejected. " +
                "The broker will recover it when the channel closes.",
                deliveryTag);
        }
    }
}
