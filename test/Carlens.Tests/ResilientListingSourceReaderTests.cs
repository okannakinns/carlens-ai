using Carlens.Application.DTOs;
using Carlens.Application.Interfaces;
using Carlens.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Carlens.Tests;

public sealed class ResilientListingSourceReaderTests
{
    [Fact]
    public async Task ReadAsync_WhenPrimaryReaderSucceeds_DoesNotUseFallback()
    {
        var expected = CreateSourceData();
        var primary = new StubArabamReader(expected);
        var fallback = new StubWebReader(CreateSourceData());
        var reader = CreateReader(primary, fallback, fallbackEnabled: true);

        var result = await reader.ReadAsync("https://www.arabam.com/ilan/test/123");

        Assert.Same(expected, result);
        Assert.Equal(1, primary.CallCount);
        Assert.Equal(0, fallback.CallCount);
    }

    [Fact]
    public async Task ReadAsync_WhenPrimaryAccessIsBlocked_UsesFallback()
    {
        var expected = CreateSourceData();
        var primary = new StubArabamReader(new ListingSourceBlockedException(
            "Security verification blocked the page."));
        var fallback = new StubWebReader(expected);
        var reader = CreateReader(primary, fallback, fallbackEnabled: true);

        var result = await reader.ReadAsync("https://www.arabam.com/ilan/test/123");

        Assert.Same(expected, result);
        Assert.Equal(1, primary.CallCount);
        Assert.Equal(1, fallback.CallCount);
    }

    [Fact]
    public async Task ReadAsync_WhenFallbackIsDisabled_PreservesBlockedError()
    {
        var exception = new ListingSourceBlockedException(
            "Security verification blocked the page.");
        var primary = new StubArabamReader(exception);
        var fallback = new StubWebReader(CreateSourceData());
        var reader = CreateReader(primary, fallback, fallbackEnabled: false);

        var result = await Assert.ThrowsAsync<ListingSourceBlockedException>(() =>
            reader.ReadAsync("https://www.arabam.com/ilan/test/123"));

        Assert.Same(exception, result);
        Assert.Equal(0, fallback.CallCount);
    }

    private static ResilientListingSourceReader CreateReader(
        StubArabamReader primary,
        StubWebReader fallback,
        bool fallbackEnabled)
    {
        return new ResilientListingSourceReader(
            primary,
            fallback,
            Options.Create(new ListingSourceOptions
            {
                EnableOpenAiWebFallback = fallbackEnabled
            }),
            NullLogger<ResilientListingSourceReader>.Instance);
    }

    private static ListingSourceData CreateSourceData()
    {
        return new ListingSourceData(
            "https://www.arabam.com/ilan/test/123",
            "123",
            "Test listing",
            "Volkswagen",
            "Polo",
            "1.6 TDi Comfortline",
            2017,
            815000,
            240000,
            Domain.Enums.FuelType.Diesel,
            Domain.Enums.TransmissionType.Manual,
            Domain.Enums.SellerType.Dealer,
            "Adana",
            null,
            null,
            new Dictionary<string, string>(),
            [],
            []);
    }

    private sealed class StubArabamReader : IPrimaryListingSourceReader
    {
        private readonly ListingSourceData? _result;
        private readonly Exception? _exception;

        public StubArabamReader(ListingSourceData result)
        {
            _result = result;
        }

        public StubArabamReader(Exception exception)
        {
            _exception = exception;
        }

        public int CallCount { get; private set; }

        public Task<ListingSourceData> ReadAsync(
            string listingUrl,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            return _exception is null
                ? Task.FromResult(_result!)
                : Task.FromException<ListingSourceData>(_exception);
        }
    }

    private sealed class StubWebReader : IFallbackListingSourceReader
    {
        private readonly ListingSourceData _result;

        public StubWebReader(ListingSourceData result)
        {
            _result = result;
        }

        public int CallCount { get; private set; }

        public Task<ListingSourceData> ReadAsync(
            string listingUrl,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_result);
        }
    }

}
