using Carlens.Application.Common.Images;
using Carlens.Application.Common.Mappings;
using Carlens.Application.Interfaces;
using Carlens.Contracts.Events;
using Carlens.Contracts.Responses;
using Carlens.Domain.Entities;
using FluentValidation;

namespace Carlens.Application.Features.Listings.Commands;

public sealed class CreateManualVehicleAnalysisCommandHandler
{
    private readonly ICarListingRepository _carListingRepository;
    private readonly IListingAnalysisRepository _listingAnalysisRepository;
    private readonly IAnalysisRequestPublisher _analysisRequestPublisher;
    private readonly IValidator<CreateManualVehicleAnalysisCommand> _validator;

    public CreateManualVehicleAnalysisCommandHandler(
        ICarListingRepository carListingRepository,
        IListingAnalysisRepository listingAnalysisRepository,
        IAnalysisRequestPublisher analysisRequestPublisher,
        IValidator<CreateManualVehicleAnalysisCommand> validator)
    {
        _carListingRepository = carListingRepository;
        _listingAnalysisRepository = listingAnalysisRepository;
        _analysisRequestPublisher = analysisRequestPublisher;
        _validator = validator;
    }

    public async Task<ListingAnalysisResponse> HandleAsync(
        CreateManualVehicleAnalysisCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var imageCandidates = command.Images
            .Select(image => new CarListing.CarListingImageCandidate(
                VehicleImageContentInspector.DetectContentType(image.Content)!,
                image.Content))
            .ToList();

        var carListing = CarListing.CreateManual(
            command.Brand,
            command.Series,
            command.Model,
            command.ModelYear,
            command.Price,
            command.Mileage,
            command.FuelType,
            command.TransmissionType,
            command.Location,
            command.Description,
            command.DamageInformation,
            imageCandidates);
        var analysis = new ListingAnalysis(carListing.Id);

        await _carListingRepository.AddAsync(carListing, cancellationToken);
        await _listingAnalysisRepository.AddAsync(analysis, cancellationToken);

        await _analysisRequestPublisher.PublishAsync(
            new AnalyzeListingRequestedEvent(
                analysis.Id,
                carListing.Id,
                DateTime.UtcNow),
            cancellationToken);

        return analysis.ToResponse(carListing);
    }
}
