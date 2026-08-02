using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Carlens.Web.Security;

public sealed class AnalysisRateLimitFilter(
    IAnalysisRateLimiter rateLimiter,
    AnalysisRateLimitOptions options,
    ILogger<AnalysisRateLimitFilter> logger)
    : IAsyncResourceFilter
{
    private static readonly TimeSpan StoreUnavailableRetryAfter =
        TimeSpan.FromSeconds(30);

    public async Task OnResourceExecutionAsync(
        ResourceExecutingContext context,
        ResourceExecutionDelegate next)
    {
        AnalysisRateLimitDecision decision;

        try
        {
            decision = await rateLimiter.AcquireAsync(
                ResolveClientIdentifier(context.HttpContext),
                context.HttpContext.RequestAborted);
        }
        catch (AnalysisRateLimitUnavailableException exception)
        {
            logger.LogWarning(
                exception,
                "Distributed analysis rate limiting is unavailable.");
            SetRetryAfter(context.HttpContext, StoreUnavailableRetryAfter);
            context.Result = CreateProblemResult(
                StatusCodes.Status503ServiceUnavailable,
                "Analiz servisi geçici olarak kullanılamıyor",
                "İstek sınırı güvenli biçimde doğrulanamadı. " +
                "Lütfen biraz sonra tekrar deneyin.");
            return;
        }

        if (!decision.IsAllowed)
        {
            SetRetryAfter(context.HttpContext, decision.RetryAfter);
            context.Result = CreateProblemResult(
                StatusCodes.Status429TooManyRequests,
                "Çok fazla analiz isteği",
                $"Bu bağlantı için {options.Window.TotalMinutes:0} dakika içinde " +
                $"en fazla {options.PermitLimit} analiz oluşturabilirsiniz.");
            return;
        }

        await next();
    }

    private static string ResolveClientIdentifier(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;

        if (address is null)
        {
            return "unknown";
        }

        return address.IsIPv4MappedToIPv6
            ? address.MapToIPv4().ToString()
            : address.ToString();
    }

    private static void SetRetryAfter(
        HttpContext context,
        TimeSpan retryAfter)
    {
        context.Response.Headers.RetryAfter = Math.Max(
            1,
            Math.Ceiling(retryAfter.TotalSeconds)).ToString(
                CultureInfo.InvariantCulture);
    }

    private static ObjectResult CreateProblemResult(
        int statusCode,
        string title,
        string detail)
    {
        return new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        })
        {
            StatusCode = statusCode
        };
    }
}
