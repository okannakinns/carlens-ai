namespace Carlens.Application.DTOs;

public sealed record UploadedVehicleImage(
    string FileName,
    byte[] Content);
