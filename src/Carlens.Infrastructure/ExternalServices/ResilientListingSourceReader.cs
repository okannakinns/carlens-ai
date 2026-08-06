using Carlens.Application.DTOs;
using Carlens.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Carlens.Infrastructure.ExternalServices;

public sealed class ResilientListingSourceReader : IListingSourceReader
{
    private readonly IPrimaryListingSourceReader _primaryReader;
    private readonly IFallbackListingSourceReader _fallbackReader;
    private readonly ListingSourceOptions _options;
    private readonly ILogger<ResilientListingSourceReader> _logger;

    public ResilientListingSourceReader(
        IPrimaryListingSourceReader primaryReader,
        IFallbackListingSourceReader fallbackReader,
        IOptions<ListingSourceOptions> options,
        ILogger<ResilientListingSourceReader> logger)
    {
        _primaryReader = primaryReader;
        _fallbackReader = fallbackReader;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ListingSourceData> ReadAsync(
        string listingUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primaryReader.ReadAsync(listingUrl, cancellationToken);
        }
        catch (ListingSourceBlockedException exception)
            when (_options.EnableOpenAiWebFallback)
        {
            _logger.LogWarning(
                exception,
                "Direct listing access was blocked. Using the verified web-search fallback for {ListingUrl}.",
                listingUrl);

            return await _fallbackReader.ReadAsync(listingUrl, cancellationToken);
        }
    }
}
