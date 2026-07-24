using Carlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Carlens.Infrastructure.Persistence.Configurations;

public sealed class CarListingImageConfiguration : IEntityTypeConfiguration<CarListingImage>
{
    public void Configure(EntityTypeBuilder<CarListingImage> builder)
    {
        builder.ToTable(
            "car_listing_images",
            table => table.HasCheckConstraint(
                "CK_car_listing_images_source",
                """
                (("Url" IS NOT NULL AND "Content" IS NULL AND "ContentType" IS NULL)
                OR ("Url" IS NULL AND "Content" IS NOT NULL AND "ContentType" IS NOT NULL))
                """));
        builder.HasKey(image => image.Id);

        builder.Property(image => image.Id).ValueGeneratedNever();
        builder.Property(image => image.CarListingId).IsRequired();
        builder.Property(image => image.Url).HasMaxLength(2000);
        builder.Property(image => image.ContentType).HasMaxLength(50);
        builder.Property(image => image.Content).HasColumnType("bytea");
        builder.Property(image => image.DisplayOrder).IsRequired();

        builder.HasIndex(image => new { image.CarListingId, image.DisplayOrder })
            .IsUnique();
    }
}
