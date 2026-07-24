using Carlens.Domain.Enums;

namespace Carlens.Application.DTOs;

public sealed record ListingAiAnalysisResult(
    PurchaseRecommendation Recommendation,
    PriceAssessment PriceAssessment,
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
    IReadOnlyList<string> InspectionChecklist,
    int InputTokens,
    int OutputTokens,
    int AnalyzedImageCount,
    decimal EstimatedCostUsd);
