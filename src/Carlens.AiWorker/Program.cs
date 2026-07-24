using Carlens.AiWorker;
using Carlens.AiWorker.Services;
using Carlens.Application.Extensions;
using Carlens.Infrastructure.Extensions;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddScoped<ListingAnalysisProcessor>();

builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
host.Run();