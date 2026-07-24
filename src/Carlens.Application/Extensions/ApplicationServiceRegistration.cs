using Carlens.Application.Features.Analyses.Queries;
using Carlens.Application.Features.Listings.Commands;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Carlens.Application.Extensions;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<CreateListingAnalysisCommandHandler>();
        services.AddScoped<CreateManualVehicleAnalysisCommandHandler>();
        services.AddScoped<GetListingAnalysisByIdQueryHandler>();
        services.AddScoped<GetListingAnalysesQueryHandler>();

        services.AddScoped<IValidator<CreateListingAnalysisCommand>, CreateListingAnalysisCommandValidator>();
        services.AddScoped<
            IValidator<CreateManualVehicleAnalysisCommand>,
            CreateManualVehicleAnalysisCommandValidator>();

        return services;
    }
}
