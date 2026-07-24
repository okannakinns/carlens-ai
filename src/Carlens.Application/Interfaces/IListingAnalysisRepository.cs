using Carlens.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Carlens.Application.Interfaces
{
    public interface IListingAnalysisRepository
    {
        Task<IReadOnlyList<ListingAnalysis>> GetAllAsync(CancellationToken cancellationToken = default);
        Task AddAsync(ListingAnalysis listingAnalysis, CancellationToken cancellationToken = default);
        Task<ListingAnalysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task UpdateAsync(ListingAnalysis listingAnalysis, CancellationToken cancellationToken = default);
    }
}
