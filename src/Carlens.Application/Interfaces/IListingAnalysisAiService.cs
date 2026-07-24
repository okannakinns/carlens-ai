using Carlens.Application.DTOs;
using Carlens.Domain.Entities;

namespace Carlens.Application.Interfaces;

public interface IListingAnalysisAiService
{
    Task<ListingAiAnalysisResult> AnalyzeAsync(
        CarListing carListing,
        CancellationToken cancellationToken = default);
}
