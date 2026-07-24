using Carlens.Application.Extensions;
using Carlens.Api.Middlewares;
using Carlens.Api.Security;
using Carlens.Infrastructure.Extensions;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

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

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<InternalApiKeyMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
