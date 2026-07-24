using System.Security.Cryptography;
using System.Text;
using Carlens.Api.Security;
using Carlens.Contracts.Security;
using Microsoft.AspNetCore.Mvc;

namespace Carlens.Api.Middlewares;

public sealed class InternalApiKeyMiddleware
{
    private readonly RequestDelegate _next;

    public InternalApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        InternalApiSecurityOptions options)
    {
        if (!options.IsEnabled)
        {
            await _next(context);
            return;
        }

        var suppliedKey = context.Request.Headers[InternalApiHeaders.ApiKey];

        if (suppliedKey.Count != 1 ||
            !KeysMatch(options.ApiKey!, suppliedKey.ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.CacheControl = "no-store";
            await context.Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Yetkisiz servis isteği"
                });
            return;
        }

        await _next(context);
    }

    private static bool KeysMatch(string expectedKey, string suppliedKey)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expectedKey);
        var suppliedBytes = Encoding.UTF8.GetBytes(suppliedKey);

        return expectedBytes.Length == suppliedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(
                   expectedBytes,
                   suppliedBytes);
    }
}
