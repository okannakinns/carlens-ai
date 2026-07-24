namespace Carlens.Domain.Entities;

public sealed class CarListingSpecification
{
    public Guid Id { get; private set; }
    public Guid CarListingId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }

    private CarListingSpecification()
    {
    }

    internal CarListingSpecification(
        Guid carListingId,
        string name,
        string value,
        int displayOrder)
    {
        if (carListingId == Guid.Empty)
        {
            throw new ArgumentException("Car listing id is required.", nameof(carListingId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Specification name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Specification value is required.", nameof(value));
        }

        if (displayOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayOrder),
                "Display order cannot be negative.");
        }

        Id = Guid.NewGuid();
        CarListingId = carListingId;
        Name = name.Trim();
        Value = value.Trim();
        DisplayOrder = displayOrder;
    }
}
