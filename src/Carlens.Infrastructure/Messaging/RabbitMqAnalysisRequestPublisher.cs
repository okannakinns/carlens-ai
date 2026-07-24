using Carlens.Application.Interfaces;
using Carlens.Contracts.Events;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
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
        var rabbitMqPort = int.TryParse(_configuration["RabbitMQ:Port"], out var configuredPort)
            ? configuredPort
            : 5672;

        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = rabbitMqPort,
            UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest",

        };

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken:cancellationToken);

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
