using Carlens.Application.Common.Mappings;
using Carlens.Application.Common.Reports;
using Carlens.Domain.Entities;
using Carlens.Domain.Enums;
using Carlens.Domain.ValueObjects;

namespace Carlens.Tests;

public sealed class ListingAnalysisResponseMapperTests
{
    [Fact]
    public void ToResponse_ConvertsStoredReportLinesToStructuredItems()
    {
        var listing = new CarListing(
            "https://www.arabam.com/ilan/test-listing/123456");
        listing.ApplySourceData(
            "123456",
            "Test ilanı",
            "Audi",
            "Q3",
            "1.4 TFSi",
            2016,
            1780000m,
            110000,
            FuelType.Gasoline,
            TransmissionType.SemiAutomatic,
            SellerType.Dealer,
            "Ankara",
            null,
            null,
            [],
            [],
            []);

        var analysis = new ListingAnalysis(listing.Id);
        analysis.MarkAsCompleted(
            new ListingAnalysisReport(
                PurchaseRecommendation.ConsiderAfterInspection,
                PriceAssessment.Fair,
                78,
                "Ekspertiz sonrası değerlendirilebilir.",
                1660000m,
                1475000m,
                1799000m,
                "- Medyan fiyat 1.660.000 TL.\n• İlan üst banda yakın.",
                "Yıllık kullanım makul.",
                "S tronic bakım geçmişi görülmeli.",
                "Bakımlıysa alınabilir.",
                "Tramer kaydı doğrulanmalı.",
                "Şanzıman geçişleri kontrol edilmeli."),
            100,
            50,
            4,
            0.01m);

        var response = analysis.ToResponse(listing);

        Assert.NotNull(response.Report);
        Assert.Equal(2, response.Report.PriceEvaluation.Count);
        Assert.Equal("Medyan fiyat 1.660.000 TL.", response.Report.PriceEvaluation[0]);
        Assert.Equal("İlan üst banda yakın.", response.Report.PriceEvaluation[1]);
    }

    [Fact]
    public void Grounding_RemovesUnsupportedDamageContradiction()
    {
        const string damageInformation = """
            Orijinal: Sağ Arka Çamurluk, Arka Kaput, Tavan
            Lokal boyalı: Yok
            Boyalı: Yok
            Değişmiş: Yok
            Belirtilmemiş: Yok
            Tramer: Tramer tutarı yok
            """;
        const string summary =
            "Araç temiz görünüyor. Boya ve değişen beyanı açıkça çelişkili. Ekspertiz şart.";
        string[] riskNotes =
        [
            "Kaporta beyanı kendi içinde tutarsız.",
            "Şanzıman bakım geçmişi görülmeli."
        ];

        var sanitizedSummary =
            ListingAnalysisReportGrounding.SanitizeSummary(
                summary,
                damageInformation);
        var sanitizedItems =
            ListingAnalysisReportGrounding.SanitizeItems(
                riskNotes,
                damageInformation);

        Assert.DoesNotContain("çelişkili", sanitizedSummary);
        Assert.DoesNotContain(
            sanitizedItems,
            item => item.Contains("tutarsız", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            sanitizedItems,
            item => item.Contains("tüm parçaların orijinal", StringComparison.OrdinalIgnoreCase));
    }
}
