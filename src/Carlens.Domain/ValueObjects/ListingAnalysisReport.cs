using Carlens.Domain.Enums;

namespace Carlens.Domain.ValueObjects;

public sealed record ListingAnalysisReport(
    PurchaseRecommendation Recommendation,
    PriceAssessment PriceAssessment,
    int ConfidenceScore,
    string Summary,
    decimal? EstimatedMarketPrice,
    decimal? EstimatedMarketPriceMin,
    decimal? EstimatedMarketPriceMax,
    string PriceEvaluation,
    string MileageEvaluation,
    string KnownIssues,
    string BuyReasoning,
    string RiskNotes,
    string InspectionChecklist);
