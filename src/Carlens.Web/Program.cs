using Azure.Monitor.OpenTelemetry.AspNetCore;
using Carlens.Web.HealthChecks;
using Carlens.Web.Middlewares;
using Carlens.Web.Security;
using Carlens.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

if (!string.IsNullOrWhiteSpace(
        builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

var redisConnectionString = builder.Configuration["Redis:ConnectionString"];

if (string.IsNullOrWhiteSpace(redisConnectionString))
{
    throw new InvalidOperationException(
        "Redis:ConnectionString configuration is missing.");
}

var redisInstanceName =
    builder.Configuration["Redis:InstanceName"] ?? "carlens:web:";
var dataProtectionApplicationName =
    builder.Configuration["DataProtection:ApplicationName"];

if (string.IsNullOrWhiteSpace(dataProtectionApplicationName))
{
    throw new InvalidOperationException(
        "DataProtection:ApplicationName configuration is missing.");
}

var dataProtectionKeyRingKeyPrefix =
    builder.Configuration["DataProtection:KeyRingKeyPrefix"];

if (string.IsNullOrWhiteSpace(dataProtectionKeyRingKeyPrefix))
{
    throw new InvalidOperationException(
        "DataProtection:KeyRingKeyPrefix configuration is missing.");
}

var environmentName = builder.Environment.EnvironmentName;
var dataProtectionApplicationDiscriminator =
    $"{dataProtectionApplicationName}:{environmentName}";
var dataProtectionKeyRingKey =
    $"{dataProtectionKeyRingKeyPrefix}:{environmentName.ToLowerInvariant()}";

var redisConfiguration = ConfigurationOptions.Parse(redisConnectionString);
redisConfiguration.AbortOnConnectFail = false;
redisConfiguration.ClientName ??= "carlens-web";

var redisConnection = new Lazy<IConnectionMultiplexer>(
    () => ConnectionMultiplexer.Connect(redisConfiguration));

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IConnectionMultiplexer>(
    _ => redisConnection.Value);
builder.Services
    .AddHealthChecks()
    .AddCheck<RedisHealthCheck>(
        name: "redis",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"]);
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "Carlens.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
builder.Services
    .AddDataProtection()
    .SetApplicationName(dataProtectionApplicationDiscriminator)
    .PersistKeysToStackExchangeRedis(
        () => redisConnection.Value.GetDatabase(),
        dataProtectionKeyRingKey);
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.ConnectionMultiplexerFactory =
        () => Task.FromResult(redisConnection.Value);
    options.InstanceName = redisInstanceName;
});
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.Name = "Carlens.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddSingleton<IAnalysisAccessStore, SessionAnalysisAccessStore>();
builder.Services.AddTransient<InternalApiKeyHandler>();

var internalApiKey = builder.Configuration["Security:InternalApiKey"];

if (!builder.Environment.IsDevelopment() &&
    (string.IsNullOrWhiteSpace(internalApiKey) || internalApiKey.Length < 32))
{
    throw new InvalidOperationException(
        "Security:InternalApiKey must contain at least 32 characters in production.");
}

var analysisPermitLimit = Math.Clamp(
    builder.Configuration.GetValue<int?>(
        "Security:AnalysisRateLimit:PermitLimit") ?? 5,
    1,
    100);
var analysisWindowMinutes = Math.Clamp(
    builder.Configuration.GetValue<int?>(
        "Security:AnalysisRateLimit:WindowMinutes") ?? 15,
    1,
    1440);
var analysisRateLimitKeyPrefix =
    builder.Configuration["Security:AnalysisRateLimit:KeyPrefix"];

if (string.IsNullOrWhiteSpace(analysisRateLimitKeyPrefix))
{
    throw new InvalidOperationException(
        "Security:AnalysisRateLimit:KeyPrefix configuration is missing.");
}

var analysisRateLimitOptions = new AnalysisRateLimitOptions(
    analysisPermitLimit,
    TimeSpan.FromMinutes(analysisWindowMinutes),
    $"{analysisRateLimitKeyPrefix}:{environmentName.ToLowerInvariant()}");

builder.Services.AddSingleton(analysisRateLimitOptions);
builder.Services.AddSingleton<IAnalysisRateLimiter, RedisAnalysisRateLimiter>();
builder.Services.AddResponseCompression();
builder.Services.AddHttpClient<IListingAnalysisApiClient, ListingAnalysisApiClient>(
    client =>
    {
        var baseUrl = builder.Configuration["CarlensApi:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                "CarlensApi:BaseUrl configuration is missing.");
        }

        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<InternalApiKeyHandler>();

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

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseResponseCompression();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSession();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
