using Carlens.Contracts.Responses;
using Carlens.Application.Common.Reports;
using Carlens.Domain.Entities;
using Carlens.Domain.Enums;

namespace Carlens.Application.Common.Mappings;

public static class ListingAnalysisResponseMapper
{
    public static ListingAnalysisResponse ToResponse(
        this ListingAnalysis analysis,
        CarListing listing)
    {
        var report = CreateReport(analysis, listing);

        return new ListingAnalysisResponse(
            analysis.Id,
            analysis.Status.ToString(),
            ResolveProgressStage(analysis, listing),
            new ListingSummaryResponse(
                listing.Id,
                listing.InputType.ToString(),
                listing.ListingUrl,
                listing.ExternalListingId,
                NullIfEmpty(listing.Title),
                NullIfEmpty(listing.Brand),
                listing.Series,
                NullIfEmpty(listing.Model),
                listing.ModelYear,
                listing.Price,
                listing.Mileage,
                listing.SourceStatus == ListingSourceStatus.Imported
                    ? listing.FuelType.ToString()
                    : null,
                listing.SourceStatus == ListingSourceStatus.Imported
                    ? listing.TransmissionType.ToString()
                    : null,
                listing.SourceStatus == ListingSourceStatus.Imported
                    ? listing.SellerType.ToString()
                    : null,
                listing.Location,
                listing.Images
                    .OrderBy(image => image.DisplayOrder)
                    .Select(image =>
                        image.Url ?? $"/api/listing-images/{image.Id}")
                    .ToList(),
                listing.Images.Count,
                listing.Comparables.Count),
            report,
            analysis.ErrorMessage,
            analysis.CreatedAtUtc,
            analysis.CompletedAtUtc,
            new AnalysisUsageResponse(
                analysis.InputTokens,
                analysis.OutputTokens,
                analysis.AnalyzedImageCount,
                analysis.EstimatedCostUsd));
    }

    private static VehicleAnalysisReportResponse? CreateReport(
        ListingAnalysis analysis,
        CarListing listing)
    {
        if (string.IsNullOrWhiteSpace(analysis.Summary))
        {
            return null;
        }

        return new VehicleAnalysisReportResponse(
            (analysis.Recommendation ??
             PurchaseRecommendation.ConsiderAfterInspection).ToString(),
            (analysis.PriceAssessment ??
             PriceAssessment.InsufficientData).ToString(),
            analysis.ConfidenceScore ?? 35,
            ListingAnalysisReportGrounding.SanitizeSummary(
                analysis.Summary,
                listing.DamageInformation),
            analysis.EstimatedMarketPrice,
            analysis.EstimatedMarketPriceMin,
            analysis.EstimatedMarketPriceMax,
            CreateGroundedReportItems(
                analysis.PriceEvaluation,
                "Piyasa karşılaştırması bulunmuyor.",
                listing.DamageInformation),
            CreateGroundedReportItems(
                analysis.MileageEvaluation,
                "Kilometre değerlendirmesi bulunmuyor.",
                listing.DamageInformation),
            CreateGroundedReportItems(
                analysis.KnownIssues,
                "Kronik sorun değerlendirmesi bulunmuyor.",
                listing.DamageInformation),
            CreateGroundedReportItems(
                analysis.BuyReasoning,
                analysis.Summary,
                listing.DamageInformation),
            CreateGroundedReportItems(
                analysis.RiskNotes,
                "Belirgin risk notu bulunmuyor.",
                listing.DamageInformation),
            CreateGroundedReportItems(
                analysis.InspectionChecklist,
                "Satın almadan önce bağımsız ekspertiz yaptırılmalı.",
                listing.DamageInformation));
    }

    private static string ResolveProgressStage(
        ListingAnalysis analysis,
        CarListing listing)
    {
        return analysis.Status switch
        {
            AnalysisStatus.Pending => "Queued",
            AnalysisStatus.Completed => "Completed",
            AnalysisStatus.Failed => "Failed",
            AnalysisStatus.Processing
                when listing.SourceStatus == ListingSourceStatus.Pending => "ReadingListing",
            AnalysisStatus.Processing => "AnalyzingVehicle",
            _ => "Queued"
        };
    }

    private static string? NullIfEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static IReadOnlyList<string> CreateReportItems(
        string? value,
        string fallback)
    {
        var source = string.IsNullOrWhiteSpace(value) ? fallback : value;

        return source
            .ReplaceLineEndings("\n")
            .Split(
                '\n',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Select(item => item.TrimStart('-', '*', '•', ' '))
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();
    }

    private static IReadOnlyList<string> CreateGroundedReportItems(
        string? value,
        string fallback,
        string? damageInformation)
    {
        return ListingAnalysisReportGrounding.SanitizeItems(
            CreateReportItems(value, fallback),
            damageInformation);
    }
}
