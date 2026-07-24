using Carlens.Contracts.Security;
using Carlens.Web.Security;
using Microsoft.Extensions.Configuration;

namespace Carlens.Tests;

public sealed class InternalApiKeyHandlerTests
{
    [Fact]
    public async Task SendAsync_WhenKeyIsConfigured_AddsInternalHeader()
    {
        const string expectedKey =
            "carlens-test-internal-key-with-more-than-32-characters";
        var configuration = new ConfigurationManager
        {
            ["Security:InternalApiKey"] = expectedKey
        };
        var recorder = new RecordingHandler();
        var handler = new InternalApiKeyHandler(configuration)
        {
            InnerHandler = recorder
        };
        using var client = new HttpClient(handler);

        await client.GetAsync("https://api.example.test/analyses");

        Assert.NotNull(recorder.Request);
        Assert.Equal(
            expectedKey,
            recorder.Request.Headers
                .GetValues(InternalApiHeaders.ApiKey)
                .Single());
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(
                System.Net.HttpStatusCode.OK));
        }
    }
}
