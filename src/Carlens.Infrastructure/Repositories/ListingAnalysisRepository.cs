using Carlens.Application.Interfaces;
using Carlens.Domain.Entities;
using Carlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Carlens.Infrastructure.Repositories
{
    public sealed class ListingAnalysisRepository : IListingAnalysisRepository
    {
        private readonly CarlensDbContext _context;

        public ListingAnalysisRepository(CarlensDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ListingAnalysis listingAnalysis, CancellationToken cancellationToken = default)
        {
           await _context.AddAsync(listingAnalysis, cancellationToken);
           await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ListingAnalysis>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.ListingAnalyses
            .OrderByDescending(analysis => analysis.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        }

        public async Task<ListingAnalysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
           return await _context.ListingAnalyses.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        }

        public async Task UpdateAsync(ListingAnalysis listingAnalysis, CancellationToken cancellationToken = default)
        {
             _context.Update(listingAnalysis);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
