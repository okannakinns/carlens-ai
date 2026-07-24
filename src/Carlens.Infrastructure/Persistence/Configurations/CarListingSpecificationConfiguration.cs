using Carlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Carlens.Infrastructure.Persistence.Configurations;

public sealed class CarListingSpecificationConfiguration
    : IEntityTypeConfiguration<CarListingSpecification>
{
    public void Configure(EntityTypeBuilder<CarListingSpecification> builder)
    {
        builder.ToTable("car_listing_specifications");
        builder.HasKey(specification => specification.Id);

        builder.Property(specification => specification.Id).ValueGeneratedNever();
        builder.Property(specification => specification.CarListingId).IsRequired();
        builder.Property(specification => specification.Name).IsRequired().HasMaxLength(200);
        builder.Property(specification => specification.Value).IsRequired().HasMaxLength(1000);
        builder.Property(specification => specification.DisplayOrder).IsRequired();

        builder.HasIndex(specification => new
        {
            specification.CarListingId,
            specification.DisplayOrder
        }).IsUnique();
    }
}
