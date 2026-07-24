using Carlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Carlens.Infrastructure.Persistence.Configurations;

public sealed class CarListingComparableConfiguration
    : IEntityTypeConfiguration<CarListingComparable>
{
    public void Configure(EntityTypeBuilder<CarListingComparable> builder)
    {
        builder.ToTable("car_listing_comparables");
        builder.HasKey(comparable => comparable.Id);

        builder.Property(comparable => comparable.Id).ValueGeneratedNever();
        builder.Property(comparable => comparable.CarListingId).IsRequired();
        builder.Property(comparable => comparable.ModelName).IsRequired().HasMaxLength(200);
        builder.Property(comparable => comparable.Title).IsRequired().HasMaxLength(400);
        builder.Property(comparable => comparable.Price).HasPrecision(18, 2).IsRequired();
        builder.Property(comparable => comparable.Location).HasMaxLength(300);
        builder.Property(comparable => comparable.Url).IsRequired().HasMaxLength(1500);
        builder.Property(comparable => comparable.DisplayOrder).IsRequired();

        builder.HasIndex(comparable => new
        {
            comparable.CarListingId,
            comparable.DisplayOrder
        }).IsUnique();
    }
}
