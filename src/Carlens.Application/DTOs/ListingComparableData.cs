namespace Carlens.Application.DTOs;

public sealed record ListingComparableData(
    string ModelName,
    string Title,
    int? ModelYear,
    int? Mileage,
    decimal Price,
    string? Location,
    string Url);
