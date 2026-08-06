using Carlens.Application.Interfaces;
using Carlens.Infrastructure.Cache;
using Carlens.Infrastructure.ExternalServices;
using Carlens.Infrastructure.Messaging;
using Carlens.Infrastructure.Persistence;
using Carlens.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Carlens.Infrastructure.Extensions;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<CarlensDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Postgres"));
        });

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var connectionString =
                configuration["Redis:ConnectionString"] ?? "localhost:6379";

            return ConnectionMultiplexer.Connect(connectionString);
        });

        services.AddScoped<ICarListingRepository, CarListingRepository>();
        services.AddScoped<IListingAnalysisRepository, ListingAnalysisRepository>();
        services.AddScoped<IAnalysisRequestPublisher, RabbitMqAnalysisRequestPublisher>();
        services.AddScoped<IAnalysisCacheService, RedisAnalysisCacheService>();

        services.Configure<ListingSourceOptions>(
            configuration.GetSection("ListingSource"));
        services.AddSingleton<IPrimaryListingSourceReader, ArabamComListingSourceReader>();
        services.AddSingleton<IFallbackListingSourceReader, OpenAiWebListingSourceReader>();
        services.AddSingleton<IListingSourceReader, ResilientListingSourceReader>();

        services.Configure<OpenAiOptions>(options =>
        {
            var section = configuration.GetSection("OpenAI");

            options.BaseUrl =
                section["BaseUrl"] ?? "https://api.openai.com/v1/";
            options.Model = section["Model"] ?? "gpt-5.4-mini";
            options.ApiKey = section["ApiKey"];
            options.MaxAnalyzedImages =
                section.GetValue("MaxAnalyzedImages", 8);
            options.ImageDetail =
                section["ImageDetail"] ?? "high";
            options.MaxOutputTokens =
                section.GetValue("MaxOutputTokens", 900);
            options.InputCostPerMillionTokensUsd =
                section.GetValue("InputCostPerMillionTokensUsd", 0.75m);
            options.OutputCostPerMillionTokensUsd =
                section.GetValue("OutputCostPerMillionTokensUsd", 4.50m);
        });

        services.AddHttpClient<IListingAnalysisAiService, OpenAiListingAnalysisService>(
            (serviceProvider, httpClient) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<OpenAiOptions>>()
                    .Value;

                httpClient.BaseAddress = new Uri(options.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(90);
            });
        services.AddHttpClient(
            OpenAiWebListingSourceReader.HttpClientName,
            (serviceProvider, httpClient) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<OpenAiOptions>>()
                    .Value;

                httpClient.BaseAddress = new Uri(options.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(120);
            });

        return services;
    }
}
