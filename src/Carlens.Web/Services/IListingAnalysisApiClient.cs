using Carlens.Contracts.Requests;
using Carlens.Contracts.Responses;

namespace Carlens.Web.Services;

public interface IListingAnalysisApiClient
{
    Task<IReadOnlyList<ListingAnalysisResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ListingAnalysisResponse?> GetByIdAsync(Guid id,CancellationToken cancellationToken = default);

    Task<ListingAnalysisResponse> CreateAsync(CreateListingAnalysisRequest request,CancellationToken cancellationToken = default);

    Task<ListingAnalysisResponse> CreateManualAsync(
        CreateManualVehicleAnalysisRequest request,
        IReadOnlyList<IFormFile> images,
        CancellationToken cancellationToken = default);

    Task<ListingImageContent?> GetImageAsync(
        Guid imageId,
        CancellationToken cancellationToken = default);
}
