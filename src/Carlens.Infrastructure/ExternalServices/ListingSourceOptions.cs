namespace Carlens.Infrastructure.ExternalServices;

public sealed class ListingSourceOptions
{
    public bool Headless { get; set; } = true;
    public bool EnableOpenAiWebFallback { get; set; } = true;
    public int NavigationTimeoutSeconds { get; set; } = 45;
    public int MaxConcurrentPages { get; set; } = 2;
    public int MaxStoredImages { get; set; } = 50;
    public int MaxComparables { get; set; } = 20;
}
