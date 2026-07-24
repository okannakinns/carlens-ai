namespace Carlens.Contracts.Responses;

public sealed record ListingAnalysisResponse(
    Guid AnalysisId,
    string Status,
    string ProgressStage,
    ListingSummaryResponse Listing,
    VehicleAnalysisReportResponse? Report,
    string? ErrorMessage,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    AnalysisUsageResponse Usage);

public sealed record ListingSummaryResponse(
    Guid CarListingId,
    string InputType,
    string? ListingUrl,
    string? ExternalListingId,
    string? Title,
    string? Brand,
    string? Series,
    string? Model,
    int? ModelYear,
    decimal? Price,
    int? Mileage,
    string? FuelType,
    string? TransmissionType,
    string? SellerType,
    string? Location,
    IReadOnlyList<string> ImageUrls,
    int TotalImageCount,
    int ComparableCount);

public sealed record VehicleAnalysisReportResponse(
    string Recommendation,
    string PriceAssessment,
    int ConfidenceScore,
    string Summary,
    decimal? EstimatedMarketPrice,
    decimal? EstimatedMarketPriceMin,
    decimal? EstimatedMarketPriceMax,
    IReadOnlyList<string> PriceEvaluation,
    IReadOnlyList<string> MileageEvaluation,
    IReadOnlyList<string> KnownIssues,
    IReadOnlyList<string> BuyReasoning,
    IReadOnlyList<string> RiskNotes,
    IReadOnlyList<string> InspectionChecklist);

public sealed record AnalysisUsageResponse(
    int InputTokens,
    int OutputTokens,
    int AnalyzedImageCount,
    decimal? EstimatedCostUsd);
