using Carlens.Application.Interfaces;
using Carlens.Domain.Entities;
using Carlens.Domain.Enums;
using Carlens.Domain.ValueObjects;

namespace Carlens.AiWorker.Services;

public sealed class ListingAnalysisProcessor
{
    private readonly IListingAnalysisRepository _listingAnalysisRepository;
    private readonly ICarListingRepository _carListingRepository;
    private readonly IListingSourceReader _listingSourceReader;
    private readonly IListingAnalysisAiService _listingAnalysisAiService;
    private readonly ILogger<ListingAnalysisProcessor> _logger;

    public ListingAnalysisProcessor(
        IListingAnalysisRepository listingAnalysisRepository,
        ICarListingRepository carListingRepository,
        IListingSourceReader listingSourceReader,
        IListingAnalysisAiService listingAnalysisAiService,
        ILogger<ListingAnalysisProcessor> logger)
    {
        _listingAnalysisRepository = listingAnalysisRepository;
        _carListingRepository = carListingRepository;
        _listingSourceReader = listingSourceReader;
        _listingAnalysisAiService = listingAnalysisAiService;
        _logger = logger;
    }

    public async Task ProcessAsync(
        Guid analysisId,
        CancellationToken cancellationToken = default)
    {
        var analysis = await _listingAnalysisRepository.GetByIdAsync(
            analysisId,
            cancellationToken);

        if (analysis is null)
        {
            return;
        }

        if (analysis.Status == AnalysisStatus.Completed)
        {
            _logger.LogInformation(
                "Analysis {AnalysisId} is already completed. Duplicate message skipped.",
                analysisId);
            return;
        }

        try
        {
            analysis.MarkAsProcessing();
            await _listingAnalysisRepository.UpdateAsync(analysis, cancellationToken);

            var carListing = await _carListingRepository.GetByIdAsync(
                analysis.CarListingId,
                cancellationToken);

            if (carListing is null)
            {
                throw new InvalidOperationException("Araç ilan kaydı bulunamadı.");
            }

            if (carListing.InputType == ListingInputType.Url &&
                carListing.SourceStatus != ListingSourceStatus.Imported)
            {
                if (string.IsNullOrWhiteSpace(carListing.ListingUrl))
                {
                    throw new InvalidOperationException(
                        "İlan bağlantısı bulunamadı.");
                }

                var sourceData = await _listingSourceReader.ReadAsync(
                    carListing.ListingUrl,
                    cancellationToken);

                carListing.ApplySourceData(
                    sourceData.ExternalListingId,
                    sourceData.Title,
                    sourceData.Brand,
                    sourceData.Series,
                    sourceData.Model,
                    sourceData.ModelYear,
                    sourceData.Price,
                    sourceData.Mileage,
                    sourceData.FuelType,
                    sourceData.TransmissionType,
                    sourceData.SellerType,
                    sourceData.Location,
                    sourceData.Description,
                    sourceData.DamageInformation,
                    sourceData.ImageUrls,
                    sourceData.Specifications,
                    sourceData.Comparables.Select(comparable =>
                        new CarListing.CarListingComparableCandidate(
                            comparable.ModelName,
                            comparable.Title,
                            comparable.ModelYear,
                            comparable.Mileage,
                            comparable.Price,
                            comparable.Location,
                            comparable.Url)));

                await _carListingRepository.UpdateAsync(carListing, cancellationToken);
            }

            var result = await _listingAnalysisAiService.AnalyzeAsync(
                carListing,
                cancellationToken);

            var report = new ListingAnalysisReport(
                result.Recommendation,
                result.PriceAssessment,
                result.ConfidenceScore,
                result.Summary,
                result.EstimatedMarketPrice,
                result.EstimatedMarketPriceMin,
                result.EstimatedMarketPriceMax,
                JoinReportItems(result.PriceEvaluation),
                JoinReportItems(result.MileageEvaluation),
                JoinReportItems(result.KnownIssues),
                JoinReportItems(result.BuyReasoning),
                JoinReportItems(result.RiskNotes),
                JoinReportItems(result.InspectionChecklist));

            analysis.MarkAsCompleted(
                report,
                result.InputTokens,
                result.OutputTokens,
                result.AnalyzedImageCount,
                result.EstimatedCostUsd);

            await _listingAnalysisRepository.UpdateAsync(analysis, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Analysis failed for analysis {AnalysisId}.",
                analysisId);

            analysis.MarkAsFailed(
                exception is InvalidOperationException
                    ? exception.Message
                    : "Araç verileri okunamadı veya AI analizi tamamlanamadı.");

            await _listingAnalysisRepository.UpdateAsync(
                analysis,
                CancellationToken.None);
        }
    }

    private static string JoinReportItems(IEnumerable<string> items)
    {
        return string.Join(
            Environment.NewLine,
            items
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item)));
    }
}
