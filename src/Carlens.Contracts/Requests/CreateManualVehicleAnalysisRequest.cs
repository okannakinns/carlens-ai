namespace Carlens.Contracts.Requests;

public sealed class CreateManualVehicleAnalysisRequest
{
    public string Brand { get; set; } = string.Empty;
    public string? Series { get; set; }
    public string Model { get; set; } = string.Empty;
    public int ModelYear { get; set; }
    public decimal? Price { get; set; }
    public int Mileage { get; set; }
    public int FuelType { get; set; }
    public int TransmissionType { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? DamageInformation { get; set; }
}
