namespace Carlens.Infrastructure.ExternalServices;

public sealed class ListingSourceBlockedException : InvalidOperationException
{
    public ListingSourceBlockedException(string message)
        : base(message)
    {
    }
}
