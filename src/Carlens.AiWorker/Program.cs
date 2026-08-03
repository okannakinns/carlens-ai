using Carlens.AiWorker;
using Carlens.AiWorker.Consumers;
using Carlens.AiWorker.Services;
using Carlens.Application.Extensions;
using Carlens.Infrastructure.Extensions;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddScoped<ListingAnalysisProcessor>();
builder.Services.AddSingleton<IAnalysisEventConsumer, RabbitMqAnalysisEventConsumer>();

var shutdownTimeoutSeconds = Math.Clamp(
    builder.Configuration.GetValue<int?>(
        "Worker:ShutdownTimeoutSeconds") ?? 120,
    5,
    600);

builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(shutdownTimeoutSeconds);
});

builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
await host.RunAsync();
