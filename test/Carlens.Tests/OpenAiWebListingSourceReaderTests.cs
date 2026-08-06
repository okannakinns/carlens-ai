using System.Net;
using System.Text;
using System.Text.Json;
using Carlens.Domain.Enums;
using Carlens.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Carlens.Tests;

public sealed class OpenAiWebListingSourceReaderTests
{
    private const string ListingUrl =
        "https://www.arabam.com/ilan/galeriden-satilik-volkswagen-polo-1-6-tdi-comfortline/test-listing/42216673";

    [Fact]
    public async Task ReadAsync_MapsVerifiedSearchDataAndFiltersComparables()
    {
        var output = JsonSerializer.Serialize(new
        {
            externalListingId = "42216673",
            title = "BOYASIZ DEĞİŞENSİZ POLO",
            brand = "Volkswagen",
            series = "Polo",
            model = "Volkswagen Polo 1.6 TDi Comfortline",
            modelYear = 2017,
            price = 815000,
            mileage = 240000,
            fuelType = "Dizel",
            transmissionType = (string?)null,
            sellerType = (string?)null,
            location = "Adana Sarıçam",
            comparables = new object[]
            {
                Comparable(
                    "Nearby listing",
                    2018,
                    133000,
                    1198750,
                    "https://www.arabam.com/ilan/test/40730320"),
                Comparable(
                    "Target listing",
                    2017,
                    240000,
                    815000,
                    "https://www.arabam.com/ilan/test/42216673"),
                Comparable(
                    "Wrong year",
                    2011,
                    265000,
                    825000,
                    "https://www.arabam.com/ilan/test/35144664"),
                Comparable(
                    "Wrong host",
                    2017,
                    200000,
                    900000,
                    "https://example.com/ilan/1")
            }
        });
        var handler = new RecordingHandler(CreateOpenAiResponse(output));
        var reader = CreateReader(handler);

        var result = await reader.ReadAsync(ListingUrl);

        Assert.Equal("42216673", result.ExternalListingId);
        Assert.Equal("Volkswagen", result.Brand);
        Assert.Equal("Polo", result.Series);
        Assert.Equal("1.6 TDi Comfortline", result.Model);
        Assert.Equal(2017, result.ModelYear);
        Assert.Equal(815000, result.Price);
        Assert.Equal(240000, result.Mileage);
        Assert.Equal(FuelType.Diesel, result.FuelType);
        Assert.Equal(TransmissionType.Unknown, result.TransmissionType);
        Assert.Equal(SellerType.Dealer, result.SellerType);
        Assert.Empty(result.ImageUrls);
        Assert.Single(result.Comparables);
        Assert.Equal(2018, result.Comparables[0].ModelYear);
        Assert.Contains("güvenlik doğrulaması", result.Specifications["Veri kapsamı"]);

        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Contains("\"type\":\"web_search\"", handler.RequestBody);
        Assert.Contains("\"allowed_domains\":[\"arabam.com\"]", handler.RequestBody);
        Assert.Contains("\"maxItems\":5", handler.RequestBody);
        Assert.Contains("\"store\":false", handler.RequestBody);
    }

    [Fact]
    public async Task ReadAsync_RejectsMismatchedListingId()
    {
        var output = JsonSerializer.Serialize(new
        {
            externalListingId = "99999999",
            title = "Different listing",
            brand = "Volkswagen",
            series = "Polo",
            model = "1.6 TDi Comfortline",
            modelYear = 2017,
            price = 815000,
            mileage = 240000,
            fuelType = "Dizel",
            transmissionType = "Düz",
            sellerType = "Galeriden",
            location = "Adana",
            comparables = Array.Empty<object>()
        });
        var reader = CreateReader(new RecordingHandler(CreateOpenAiResponse(output)));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reader.ReadAsync(ListingUrl));

        Assert.Contains("doğrulanabilir temel", exception.Message);
    }

    private static object Comparable(
        string title,
        int modelYear,
        int mileage,
        decimal price,
        string url)
    {
        return new
        {
            modelName = "Volkswagen Polo 1.6 TDi Comfortline",
            title,
            modelYear,
            mileage,
            price,
            location = "Adana",
            url
        };
    }

    private static HttpResponseMessage CreateOpenAiResponse(string output)
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            output = new[]
            {
                new
                {
                    type = "message",
                    content = new[]
                    {
                        new
                        {
                            type = "output_text",
                            text = output
                        }
                    }
                }
            }
        });

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };
    }

    private static OpenAiWebListingSourceReader CreateReader(
        RecordingHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.openai.com/v1/")
        };

        return new OpenAiWebListingSourceReader(
            new StubHttpClientFactory(httpClient),
            Options.Create(new OpenAiOptions
            {
                ApiKey = "test-api-key",
                Model = "gpt-test"
            }),
            Options.Create(new ListingSourceOptions
            {
                MaxComparables = 8
            }),
            NullLogger<OpenAiWebListingSourceReader>.Instance);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _httpClient;

        public StubHttpClientFactory(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public HttpClient CreateClient(string name)
        {
            Assert.Equal(OpenAiWebListingSourceReader.HttpClientName, name);
            return _httpClient;
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public RecordingHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public string? AuthorizationScheme { get; private set; }
        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            RequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return _response;
        }
    }
}
