using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Carlens.Infrastructure.Messaging;

public static class RabbitMqConnectionFactory
{
    private const int DefaultPort = 5672;
    private const ushort DefaultConsumerDispatchConcurrency = 1;

    public static ConnectionFactory Create(
        IConfiguration configuration,
        string clientProvidedName,
        ushort consumerDispatchConcurrency = DefaultConsumerDispatchConcurrency)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientProvidedName);

        if (consumerDispatchConcurrency == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(consumerDispatchConcurrency),
                "Consumer dispatch concurrency must be greater than zero.");
        }

        var factory = new ConnectionFactory
        {
            AutomaticRecoveryEnabled = true,
            ClientProvidedName = clientProvidedName,
            ConsumerDispatchConcurrency = consumerDispatchConcurrency,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            TopologyRecoveryEnabled = true
        };

        var configuredUri = configuration["RabbitMQ:Uri"];

        if (!string.IsNullOrWhiteSpace(configuredUri))
        {
            factory.Uri = ParseUri(configuredUri);
            return factory;
        }

        factory.HostName = configuration["RabbitMQ:HostName"] ?? "localhost";
        factory.Port = ParsePort(configuration["RabbitMQ:Port"]);
        factory.UserName = configuration["RabbitMQ:UserName"] ?? "guest";
        factory.Password = configuration["RabbitMQ:Password"] ?? "guest";

        return factory;
    }

    private static Uri ParseUri(string configuredUri)
    {
        if (!Uri.TryCreate(configuredUri, UriKind.Absolute, out var uri) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            (uri.Scheme != "amqp" && uri.Scheme != "amqps"))
        {
            throw new InvalidOperationException(
                "RabbitMQ:Uri must be an absolute amqp:// or amqps:// URI.");
        }

        return uri;
    }

    private static int ParsePort(string? configuredPort)
    {
        if (string.IsNullOrWhiteSpace(configuredPort))
        {
            return DefaultPort;
        }

        if (!int.TryParse(configuredPort, out var port) || port is < 1 or > 65535)
        {
            throw new InvalidOperationException(
                "RabbitMQ:Port must be an integer between 1 and 65535.");
        }

        return port;
    }
}
