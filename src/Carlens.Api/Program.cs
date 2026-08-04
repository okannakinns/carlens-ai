using Azure.Monitor.OpenTelemetry.AspNetCore;
using Carlens.Application.Extensions;
using Carlens.Api.Middlewares;
using Carlens.Api.Security;
using Carlens.Infrastructure.Extensions;
using Carlens.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

if (!string.IsNullOrWhiteSpace(
        builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<CarlensDbContext>(
        name: "postgres",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"]);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var internalApiKey = builder.Configuration["Security:InternalApiKey"];

if (!builder.Environment.IsDevelopment() &&
    (string.IsNullOrWhiteSpace(internalApiKey) || internalApiKey.Length < 32))
{
    throw new InvalidOperationException(
        "Security:InternalApiKey must contain at least 32 characters in production.");
}

builder.Services.AddSingleton(new InternalApiSecurityOptions(internalApiKey));

var app = builder.Build();

app.UseHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false
    });
app.UseHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready")
    });

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<InternalApiKeyMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
