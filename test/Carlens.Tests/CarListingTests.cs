using Carlens.Domain.Entities;
using Carlens.Domain.Enums;

namespace Carlens.Tests;

public sealed class CarListingTests
{
    [Fact]
    public void ApplySourceData_LimitsStoredImagesAndRemovesDuplicates()
    {
        var listing = new CarListing(
            "https://www.arabam.com/ilan/test-listing/123456");
        var imageUrls = Enumerable.Range(1, 60)
            .Select(index => $"https://cdn.example.com/car_{index}.jpg")
            .Append("https://cdn.example.com/car_1.jpg");

        listing.ApplySourceData(
            "123456",
            "Test ilanı",
            "Volkswagen",
            "Polo",
            "1.6 TDI",
            2010,
            575000m,
            245000,
            FuelType.Diesel,
            TransmissionType.Manual,
            SellerType.Dealer,
            "Antalya",
            "Test açıklaması",
            "Test hasar bilgisi",
            imageUrls,
            [],
            []);

        Assert.Equal(50, listing.Images.Count);
        Assert.Equal(50, listing.Images.Select(image => image.Url).Distinct().Count());
        Assert.Equal(ListingSourceStatus.Imported, listing.SourceStatus);
    }

    [Fact]
    public void ApplySourceData_LimitsComparablesAndRemovesDuplicates()
    {
        var listing = new CarListing(
            "https://www.arabam.com/ilan/test-listing/123456");
        var comparables = Enumerable.Range(1, 30)
            .Select(index => new CarListing.CarListingComparableCandidate(
                "Polo 1.6 TDI",
                $"Karşılaştırma {index}",
                2010,
                200000 + index,
                500000 + index,
                "İstanbul",
                $"https://www.arabam.com/ilan/comparable/{index}"))
            .Append(new CarListing.CarListingComparableCandidate(
                "Polo 1.6 TDI",
                "Tekrar",
                2010,
                200001,
                500001,
                "İstanbul",
                "https://www.arabam.com/ilan/comparable/1"));

        listing.ApplySourceData(
            "123456",
            "Test ilanı",
            "Volkswagen",
            "Polo",
            "1.6 TDI",
            2010,
            575000m,
            245000,
            FuelType.Diesel,
            TransmissionType.Manual,
            SellerType.Dealer,
            "Antalya",
            null,
            null,
            [],
            [],
            comparables);

        Assert.Equal(24, listing.Comparables.Count);
        Assert.Equal(
            24,
            listing.Comparables.Select(comparable => comparable.Url).Distinct().Count());
    }
}
