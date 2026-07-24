using Carlens.Application.DTOs;

namespace Carlens.Application.Interfaces;

public interface IListingSourceReader
{
    Task<ListingSourceData> ReadAsync(
        string listingUrl,
        CancellationToken cancellationToken = default);
}
