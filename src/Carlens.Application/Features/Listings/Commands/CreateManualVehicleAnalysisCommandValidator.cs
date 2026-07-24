using Carlens.Application.Common.Images;
using Carlens.Domain.Entities;
using Carlens.Domain.Enums;
using FluentValidation;

namespace Carlens.Application.Features.Listings.Commands;

public sealed class CreateManualVehicleAnalysisCommandValidator
    : AbstractValidator<CreateManualVehicleAnalysisCommand>
{
    public CreateManualVehicleAnalysisCommandValidator()
    {
        RuleFor(command => command.Brand)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Series)
            .MaximumLength(100);

        RuleFor(command => command.Model)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(command => command.ModelYear)
            .InclusiveBetween(1900, DateTime.UtcNow.Year + 1);

        RuleFor(command => command.Price)
            .GreaterThan(0)
            .LessThanOrEqualTo(1_000_000_000)
            .When(command => command.Price.HasValue);

        RuleFor(command => command.Mileage)
            .InclusiveBetween(0, 2_000_000);

        RuleFor(command => command.FuelType)
            .IsInEnum()
            .NotEqual(FuelType.Unknown);

        RuleFor(command => command.TransmissionType)
            .IsInEnum()
            .NotEqual(TransmissionType.Unknown);

        RuleFor(command => command.Location)
            .MaximumLength(300);

        RuleFor(command => command.Description)
            .MaximumLength(5000);

        RuleFor(command => command.DamageInformation)
            .MaximumLength(3000);

        RuleFor(command => command)
            .Must(command => string.Join(
                    ' ',
                    new[]
                    {
                        command.ModelYear.ToString(),
                        command.Brand,
                        command.Series,
                        command.Model
                    }.Where(value => !string.IsNullOrWhiteSpace(value)))
                .Length <= 300)
            .WithMessage("Araç başlığı 300 karakteri geçemez.");

        RuleFor(command => command.Images)
            .NotNull()
            .Must(images => images.Count is >= 1 and <= 5)
            .WithMessage("Araç için 1 ile 5 arasında fotoğraf yükleyin.");

        RuleForEach(command => command.Images)
            .Must(image => image.Content.Length is > 0 and <=
                CarListingImage.MaximumUploadedImageSizeBytes)
            .WithMessage("Her fotoğraf en fazla 3 MB olabilir.")
            .Must(image =>
                VehicleImageContentInspector.DetectContentType(image.Content) is not null)
            .WithMessage("Yalnızca gerçek JPEG, PNG veya WebP fotoğrafları kabul edilir.");
    }
}
