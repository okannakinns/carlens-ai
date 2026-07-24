using Carlens.Application.DTOs;
using Carlens.Domain.Enums;

namespace Carlens.Application.Features.Listings.Commands;

public sealed record CreateManualVehicleAnalysisCommand(
    string Brand,
    string? Series,
    string Model,
    int ModelYear,
    decimal? Price,
    int Mileage,
    FuelType FuelType,
    TransmissionType TransmissionType,
    string? Location,
    string? Description,
    string? DamageInformation,
    IReadOnlyList<UploadedVehicleImage> Images);
