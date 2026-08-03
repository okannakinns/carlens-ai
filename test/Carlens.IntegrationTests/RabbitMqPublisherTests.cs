using System.Text;
using System.Text.Json;
using Carlens.Contracts.Events;
using Carlens.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace Carlens.IntegrationTests;

public sealed class RabbitMqPublisherTests : IAsyncLifetime
{
    private const string QueueName = "listing-analysis-requested";
    private const string UserName = "carlens";
    private const string Password = "carlens-integration-tests";

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder(
            "rabbitmq:4-management-alpine")
        .WithUsername(UserName)
        .WithPassword(Password)
        .Build();

    public Task InitializeAsync()
    {
        return _rabbitMq.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _rabbitMq.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task Publisher_sends_the_contract_to_the_expected_queue()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMQ:HostName"] = _rabbitMq.Hostname,
                ["RabbitMQ:Port"] = _rabbitMq.GetMappedPublicPort(5672).ToString(),
                ["RabbitMQ:UserName"] = UserName,
                ["RabbitMQ:Password"] = Password
            })
            .Build();
        var publisher = new RabbitMqAnalysisRequestPublisher(configuration);
        var expectedEvent = new AnalyzeListingRequestedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow);

        await publisher.PublishAsync(expectedEvent);

        var factory = new ConnectionFactory
        {
            HostName = _rabbitMq.Hostname,
            Port = _rabbitMq.GetMappedPublicPort(5672),
            UserName = UserName,
            Password = Password
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        BasicGetResult? message = null;

        for (var attempt = 0; attempt < 10 && message is null; attempt++)
        {
            message = await channel.BasicGetAsync(QueueName, autoAck: true);

            if (message is null)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }

        Assert.NotNull(message);

        var json = Encoding.UTF8.GetString(message.Body.Span);
        var actualEvent = JsonSerializer.Deserialize<AnalyzeListingRequestedEvent>(json);

        Assert.Equal(expectedEvent, actualEvent);
    }
}
