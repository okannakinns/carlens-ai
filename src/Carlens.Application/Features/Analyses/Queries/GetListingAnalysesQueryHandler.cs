using Carlens.Application.Common.Mappings;
using Carlens.Application.Interfaces;
using Carlens.Contracts.Responses;

namespace Carlens.Application.Features.Analyses.Queries;

public sealed class GetListingAnalysesQueryHandler
{
    private readonly IListingAnalysisRepository _listingAnalysisRepository;
    private readonly ICarListingRepository _carListingRepository;

    public GetListingAnalysesQueryHandler(
        IListingAnalysisRepository listingAnalysisRepository,
        ICarListingRepository carListingRepository)
    {
        _listingAnalysisRepository = listingAnalysisRepository;
        _carListingRepository = carListingRepository;
    }

    public async Task<IReadOnlyList<ListingAnalysisResponse>> HandleAsync(
        GetListingAnalysesQuery query,
        CancellationToken cancellationToken = default)
    {
        var analyses = await _listingAnalysisRepository.GetAllAsync(cancellationToken);
        var listingIds = analyses
            .Select(analysis => analysis.CarListingId)
            .Distinct()
            .ToList();
        var listings = await _carListingRepository.GetByIdsAsync(
            listingIds,
            cancellationToken);

        return analyses
            .Where(analysis => listings.ContainsKey(analysis.CarListingId))
            .Select(analysis => analysis.ToResponse(listings[analysis.CarListingId]))
            .ToList();
    }
}
