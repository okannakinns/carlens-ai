using Carlens.Domain.Entities;
using Carlens.Domain.Enums;
using Carlens.Domain.ValueObjects;

namespace Carlens.Tests;

public sealed class ListingAnalysisTests
{
    [Fact]
    public void MarkAsCompleted_StoresUsageAndCostMetrics()
    {
        var analysis = new ListingAnalysis(Guid.NewGuid());
        var report = new ListingAnalysisReport(
            PurchaseRecommendation.ConsiderAfterInspection,
            PriceAssessment.Fair,
            78,
            "Temkinli biçimde değerlendirilebilir.",
            575000m,
            550000m,
            600000m,
            "Fiyat piyasa bandında.",
            "Kilometre yüksek; bakım kayıtları görülmeli.",
            "Model ailesine özgü riskler kontrol edilmeli.",
            "Ekspertiz sonucu temizse değerlendirilebilir.",
            "Satıcı beyanı doğrulanmalı.",
            "Soğuk çalıştırma ve alt takım kontrolü yapılmalı.");

        analysis.MarkAsCompleted(
            report,
            inputTokens: 3992,
            outputTokens: 362,
            analyzedImageCount: 8,
            estimatedCostUsd: 0.004623m);

        Assert.Equal(AnalysisStatus.Completed, analysis.Status);
        Assert.Equal(3992, analysis.InputTokens);
        Assert.Equal(362, analysis.OutputTokens);
        Assert.Equal(8, analysis.AnalyzedImageCount);
        Assert.Equal(0.004623m, analysis.EstimatedCostUsd);
    }
}
