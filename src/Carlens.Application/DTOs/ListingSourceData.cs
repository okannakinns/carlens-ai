using Carlens.Domain.Enums;

namespace Carlens.Application.DTOs;

public sealed record ListingSourceData(
    string SourceUrl,
    string ExternalListingId,
    string Title,
    string Brand,
    string? Series,
    string Model,
    int? ModelYear,
    decimal? Price,
    int? Mileage,
    FuelType FuelType,
    TransmissionType TransmissionType,
    SellerType SellerType,
    string? Location,
    string? Description,
    string? DamageInformation,
    IReadOnlyDictionary<string, string> Specifications,
    IReadOnlyList<string> ImageUrls,
    IReadOnlyList<ListingComparableData> Comparables);
