using Carlens.Application.Interfaces;
using Carlens.Contracts.Events;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Carlens.Infrastructure.Messaging;

public sealed class RabbitMqAnalysisRequestPublisher : IAnalysisRequestPublisher
{
    private const string QueueName = "listing-analysis-requested";
    private readonly IConfiguration _configuration;

    public RabbitMqAnalysisRequestPublisher(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task PublishAsync(AnalyzeListingRequestedEvent analysisRequestedEvent, CancellationToken cancellationToken = default)
    {
        var factory = RabbitMqConnectionFactory.Create(
            _configuration,
            clientProvidedName: "carlens-api-publisher");

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);


        var json = JsonSerializer.Serialize(analysisRequestedEvent);
        var body = Encoding.UTF8.GetBytes(json);

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: QueueName,
            body: body,
            cancellationToken: cancellationToken);
    }
}
