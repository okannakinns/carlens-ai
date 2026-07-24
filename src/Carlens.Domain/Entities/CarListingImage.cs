namespace Carlens.Domain.Entities;

public sealed class CarListingImage
{
    public const int MaximumUploadedImageSizeBytes = 3 * 1024 * 1024;

    public Guid Id { get; private set; }
    public Guid CarListingId { get; private set; }
    public string? Url { get; private set; }
    public string? ContentType { get; private set; }
    public byte[]? Content { get; private set; }
    public int DisplayOrder { get; private set; }

    private CarListingImage()
    {
    }

    internal CarListingImage(
        Guid carListingId,
        string url,
        int displayOrder)
    {
        if (carListingId == Guid.Empty)
        {
            throw new ArgumentException("Car listing id is required.", nameof(carListingId));
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var imageUri) ||
            imageUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("A valid HTTPS image URL is required.", nameof(url));
        }

        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder),
                "Display order cannot be negative.");
        }

        Id = Guid.NewGuid();
        CarListingId = carListingId;
        Url = imageUri.AbsoluteUri;
        DisplayOrder = displayOrder;
    }

    internal CarListingImage(
        Guid carListingId,
        string contentType,
        byte[] content,
        int displayOrder)
    {
        if (carListingId == Guid.Empty)
        {
            throw new ArgumentException("Car listing id is required.", nameof(carListingId));
        }

        if (contentType is not ("image/jpeg" or "image/png" or "image/webp"))
        {
            throw new ArgumentException(
                "Only JPEG, PNG and WebP images are supported.",
                nameof(contentType));
        }

        if (content.Length is 0 or > MaximumUploadedImageSizeBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(content),
                $"Image size must be between 1 and {MaximumUploadedImageSizeBytes} bytes.");
        }

        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder),
                "Display order cannot be negative.");
        }

        Id = Guid.NewGuid();
        CarListingId = carListingId;
        ContentType = contentType;
        Content = content.ToArray();
        DisplayOrder = displayOrder;
    }
}
