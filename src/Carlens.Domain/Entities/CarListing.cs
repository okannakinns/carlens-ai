using Carlens.Domain.Common;
using Carlens.Domain.Enums;

namespace Carlens.Domain.Entities;

public sealed class CarListing : BaseEntity
{
    private readonly List<CarListingImage> _images = [];
    private readonly List<CarListingSpecification> _specifications = [];
    private readonly List<CarListingComparable> _comparables = [];

    public Guid Id { get; private set; }
    public string? ListingUrl { get; private set; }
    public ListingInputType InputType { get; private set; }
    public string? ExternalListingId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Brand { get; private set; } = string.Empty;
    public string? Series { get; private set; }
    public string Model { get; private set; } = string.Empty;
    public int? ModelYear { get; private set; }
    public decimal? Price { get; private set; }
    public int? Mileage { get; private set; }
    public FuelType FuelType { get; private set; }
    public TransmissionType TransmissionType { get; private set; }
    public SellerType SellerType { get; private set; }
    public string? Location { get; private set; }
    public string? Description { get; private set; }
    public string? DamageInformation { get; private set; }
    public ListingSourceStatus SourceStatus { get; private set; }
    public string? ImportError { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ImportedAtUtc { get; private set; }

    public IReadOnlyCollection<CarListingImage> Images => _images;
    public IReadOnlyCollection<CarListingSpecification> Specifications => _specifications;
    public IReadOnlyCollection<CarListingComparable> Comparables => _comparables;

    private CarListing()
    {
    }

    public CarListing(string listingUrl)
    {
        if (!Uri.TryCreate(listingUrl, UriKind.Absolute, out var listingUri) ||
            listingUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("A valid HTTPS listing URL is required.", nameof(listingUrl));
        }

        Id = Guid.NewGuid();
        ListingUrl = listingUri.AbsoluteUri;
        InputType = ListingInputType.Url;
        SourceStatus = ListingSourceStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static CarListing CreateManual(
        string brand,
        string? series,
        string model,
        int modelYear,
        decimal? price,
        int mileage,
        FuelType fuelType,
        TransmissionType transmissionType,
        string? location,
        string? description,
        string? damageInformation,
        IEnumerable<CarListingImageCandidate> images)
    {
        ValidateRequiredText(brand, nameof(brand));
        ValidateRequiredText(model, nameof(model));

        if (modelYear <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(modelYear),
                "Model year must be greater than zero.");
        }

        if (price is not null && price <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price),
                "Price must be greater than zero.");
        }

