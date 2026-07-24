using Carlens.Domain.Common;
using Carlens.Domain.Enums;
using Carlens.Domain.ValueObjects;

namespace Carlens.Domain.Entities;

public sealed class ListingAnalysis : BaseEntity
{
    public Guid Id { get; private set; }

    public Guid CarListingId { get; private set; }

    public AnalysisStatus Status { get; private set; }

    public string? Summary { get; private set; }

    public decimal? EstimatedMarketPrice { get; private set; }

    public decimal? EstimatedMarketPriceMin { get; private set; }

    public decimal? EstimatedMarketPriceMax { get; private set; }

    public PurchaseRecommendation? Recommendation { get; private set; }

    public PriceAssessment? PriceAssessment { get; private set; }

    public int? ConfidenceScore { get; private set; }

    public string? PriceEvaluation { get; private set; }

    public string? MileageEvaluation { get; private set; }

    public string? KnownIssues { get; private set; }

    public string? BuyReasoning { get; private set; }

    public string? RiskNotes { get; private set; }

    public string? InspectionChecklist { get; private set; }

    public string? ErrorMessage { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public int InputTokens { get; private set; }

    public int OutputTokens { get; private set; }

    public int AnalyzedImageCount { get; private set; }

    public decimal? EstimatedCostUsd { get; private set; }

    private ListingAnalysis()
    {
    }

    public ListingAnalysis(Guid carListingId)
    {

        if(carListingId == Guid.Empty)
        {
            throw new ArgumentException("Car listing id is required.", nameof(carListingId));
        }
    
        Id = Guid.NewGuid();
        CarListingId = carListingId;
        Status = AnalysisStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsProcessing()
    {
        Status = AnalysisStatus.Processing;
    }

    public void MarkAsCompleted(
        ListingAnalysisReport report,
        int inputTokens = 0,
        int outputTokens = 0,
        int analyzedImageCount = 0,
        decimal? estimatedCostUsd = null)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (string.IsNullOrWhiteSpace(report.Summary))
        {
            throw new ArgumentException("Summary is required.", nameof(report));
        }

        if (report.ConfidenceScore is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(report));
        }

        var priceValues = new[]
        {
            report.EstimatedMarketPrice,
            report.EstimatedMarketPriceMin,
            report.EstimatedMarketPriceMax
        };

        if (priceValues.Any(value => value is < 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(report),
                "Estimated market prices cannot be negative.");
        }

        if (inputTokens < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inputTokens));
        }

        if (outputTokens < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(outputTokens));
        }

        if (analyzedImageCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(analyzedImageCount));
        }

        if (estimatedCostUsd is not null && estimatedCostUsd < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(estimatedCostUsd));
        }

        Status = AnalysisStatus.Completed;
        Summary = report.Summary.Trim();
        Recommendation = report.Recommendation;
        PriceAssessment = report.PriceAssessment;
        ConfidenceScore = report.ConfidenceScore;
        EstimatedMarketPrice = report.EstimatedMarketPrice;
        EstimatedMarketPriceMin = report.EstimatedMarketPriceMin;
        EstimatedMarketPriceMax = report.EstimatedMarketPriceMax;
        PriceEvaluation = NormalizeRequired(report.PriceEvaluation, nameof(report.PriceEvaluation));
        MileageEvaluation = NormalizeRequired(report.MileageEvaluation, nameof(report.MileageEvaluation));
        KnownIssues = NormalizeRequired(report.KnownIssues, nameof(report.KnownIssues));
        BuyReasoning = NormalizeRequired(report.BuyReasoning, nameof(report.BuyReasoning));
        RiskNotes = NormalizeRequired(report.RiskNotes, nameof(report.RiskNotes));
        InspectionChecklist = NormalizeRequired(
            report.InspectionChecklist,
            nameof(report.InspectionChecklist));
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        AnalyzedImageCount = analyzedImageCount;
        EstimatedCostUsd = estimatedCostUsd;
        CompletedAtUtc = DateTime.UtcNow;
        ErrorMessage = null;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Analysis report section is required.", parameterName);
        }

        return value.Trim();
    }

    public void MarkAsFailed(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException("Error message is required.", nameof(errorMessage));
        }

        Status = AnalysisStatus.Failed;
        ErrorMessage = errorMessage.Trim();
        CompletedAtUtc = DateTime.UtcNow;
    }
}
