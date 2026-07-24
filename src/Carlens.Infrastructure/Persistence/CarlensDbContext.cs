using Carlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Carlens.Infrastructure.Persistence;

public sealed class CarlensDbContext : DbContext
{
    public CarlensDbContext(DbContextOptions<CarlensDbContext> options) 
        : base(options)
    {

    }

    public DbSet<CarListing> CarListings => Set<CarListing>();
    public DbSet<CarListingImage> CarListingImages => Set<CarListingImage>();
    public DbSet<CarListingSpecification> CarListingSpecifications => Set<CarListingSpecification>();
    public DbSet<CarListingComparable> CarListingComparables => Set<CarListingComparable>();
    public DbSet<ListingAnalysis> ListingAnalyses => Set<ListingAnalysis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CarlensDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
