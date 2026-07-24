using Carlens.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Carlens.Application.Interfaces
{
public interface ICarListingRepository
{
    Task AddAsync(CarListing carListing, CancellationToken cancellationToken = default);
    Task<CarListing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CarListing?> GetByIdWithoutImagesAsync(
        Guid id,
        CancellationToken cancellationToken = default);
    Task<CarListingImage?> GetImageByIdAsync(
        Guid imageId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, CarListing>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);
    Task UpdateAsync(CarListing carListing, CancellationToken cancellationToken = default);
}
}
