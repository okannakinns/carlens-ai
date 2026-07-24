using Carlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Carlens.Infrastructure.Persistence.Configurations;

public sealed class ListingAnalysisConfiguration
    : IEntityTypeConfiguration<ListingAnalysis>
{
    public void Configure(EntityTypeBuilder<ListingAnalysis> builder)
    {
        builder.ToTable("listing_analyses");
        builder.HasKey(analysis => analysis.Id);

        builder.Property(analysis => analysis.Id).ValueGeneratedNever();
        builder.Property(analysis => analysis.CarListingId).IsRequired();
        builder.Property(analysis => analysis.Summary).HasMaxLength(4000);
        builder.Property(analysis => analysis.EstimatedMarketPrice).HasPrecision(18, 2);
        builder.Property(analysis => analysis.EstimatedMarketPriceMin).HasPrecision(18, 2);
        builder.Property(analysis => analysis.EstimatedMarketPriceMax).HasPrecision(18, 2);
        builder.Property(analysis => analysis.ConfidenceScore);
        builder.Property(analysis => analysis.PriceEvaluation).HasColumnType("text");
        builder.Property(analysis => analysis.MileageEvaluation).HasColumnType("text");
        builder.Property(analysis => analysis.KnownIssues).HasColumnType("text");
        builder.Property(analysis => analysis.BuyReasoning).HasColumnType("text");
        builder.Property(analysis => analysis.RiskNotes).HasColumnType("text");
        builder.Property(analysis => analysis.InspectionChecklist).HasColumnType("text");
        builder.Property(analysis => analysis.ErrorMessage).HasMaxLength(2000);
        builder.Property(analysis => analysis.CreatedAtUtc).IsRequired();
        builder.Property(analysis => analysis.CompletedAtUtc);
        builder.Property(analysis => analysis.InputTokens).IsRequired();
        builder.Property(analysis => analysis.OutputTokens).IsRequired();
        builder.Property(analysis => analysis.AnalyzedImageCount).IsRequired();
        builder.Property(analysis => analysis.EstimatedCostUsd).HasPrecision(18, 8);
        builder.Property(analysis => analysis.IsDeleted).IsRequired();
        builder.Property(analysis => analysis.DeletedAtUtc);

        builder.Property(analysis => analysis.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(analysis => analysis.Recommendation)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(analysis => analysis.PriceAssessment)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne<CarListing>()
            .WithMany()
            .HasForeignKey(analysis => analysis.CarListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(analysis => !analysis.IsDeleted);
    }
}
