using Carlens.Application.Interfaces;
using Carlens.Domain.Entities;
using Carlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Carlens.Infrastructure.Repositories;

public sealed class CarListingRepository : ICarListingRepository
{
    private readonly CarlensDbContext _context;

    public CarListingRepository(CarlensDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CarListing carListing, CancellationToken cancellationToken = default)
    {
       await _context.AddAsync(carListing, cancellationToken);
       await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<CarListing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CarListings
            .Include(listing => listing.Images)
            .Include(listing => listing.Specifications)
            .Include(listing => listing.Comparables)
            .AsSplitQuery()
            .FirstOrDefaultAsync(listing => listing.Id == id, cancellationToken);
    }

    public async Task<CarListing?> GetByIdWithoutImagesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.CarListings
            .Include(listing => listing.Specifications)
            .Include(listing => listing.Comparables)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(listing => listing.Id == id, cancellationToken);
    }

    public async Task<CarListingImage?> GetImageByIdAsync(
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CarListingImages
            .AsNoTracking()
            .FirstOrDefaultAsync(image => image.Id == imageId, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, CarListing>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, CarListing>();
        }

        var listings = await _context.CarListings
            .Where(listing => ids.Contains(listing.Id))
            .Include(listing => listing.Specifications)
            .Include(listing => listing.Comparables)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return listings.ToDictionary(listing => listing.Id);
    }

    public async Task UpdateAsync(
        CarListing carListing,
        CancellationToken cancellationToken = default)
    {
        _context.Update(carListing);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
