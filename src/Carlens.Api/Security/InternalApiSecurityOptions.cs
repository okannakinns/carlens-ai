namespace Carlens.Api.Security;

public sealed record InternalApiSecurityOptions(string? ApiKey)
{
    public bool IsEnabled => !string.IsNullOrWhiteSpace(ApiKey);
}
