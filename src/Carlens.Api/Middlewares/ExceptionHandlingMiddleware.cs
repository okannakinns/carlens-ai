using System.Net;
using Carlens.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using HttpBadRequestException =
    Microsoft.AspNetCore.Http.BadHttpRequestException;

namespace Carlens.Api.Middlewares;

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
        catch (ValidationException exception)
        {
            await WriteValidationProblemAsync(context, exception);
        }
        catch (DuplicateAnalysisRequestException exception)
        {
            await WriteProblemAsync(
                context,
                HttpStatusCode.Conflict,
                "Duplicate analysis request",
                exception.Message);
        }
        catch (HttpBadRequestException exception)
            when (exception.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            await WriteProblemAsync(
                context,
                HttpStatusCode.RequestEntityTooLarge,
                "Request body too large",
                "The request body exceeds the allowed size.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception occurred.");

            await WriteProblemAsync(
                context,
                HttpStatusCode.InternalServerError,
                "Server error",
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteValidationProblemAsync(
        HttpContext context,
        ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());

        var problemDetails = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation error"
        };

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string title,
        string detail)
    {
        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail
        };

        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
