namespace Carlens.Domain.Entities;

public sealed class CarListingComparable
{
    public Guid Id { get; private set; }
    public Guid CarListingId { get; private set; }
    public string ModelName { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public int? ModelYear { get; private set; }
    public int? Mileage { get; private set; }
    public decimal Price { get; private set; }
    public string? Location { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }

    private CarListingComparable()
    {
    }

    internal CarListingComparable(
        Guid carListingId,
        string modelName,
        string title,
        int? modelYear,
        int? mileage,
        decimal price,
        string? location,
        string url,
        int displayOrder)
    {
        if (carListingId == Guid.Empty)
        {
            throw new ArgumentException("Car listing id is required.", nameof(carListingId));
        }

        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException("Model name is required.", nameof(modelName));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price));
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("A valid HTTPS URL is required.", nameof(url));
        }

        Id = Guid.NewGuid();
        CarListingId = carListingId;
        ModelName = modelName.Trim();
        Title = title.Trim();
        ModelYear = modelYear;
        Mileage = mileage;
        Price = price;
        Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        Url = uri.AbsoluteUri;
        DisplayOrder = displayOrder;
    }
}
