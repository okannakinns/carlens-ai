using Carlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Carlens.Infrastructure.Persistence.Configurations;

public sealed class CarListingConfiguration : IEntityTypeConfiguration<CarListing>
{
    public void Configure(EntityTypeBuilder<CarListing> builder)
    {
        builder.ToTable("car_listings");
        builder.HasKey(listing => listing.Id);

        builder.Property(listing => listing.Id).ValueGeneratedNever();
        builder.Property(listing => listing.ListingUrl).HasMaxLength(1000);
        builder.Property(listing => listing.ExternalListingId).HasMaxLength(100);
        builder.Property(listing => listing.Title).IsRequired().HasMaxLength(300);
        builder.Property(listing => listing.Brand).IsRequired().HasMaxLength(100);
        builder.Property(listing => listing.Series).HasMaxLength(100);
        builder.Property(listing => listing.Model).IsRequired().HasMaxLength(150);
        builder.Property(listing => listing.Price).HasPrecision(18, 2);
        builder.Property(listing => listing.Location).HasMaxLength(300);
        builder.Property(listing => listing.Description).HasColumnType("text");
        builder.Property(listing => listing.DamageInformation).HasColumnType("text");
        builder.Property(listing => listing.ImportError).HasMaxLength(2000);
        builder.Property(listing => listing.CreatedAtUtc).IsRequired();
        builder.Property(listing => listing.ImportedAtUtc);
        builder.Property(listing => listing.IsDeleted).IsRequired();
        builder.Property(listing => listing.DeletedAtUtc);

        builder.Property(listing => listing.FuelType)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(listing => listing.TransmissionType)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(listing => listing.SellerType)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(listing => listing.SourceStatus)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(listing => listing.InputType)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.HasMany(listing => listing.Images)
            .WithOne()
            .HasForeignKey(image => image.CarListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(listing => listing.Specifications)
            .WithOne()
            .HasForeignKey(specification => specification.CarListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(listing => listing.Comparables)
            .WithOne()
            .HasForeignKey(comparable => comparable.CarListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(listing => listing.Images)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(listing => listing.Specifications)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(listing => listing.Comparables)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(listing => listing.ExternalListingId);
        builder.HasQueryFilter(listing => !listing.IsDeleted);
    }
}
