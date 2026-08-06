using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Carlens.Application.DTOs;
using Carlens.Application.Interfaces;
using Carlens.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace Carlens.Infrastructure.ExternalServices;

public sealed class ArabamComListingSourceReader :
    IPrimaryListingSourceReader,
    IAsyncDisposable
{
    private const string PageExtractionScript = """
        () => {
          const clean = value => String(value ?? "")
            .replace(/\s+/g, " ")
            .trim();
          const normalize = value => clean(value).toLocaleLowerCase("tr-TR");

          const scripts = Array.from(
            document.querySelectorAll('script[type="application/ld+json"]'));

          let car = null;

          for (const script of scripts) {
            try {
              const parsed = JSON.parse(script.textContent || "{}");
              const candidates = Array.isArray(parsed) ? parsed : [parsed];
              car = candidates.find(item => {
                const type = item?.["@type"];
                return type === "Car" || (Array.isArray(type) && type.includes("Car"));
              }) || car;
            } catch {
              // Ignore unrelated or malformed structured data blocks.
            }
          }

          const properties = {};

          document.querySelectorAll(".property-item").forEach(item => {
            const key = clean(item.querySelector(".property-key")?.textContent);
            const value = clean(item.querySelector(".property-value")?.textContent);

            if (key && value) {
              properties[key] = value.replace(/^Kopyalandı\s*/i, "");
            }
          });

          const imageValues = Array.isArray(car?.image)
            ? car.image
            : car?.image
              ? [car.image]
              : [];

          const imageUrls = [...new Set(
            imageValues
              .map(image => typeof image === "string" ? image : image?.url)
              .filter(Boolean)
          )];

          const descriptionRoot = document.querySelector("#tab-description");
          const damageRoot = document.querySelector("#tab-damage-information");
          const metaTitle = document.querySelector('meta[property="og:title"]')
            ?.getAttribute("content");
          const locationElement = document.querySelector(
            ".product-location, .product-detail-location, [class*='product-location']");

          const pathnameParts = location.pathname.split("/").filter(Boolean);
          const externalListingId = pathnameParts[pathnameParts.length - 1] || "";
          const brand = typeof car?.brand === "string"
            ? car.brand
            : car?.brand?.name;
          const offer = Array.isArray(car?.offers) ? car.offers[0] : car?.offers;
          const mileage = car?.mileageFromOdometer?.value;
          const series = clean(properties["Seri"] || car?.model);
          const model = clean(properties["Model"] || car?.model);

          const comparableLinks = Array.from(
            document.querySelectorAll('a[href^="/ikinci-el/"]'));
          const comparableLink =
            comparableLinks.find(link => normalize(link.textContent) === normalize(model)) ||
            comparableLinks.find(link => normalize(link.textContent) === normalize(series));

          const damageStatusNames = new Map([
            ["orijinal", "Orijinal"],
            ["orjinal", "Orijinal"],
            ["lokal boyalı", "Lokal boyalı"],
            ["lokal boyali", "Lokal boyalı"],
            ["boyalı", "Boyalı"],
            ["boyali", "Boyalı"],
            ["değişmiş", "Değişmiş"],
            ["degismis", "Değişmiş"],
            ["belirtilmemiş", "Belirtilmemiş"],
            ["belirtilmemis", "Belirtilmemiş"]
          ]);
          const damageGroups = [];

          damageRoot?.querySelectorAll("p").forEach(paragraph => {
            const statusName = damageStatusNames.get(normalize(paragraph.textContent));

            if (!statusName) {
              return;
            }

            let sibling = paragraph.nextElementSibling;
            let list = null;

            while (sibling && sibling.tagName !== "P") {
              if (sibling.matches("ul")) {
                list = sibling;
                break;
              }

              list = sibling.querySelector("ul");

              if (list) {
                break;
              }

              sibling = sibling.nextElementSibling;
            }

            const parts = Array.from(list?.querySelectorAll("li") || [])
              .map(item => clean(item.textContent).replace(/[·•]+$/g, ""))
              .filter(item => item && item !== "-");

            damageGroups.push(
              `${statusName}: ${parts.length > 0 ? parts.join(", ") : "Yok"}`);
          });

          const damageSummary = clean(properties["Boya-değişen"]);
          const tramerSummary = Array.from(damageRoot?.querySelectorAll("p") || [])
            .map(paragraph => clean(paragraph.textContent))
            .find(value => normalize(value).startsWith("tramer"));
          const damageInformation = (
            damageGroups.length > 0
              ? [
                  ...damageGroups,
                  tramerSummary ? `Tramer: ${tramerSummary}` : ""
                ]
              : [
                  damageSummary ? `Özet: ${damageSummary}` : "",
                  tramerSummary ? `Tramer: ${tramerSummary}` : ""
                ]
          ).filter(Boolean).join("\n");

          return JSON.stringify({
            externalListingId: clean(properties["İlan No"] || externalListingId),
            title: clean(metaTitle || car?.name || document.title)
              .replace(/\s*\|\s*arabam\.com\s*$/i, ""),
            brand: clean(properties["Marka"] || brand),
            series,
            model,
            modelYear: clean(properties["Yıl"] || car?.productionDate),
            price: clean(properties["Fiyat"] || offer?.price),
            mileage: clean(properties["Kilometre"] || mileage),
            fuelType: clean(properties["Yakıt Tipi"]),
            transmissionType: clean(
              properties["Vites Tipi"] || car?.vehicleTransmission),
            sellerType: clean(properties["Kimden"]),
            location: clean(locationElement?.textContent),
            description: clean(descriptionRoot?.innerText)
              .replace(/^Açıklama\s*/i, ""),
            damageInformation,
            comparableSearchUrl: comparableLink?.href || "",
            properties,
            imageUrls
          });
        }
        """;

    private const string ComparableExtractionScript = """
        () => {
          const clean = value => String(value ?? "")
            .replace(/\s+/g, " ")
            .trim();

          const rows = Array.from(document.querySelectorAll("table tbody tr"));

          return JSON.stringify(rows.map(row => {
            const cells = Array.from(row.querySelectorAll("td"))
              .map(cell => clean(cell.innerText));
            const link = row.querySelector('a[href*="/ilan/"]');

            if (cells.length < 9 || !link) {
              return null;
            }

            return {
              modelName: cells[1],
              title: cells[2],
              modelYear: cells[3],
              mileage: cells[4],
              price: (
                cells[6].match(/\d{1,3}(?:\.\d{3})+(?:,\d+)?\s*TL/gi) || []
              ).at(-1) || cells[6],
              location: cells[8],
              url: link.href
            };
          }).filter(Boolean));
        }
        """;

    private readonly ListingSourceOptions _options;
    private readonly ILogger<ArabamComListingSourceReader> _logger;
    private readonly SemaphoreSlim _browserInitializationLock = new(1, 1);
    private readonly SemaphoreSlim _pageConcurrency;

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public ArabamComListingSourceReader(
        IOptions<ListingSourceOptions> options,
        ILogger<ArabamComListingSourceReader> logger)
    {
        _options = options.Value;
        _logger = logger;
        _pageConcurrency = new SemaphoreSlim(
            Math.Max(1, _options.MaxConcurrentPages),
            Math.Max(1, _options.MaxConcurrentPages));
    }

    public async Task<ListingSourceData> ReadAsync(
        string listingUrl,
        CancellationToken cancellationToken = default)
    {
        var sourceUri = ValidateListingUrl(listingUrl);

        await _pageConcurrency.WaitAsync(cancellationToken);

        try
        {
            var browser = await GetBrowserAsync(cancellationToken);

            await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                Locale = "tr-TR",
                IgnoreHTTPSErrors = false
            }).WaitAsync(cancellationToken);

            var page = await context.NewPageAsync().WaitAsync(cancellationToken);
            page.SetDefaultTimeout(_options.NavigationTimeoutSeconds * 1000);

            var response = await page.GotoAsync(
                sourceUri.AbsoluteUri,
                new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = _options.NavigationTimeoutSeconds * 1000
                }).WaitAsync(cancellationToken);

            if (response is null)
            {
                throw new InvalidOperationException(
                    "İlan sayfasına erişilemedi veya sayfa geçerli bir cevap döndürmedi.");
            }

            if (!response.Ok)
            {
                if (IsSecurityChallenge(response))
                {
                    throw CreateBlockedException();
                }

                throw new InvalidOperationException(
                    $"İlan sayfası geçerli bir cevap döndürmedi (HTTP {response.Status}).");
            }

            try
            {
                await page.WaitForFunctionAsync(
                    """
                    () => Array.from(
                      document.querySelectorAll('script[type="application/ld+json"]')
                    ).some(script => /"@type"\s*:\s*"Car"/.test(script.textContent || ""))
                    """,
                    null,
                    new PageWaitForFunctionOptions
                    {
                        Timeout = _options.NavigationTimeoutSeconds * 1000
                    }).WaitAsync(cancellationToken);
            }
            catch (TimeoutException)
            {
                var pageTitle = await page.TitleAsync().WaitAsync(cancellationToken);

                if (IsSecurityChallengeTitle(pageTitle))
                {
                    throw CreateBlockedException();
                }

                throw new InvalidOperationException(
                    "Sayfada okunabilir bir araç ilanı bulunamadı.");
            }

            var pageDataJson = await page
                .EvaluateAsync<string>(PageExtractionScript)
                .WaitAsync(cancellationToken);
            var pageData = JsonSerializer.Deserialize<ArabamPageData>(pageDataJson)
                ?? throw new InvalidOperationException(
                    "İlan sayfasından alınan veri çözümlenemedi.");
            var comparables = await TryReadComparablesAsync(
                page,
                sourceUri,
                pageData,
                cancellationToken);

            return MapToSourceData(sourceUri, pageData, comparables);
        }
        finally
        {
            _pageConcurrency.Release();
        }
    }

    private async Task<IBrowser> GetBrowserAsync(CancellationToken cancellationToken)
    {
        if (_browser is { IsConnected: true })
        {
            return _browser;
        }

        await _browserInitializationLock.WaitAsync(cancellationToken);

        try
        {
            if (_browser is { IsConnected: true })
            {
                return _browser;
            }

            if (_browser is not null)
            {
                await _browser.DisposeAsync();
            }

            _playwright?.Dispose();
            _playwright = await Playwright.CreateAsync().WaitAsync(cancellationToken);
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = _options.Headless,
                Channel = "chromium"
            }).WaitAsync(cancellationToken);

            return _browser;
        }
        finally
        {
            _browserInitializationLock.Release();
        }
    }

    private static bool IsSecurityChallenge(IResponse response)
    {
        return response.Status == 403 &&
               response.Headers.Any(header =>
                   header.Key.Equals("cf-mitigated", StringComparison.OrdinalIgnoreCase) &&
                   header.Value.Equals("challenge", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSecurityChallengeTitle(string title)
    {
        return title.Contains("Just a moment", StringComparison.OrdinalIgnoreCase) ||
               title.Contains("Bir dakika lütfen", StringComparison.OrdinalIgnoreCase);
    }

    private static ListingSourceBlockedException CreateBlockedException()
    {
        return new ListingSourceBlockedException(
            "Arabam.com güvenlik doğrulaması ilan sayfasının otomatik okunmasını engelledi.");
    }

    private ListingSourceData MapToSourceData(
        Uri sourceUri,
        ArabamPageData pageData,
        IReadOnlyList<ListingComparableData> comparables)
    {
        if (string.IsNullOrWhiteSpace(pageData.ExternalListingId) ||
            string.IsNullOrWhiteSpace(pageData.Title) ||
            string.IsNullOrWhiteSpace(pageData.Brand) ||
            string.IsNullOrWhiteSpace(pageData.Model))
        {
            throw new InvalidOperationException(
                "İlanın zorunlu bilgileri Arabam.com sayfasından okunamadı.");
        }

        var properties = (pageData.Properties ?? new Dictionary<string, string>())
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.Key) &&
                !string.IsNullOrWhiteSpace(item.Value))
            .Take(100)
            .ToDictionary(
                item => Limit(item.Key, 200),
                item => Limit(item.Value, 1000),
                StringComparer.OrdinalIgnoreCase);

        var imageUrls = (pageData.ImageUrls ?? [])
            .Where(IsSupportedImageUrl)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, _options.MaxStoredImages))
            .ToList();

        return new ListingSourceData(
            sourceUri.AbsoluteUri,
            pageData.ExternalListingId,
            Limit(pageData.Title, 300),
            Limit(pageData.Brand, 100),
            LimitOptional(pageData.Series, 100),
            Limit(pageData.Model, 150),
            ParseNullableInt(pageData.ModelYear),
            ParseNullableDecimal(pageData.Price),
            ParseNullableInt(pageData.Mileage),
            MapFuelType(pageData.FuelType),
            MapTransmissionType(pageData.TransmissionType),
            MapSellerType(pageData.SellerType),
            LimitOptional(pageData.Location, 300),
            LimitOptional(pageData.Description, 12000),
            LimitOptional(pageData.DamageInformation, 8000),
            properties,
            imageUrls,
            comparables);
    }

    private async Task<IReadOnlyList<ListingComparableData>> TryReadComparablesAsync(
        IPage page,
        Uri sourceUri,
        ArabamPageData pageData,
        CancellationToken cancellationToken)
    {
        var modelYear = ParseNullableInt(pageData.ModelYear);

        if (modelYear is null ||
            string.IsNullOrWhiteSpace(pageData.Brand) ||
            string.IsNullOrWhiteSpace(pageData.Series))
        {
            return [];
        }

        try
        {
            var marketUri = ResolveMarketSearchUri(pageData, modelYear.Value);
            var response = await page.GotoAsync(
                marketUri.AbsoluteUri,
                new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = _options.NavigationTimeoutSeconds * 1000
                }).WaitAsync(cancellationToken);

            if (response is null || !response.Ok)
            {
                return [];
            }

            await page.Locator("table tbody tr")
                .First
                .WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = _options.NavigationTimeoutSeconds * 1000
                })
                .WaitAsync(cancellationToken);

            var comparableJson = await page
                .EvaluateAsync<string>(ComparableExtractionScript)
                .WaitAsync(cancellationToken);
            var comparableRows =
                JsonSerializer.Deserialize<List<ArabamComparablePageData>>(comparableJson) ?? [];

            var parsedRows = comparableRows
                .Where(row =>
                    !string.IsNullOrWhiteSpace(row.ModelName) &&
                    !string.IsNullOrWhiteSpace(row.Title) &&
                    !string.IsNullOrWhiteSpace(row.Url) &&
                    !row.Url.EndsWith(
                        $"/{pageData.ExternalListingId}",
                        StringComparison.OrdinalIgnoreCase))
                .Select(row => new ListingComparableData(
                    Limit(row.ModelName!, 200),
                    Limit(row.Title!, 400),
                    ParseNullableInt(row.ModelYear),
                    ParseNullableInt(row.Mileage),
                    ParseNullableDecimal(row.Price) ?? 0,
                    LimitOptional(row.Location, 300),
                    row.Url!))
                .Where(row =>
                    row.Price > 0 &&
                    Uri.TryCreate(row.Url, UriKind.Absolute, out var uri) &&
                    uri.Scheme == Uri.UriSchemeHttps &&
                    uri.Host.EndsWith("arabam.com", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var exactYearRows = parsedRows
                .Where(row => row.ModelYear == modelYear)
                .ToList();
            var nearbyYearRows = parsedRows
                .Where(row =>
                    row.ModelYear.HasValue &&
                    Math.Abs(row.ModelYear.Value - modelYear.Value) <= 1)
                .ToList();
            var marketRows = exactYearRows.Count >= 3
                ? exactYearRows
                : nearbyYearRows.Count >= 3
                    ? nearbyYearRows
                    : parsedRows;

            return marketRows
                .Take(Math.Clamp(_options.MaxComparables, 0, 30))
                .ToList();
        }
        catch (Exception exception) when (
            exception is TimeoutException or PlaywrightException or JsonException)
        {
            _logger.LogWarning(
                exception,
                "Comparable listings could not be read for {ListingUrl}.",
                sourceUri.AbsoluteUri);

            return [];
        }
    }

    private static Uri ResolveMarketSearchUri(
        ArabamPageData pageData,
        int modelYear)
    {
        if (Uri.TryCreate(
                pageData.ComparableSearchUrl,
                UriKind.Absolute,
                out var marketUri) &&
            marketUri.Scheme == Uri.UriSchemeHttps &&
            marketUri.Host.Equals("www.arabam.com", StringComparison.OrdinalIgnoreCase) &&
            marketUri.AbsolutePath.StartsWith(
                "/ikinci-el/",
                StringComparison.OrdinalIgnoreCase))
        {
            return marketUri;
        }

        return BuildMarketSearchUri(
            pageData.Brand!,
            pageData.Series!,
            modelYear);
    }

    private static Uri BuildMarketSearchUri(
        string brand,
        string series,
        int modelYear)
    {
        var slug = $"{Slugify(brand)}-{Slugify(series)}-{modelYear}";
        return new Uri($"https://www.arabam.com/ikinci-el/model/{slug}");
    }

    private static string Slugify(string value)
    {
        var normalized = value
            .Trim()
            .ToLower(CultureInfo.GetCultureInfo("tr-TR"))
            .Replace('ı', 'i')
            .Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        var previousWasSeparator = false;

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSeparator = false;
                continue;
            }

            if (!previousWasSeparator && builder.Length > 0)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static Uri ValidateListingUrl(string listingUrl)
    {
        if (!Uri.TryCreate(listingUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Geçerli bir HTTPS ilan bağlantısı girin.", nameof(listingUrl));
        }

        var isSupportedHost =
            uri.Host.Equals("arabam.com", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Equals("www.arabam.com", StringComparison.OrdinalIgnoreCase);

        if (!isSupportedHost ||
            !uri.AbsolutePath.StartsWith("/ilan/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Yalnızca Arabam.com ilan bağlantıları destekleniyor.",
                nameof(listingUrl));
        }

        return uri;
    }

    private static bool IsSupportedImageUrl(string imageUrl)
    {
        return Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) &&
               uri.Scheme == Uri.UriSchemeHttps &&
               uri.Host.EndsWith("mncdn.com", StringComparison.OrdinalIgnoreCase);
    }

    private static int? ParseNullableInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static decimal? ParseNullableDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = new string(
            value.Where(character => char.IsDigit(character) || character is ',' or '.').ToArray());

        if (decimal.TryParse(
            normalized,
            NumberStyles.Number,
            CultureInfo.GetCultureInfo("tr-TR"),
            out var result))
        {
            return result;
        }

        return decimal.TryParse(
            normalized,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out result)
            ? result
            : null;
    }

    private static FuelType MapFuelType(string? value)
    {
        var normalized = NormalizeTurkish(value);

        if (normalized.Contains("elektrik"))
        {
            return FuelType.Electric;
        }

        if (normalized.Contains("hibrit"))
        {
            return FuelType.Hybrid;
        }

        if (normalized.Contains("dizel"))
        {
            return FuelType.Diesel;
        }

        if (normalized.Contains("lpg"))
        {
            return FuelType.LPG;
        }

        if (normalized.Contains("benzin"))
        {
            return FuelType.Gasoline;
        }

        return FuelType.Unknown;
    }

    private static TransmissionType MapTransmissionType(string? value)
    {
        var normalized = NormalizeTurkish(value);

        if (normalized.Contains("yarı otomatik") ||
            normalized.Contains("yari otomatik"))
        {
            return TransmissionType.SemiAutomatic;
        }

        if (normalized.Contains("otomatik"))
        {
            return TransmissionType.Automatic;
        }

        if (normalized.Contains("düz") ||
            normalized.Contains("duz") ||
            normalized.Contains("manuel"))
        {
            return TransmissionType.Manual;
        }

        return TransmissionType.Unknown;
    }

    private static SellerType MapSellerType(string? value)
    {
        var normalized = NormalizeTurkish(value);

        if (normalized.Contains("galeri"))
        {
            return SellerType.Dealer;
        }

        if (normalized.Contains("sahibinden") ||
            normalized.Contains("bireysel"))
        {
            return SellerType.Individual;
        }

        return SellerType.Unknown;
    }

    private static string NormalizeTurkish(string? value)
    {
        return (value ?? string.Empty)
            .Trim()
            .ToLower(CultureInfo.GetCultureInfo("tr-TR"));
    }

    private static string Limit(string value, int maximumLength)
    {
        var normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : normalized[..maximumLength];
    }

    private static string? LimitOptional(string? value, int maximumLength)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : Limit(value, maximumLength);
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _browserInitializationLock.Dispose();
        _pageConcurrency.Dispose();
    }

    private sealed class ArabamPageData
    {
        [JsonPropertyName("externalListingId")]
        public string? ExternalListingId { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("brand")]
        public string? Brand { get; init; }

        [JsonPropertyName("series")]
        public string? Series { get; init; }

        [JsonPropertyName("model")]
        public string? Model { get; init; }

        [JsonPropertyName("modelYear")]
        public string? ModelYear { get; init; }

        [JsonPropertyName("price")]
        public string? Price { get; init; }

        [JsonPropertyName("mileage")]
        public string? Mileage { get; init; }

        [JsonPropertyName("fuelType")]
        public string? FuelType { get; init; }

        [JsonPropertyName("transmissionType")]
        public string? TransmissionType { get; init; }

        [JsonPropertyName("sellerType")]
        public string? SellerType { get; init; }

        [JsonPropertyName("location")]
        public string? Location { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("damageInformation")]
        public string? DamageInformation { get; init; }

        [JsonPropertyName("comparableSearchUrl")]
        public string? ComparableSearchUrl { get; init; }

        [JsonPropertyName("properties")]
        public Dictionary<string, string>? Properties { get; init; }

        [JsonPropertyName("imageUrls")]
        public List<string>? ImageUrls { get; init; }
    }

    private sealed class ArabamComparablePageData
    {
        [JsonPropertyName("modelName")]
        public string? ModelName { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("modelYear")]
        public string? ModelYear { get; init; }

        [JsonPropertyName("mileage")]
        public string? Mileage { get; init; }

        [JsonPropertyName("price")]
        public string? Price { get; init; }

        [JsonPropertyName("location")]
        public string? Location { get; init; }

        [JsonPropertyName("url")]
        public string? Url { get; init; }
    }
}
