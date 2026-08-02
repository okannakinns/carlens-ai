using System.Threading.RateLimiting;
using Carlens.Web.Middlewares;
using Carlens.Web.Security;
using Carlens.Web.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "Carlens.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddDistributedMemoryCache();
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

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(
        SecurityPolicyNames.AnalysisCreation,
        context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = analysisPermitLimit,
                Window = TimeSpan.FromMinutes(analysisWindowMinutes),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(
                MetadataName.RetryAfter,
                out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Ceiling(retryAfter.TotalSeconds).ToString();
        }

        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Çok fazla analiz isteği",
                Detail =
                    $"Bu bağlantı için {analysisWindowMinutes} dakika içinde " +
                    $"en fazla {analysisPermitLimit} analiz oluşturabilirsiniz."
            },
            cancellationToken);
    };
});
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
app.UseRateLimiter();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