        if (mileage < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(mileage),
                "Mileage cannot be negative.");
        }

        if (!Enum.IsDefined(fuelType) || fuelType == FuelType.Unknown)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fuelType),
                "A known fuel type is required.");
        }

        if (!Enum.IsDefined(transmissionType) ||
            transmissionType == TransmissionType.Unknown)
        {
            throw new ArgumentOutOfRangeException(
                nameof(transmissionType),
                "A known transmission type is required.");
        }

        var imageCandidates = images.ToList();

        if (imageCandidates.Count is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(
                nameof(images),
                "Between one and five vehicle images are required.");
        }

        var normalizedBrand = brand.Trim();
        var normalizedSeries = NormalizeOptionalText(series);
        var normalizedModel = model.Trim();
        var now = DateTime.UtcNow;
        var listing = new CarListing
        {
            Id = Guid.NewGuid(),
            InputType = ListingInputType.Manual,
            Title = string.Join(
                ' ',
                new[]
                {
                    modelYear.ToString(),
                    normalizedBrand,
                    normalizedSeries,
                    normalizedModel
                }.Where(value => !string.IsNullOrWhiteSpace(value))),
            Brand = normalizedBrand,
            Series = normalizedSeries,
            Model = normalizedModel,
            ModelYear = modelYear,
            Price = price,
            Mileage = mileage,
            FuelType = fuelType,
            TransmissionType = transmissionType,
            SellerType = SellerType.Individual,
            Location = NormalizeOptionalText(location),
            Description = NormalizeOptionalText(description),
            DamageInformation = NormalizeOptionalText(damageInformation),
            SourceStatus = ListingSourceStatus.Imported,
            CreatedAtUtc = now,
            ImportedAtUtc = now
        };

        for (var index = 0; index < imageCandidates.Count; index++)
        {
            var image = imageCandidates[index];
            listing._images.Add(new CarListingImage(
                listing.Id,
                image.ContentType,
                image.Content,
                index));
        }

        return listing;
    }

    public void ApplySourceData(
        string externalListingId,
        string title,
        string brand,
        string? series,
        string model,
        int? modelYear,
        decimal? price,
        int? mileage,
        FuelType fuelType,
        TransmissionType transmissionType,
        SellerType sellerType,
        string? location,
        string? description,
        string? damageInformation,
        IEnumerable<string> imageUrls,
        IEnumerable<KeyValuePair<string, string>> specifications,
        IEnumerable<CarListingComparableCandidate> comparables)
    {
        ValidateRequiredText(externalListingId, nameof(externalListingId));
        ValidateRequiredText(title, nameof(title));
        ValidateRequiredText(brand, nameof(brand));
        ValidateRequiredText(model, nameof(model));

        if (modelYear is not null && modelYear <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(modelYear),
                "Model year must be greater than zero.");
        }

        if (price is not null && price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        }

        if (mileage is not null && mileage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mileage), "Mileage cannot be negative.");
        }

        ExternalListingId = externalListingId.Trim();
        Title = title.Trim();
        Brand = brand.Trim();
        Series = NormalizeOptionalText(series);
        Model = model.Trim();
        ModelYear = modelYear;
        Price = price;
        Mileage = mileage;
        FuelType = fuelType;
        TransmissionType = transmissionType;
        SellerType = sellerType;
        Location = NormalizeOptionalText(location);
        Description = NormalizeOptionalText(description);
        DamageInformation = NormalizeOptionalText(damageInformation);

        _images.Clear();
        _specifications.Clear();
        _comparables.Clear();

        var uniqueImageUrls = imageUrls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(50)
            .ToList();

        for (var index = 0; index < uniqueImageUrls.Count; index++)
        {
            _images.Add(new CarListingImage(Id, uniqueImageUrls[index], index));
        }

        var uniqueSpecifications = specifications
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.Key) &&
                !string.IsNullOrWhiteSpace(item.Value))
            .DistinctBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Take(100)
            .ToList();

        for (var index = 0; index < uniqueSpecifications.Count; index++)
        {
            var item = uniqueSpecifications[index];
            _specifications.Add(new CarListingSpecification(Id, item.Key, item.Value, index));
        }

        var uniqueComparables = comparables
            .Where(item =>
                item.Price > 0 &&
                !string.IsNullOrWhiteSpace(item.Url))
            .DistinctBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
            .Take(24)
            .ToList();

        for (var index = 0; index < uniqueComparables.Count; index++)
        {
            var item = uniqueComparables[index];
            _comparables.Add(new CarListingComparable(
                Id,
                item.ModelName,
                item.Title,
                item.ModelYear,
                item.Mileage,
                item.Price,
                item.Location,
                item.Url,
                index));
        }

        SourceStatus = ListingSourceStatus.Imported;
        ImportError = null;
        ImportedAtUtc = DateTime.UtcNow;
    }

    public sealed record CarListingComparableCandidate(
        string ModelName,
        string Title,
        int? ModelYear,
        int? Mileage,
        decimal Price,
        string? Location,
        string Url);

    public sealed record CarListingImageCandidate(
        string ContentType,
        byte[] Content);

    public void MarkImportFailed(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException("Import error is required.", nameof(errorMessage));
        }

        SourceStatus = ListingSourceStatus.Failed;
        ImportError = errorMessage.Trim();
    }

    private static void ValidateRequiredText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
