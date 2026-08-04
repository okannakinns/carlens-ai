using Carlens.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;

namespace Carlens.Tests;

public sealed class RabbitMqConnectionFactoryTests
{
    [Fact]
    public void Create_UsesAmqpsUriAndEnablesRecovery()
    {
        var configuration = CreateConfiguration(
            ("RabbitMQ:Uri", "amqps://carlens:secret@rabbitmq.example.com:5671/carlens"));

        var factory = RabbitMqConnectionFactory.Create(
            configuration,
            "carlens-tests");

        Assert.Equal("rabbitmq.example.com", factory.HostName);
        Assert.Equal(5671, factory.Port);
        Assert.Equal("carlens", factory.UserName);
        Assert.Equal("carlens", factory.VirtualHost);
        Assert.True(factory.Ssl.Enabled);
        Assert.True(factory.AutomaticRecoveryEnabled);
        Assert.True(factory.TopologyRecoveryEnabled);
        Assert.Equal(TimeSpan.FromSeconds(10), factory.NetworkRecoveryInterval);
        Assert.Equal("carlens-tests", factory.ClientProvidedName);
    }

    [Fact]
    public void Create_UsesLegacyHostConfigurationWhenUriIsMissing()
    {
        var configuration = CreateConfiguration(
            ("RabbitMQ:HostName", "localhost"),
            ("RabbitMQ:Port", "5673"),
            ("RabbitMQ:UserName", "guest"),
            ("RabbitMQ:Password", "guest"));

        var factory = RabbitMqConnectionFactory.Create(
            configuration,
            "carlens-tests");

        Assert.Equal("localhost", factory.HostName);
        Assert.Equal(5673, factory.Port);
        Assert.Equal("guest", factory.UserName);
        Assert.False(factory.Ssl.Enabled);
    }

    [Theory]
    [InlineData("https://rabbitmq.example.com")]
    [InlineData("rabbitmq.example.com")]
    [InlineData("amqps://")]
    public void Create_RejectsInvalidUri(string value)
    {
        var configuration = CreateConfiguration(("RabbitMQ:Uri", value));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            RabbitMqConnectionFactory.Create(configuration, "carlens-tests"));

        Assert.Contains("amqp:// or amqps://", exception.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("65536")]
    [InlineData("not-a-port")]
    public void Create_RejectsInvalidLegacyPort(string value)
    {
        var configuration = CreateConfiguration(("RabbitMQ:Port", value));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            RabbitMqConnectionFactory.Create(configuration, "carlens-tests"));

        Assert.Contains("between 1 and 65535", exception.Message);
    }

    private static IConfiguration CreateConfiguration(
        params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(
                value => value.Key,
                value => (string?)value.Value))
            .Build();
    }
}
