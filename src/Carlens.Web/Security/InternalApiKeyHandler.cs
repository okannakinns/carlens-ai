using Carlens.Contracts.Security;

namespace Carlens.Web.Security;

public sealed class InternalApiKeyHandler : DelegatingHandler
{
    private readonly IConfiguration _configuration;

    public InternalApiKeyHandler(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var apiKey = _configuration["Security:InternalApiKey"];

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.TryAddWithoutValidation(
                InternalApiHeaders.ApiKey,
                apiKey);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
