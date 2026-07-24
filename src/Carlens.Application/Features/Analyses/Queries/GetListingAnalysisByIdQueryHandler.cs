using Carlens.Application.Common.Mappings;
using Carlens.Application.Interfaces;
using Carlens.Contracts.Responses;
using Carlens.Domain.Enums;

namespace Carlens.Application.Features.Analyses.Queries;

public sealed class GetListingAnalysisByIdQueryHandler
{
    private readonly IListingAnalysisRepository _listingAnalysisRepository;
    private readonly ICarListingRepository _carListingRepository;

    public GetListingAnalysisByIdQueryHandler(
        IListingAnalysisRepository listingAnalysisRepository,
        ICarListingRepository carListingRepository)
    {
        _listingAnalysisRepository = listingAnalysisRepository;
        _carListingRepository = carListingRepository;
    }

    public async Task<ListingAnalysisResponse?> HandleAsync(
        GetListingAnalysisByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.AnalysisId == Guid.Empty)
        {
            throw new ArgumentException("Analysis id is required.", nameof(query.AnalysisId));
        }

        var analysis = await _listingAnalysisRepository.GetByIdAsync(
            query.AnalysisId,
            cancellationToken);

        if (analysis is null)
        {
            return null;
        }

        var listing = analysis.Status is AnalysisStatus.Pending or AnalysisStatus.Processing
            ? await _carListingRepository.GetByIdWithoutImagesAsync(
                analysis.CarListingId,
                cancellationToken)
            : await _carListingRepository.GetByIdAsync(
                analysis.CarListingId,
                cancellationToken);

        return listing is null ? null : analysis.ToResponse(listing);
    }
}
