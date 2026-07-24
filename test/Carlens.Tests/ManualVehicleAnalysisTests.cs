using Carlens.Application.Common.Mappings;
using Carlens.Application.DTOs;
using Carlens.Application.Features.Listings.Commands;
using Carlens.Domain.Entities;
using Carlens.Domain.Enums;

namespace Carlens.Tests;

public sealed class ManualVehicleAnalysisTests
{
    private static readonly byte[] JpegContent =
        [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

    [Fact]
    public void CreateManual_CreatesImportedListingWithUploadedImage()
    {
        var listing = CreateManualListing();

        Assert.Equal(ListingInputType.Manual, listing.InputType);
        Assert.Equal(ListingSourceStatus.Imported, listing.SourceStatus);
        Assert.Null(listing.ListingUrl);
        Assert.Equal("2020 Volkswagen Golf 1.6 TDI Comfortline", listing.Title);
        Assert.Single(listing.Images);

        var image = listing.Images.Single();
        Assert.Null(image.Url);
        Assert.Equal("image/jpeg", image.ContentType);
        Assert.Equal(JpegContent, image.Content);
    }

    [Fact]
    public async Task Validator_AcceptsValidManualVehicle()
    {
        var validator = new CreateManualVehicleAnalysisCommandValidator();
        var command = CreateCommand(
            [new UploadedVehicleImage("front.jpg", JpegContent)]);

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validator_RejectsInvalidImageSignature()
    {
        var validator = new CreateManualVehicleAnalysisCommandValidator();
        var command = CreateCommand(
            [new UploadedVehicleImage("fake.jpg", [0x01, 0x02, 0x03])]);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.ErrorMessage.Contains("JPEG", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Validator_RejectsMoreThanFiveImages()
    {
        var validator = new CreateManualVehicleAnalysisCommandValidator();
        var images = Enumerable.Range(1, 6)
            .Select(index => new UploadedVehicleImage(
                $"vehicle-{index}.jpg",
                JpegContent))
            .ToList();

        var result = await validator.ValidateAsync(CreateCommand(images));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.ErrorMessage.Contains("1 ile 5", StringComparison.Ordinal));
    }

    [Fact]
    public void ResponseMapper_UsesLocalImageEndpointForManualImage()
    {
        var listing = CreateManualListing();
        var analysis = new ListingAnalysis(listing.Id);

        var response = analysis.ToResponse(listing);

        Assert.Equal("Manual", response.Listing.InputType);
        Assert.Null(response.Listing.ListingUrl);
        Assert.Single(response.Listing.ImageUrls);
        Assert.StartsWith(
            "/api/listing-images/",
            response.Listing.ImageUrls.Single(),
            StringComparison.Ordinal);
    }

    private static CarListing CreateManualListing()
    {
        return CarListing.CreateManual(
            "Volkswagen",
            "Golf",
            "1.6 TDI Comfortline",
            2020,
            1_250_000m,
            85_000,
            FuelType.Diesel,
            TransmissionType.Automatic,
            "İstanbul",
            "Bakımları düzenli yapıldı.",
            "Boya ve değişen yok.",
            [new CarListing.CarListingImageCandidate("image/jpeg", JpegContent)]);
    }

    private static CreateManualVehicleAnalysisCommand CreateCommand(
        IReadOnlyList<UploadedVehicleImage> images)
    {
        return new CreateManualVehicleAnalysisCommand(
            "Volkswagen",
            "Golf",
            "1.6 TDI Comfortline",
            2020,
            1_250_000m,
            85_000,
            FuelType.Diesel,
            TransmissionType.Automatic,
            "İstanbul",
            null,
            null,
            images);
    }
}
