using System.Net;
using Microsoft.AspNetCore.Mvc;
using HttpBadRequestException =
    Microsoft.AspNetCore.Http.BadHttpRequestException;

namespace Carlens.Web.Middlewares;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (HttpBadRequestException exception)
            when (exception.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteProblemAsync(
                context,
                HttpStatusCode.RequestEntityTooLarge,
                "İstek gövdesi çok büyük",
                "Gönderilen veri izin verilen boyutu aşıyor.");
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            _logger.LogError(exception, "Unhandled Web BFF exception occurred.");

            await WriteProblemAsync(
                context,
                HttpStatusCode.InternalServerError,
                "Sunucu hatası",
                "Beklenmeyen bir hata oluştu.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string title,
        string detail)
    {
        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;
        context.Response.Headers.CacheControl = "no-store";

        await context.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = (int)statusCode,
                Title = title,
                Detail = detail
            });
    }
}
