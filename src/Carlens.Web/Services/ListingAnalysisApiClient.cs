using Carlens.Contracts.Requests;
using Carlens.Contracts.Responses;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace Carlens.Web.Services;

public sealed class ListingAnalysisApiClient : IListingAnalysisApiClient
{
    private readonly HttpClient _httpClient;

    public ListingAnalysisApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<ListingAnalysisResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<ListingAnalysisResponse>>(
            "api/listing-analyses",
            cancellationToken);

        return response ?? [];
    }

    public async Task<ListingAnalysisResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/listing-analyses/{id}",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ListingAnalysisResponse>(
            cancellationToken);
    }

    public async Task<ListingAnalysisResponse> CreateAsync(
        CreateListingAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/listing-analyses",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ListingAnalysisResponse>(
                   cancellationToken)
               ?? throw new InvalidOperationException(
                   "API returned an empty analysis response.");
    }

    public async Task<ListingAnalysisResponse> CreateManualAsync(
        CreateManualVehicleAnalysisRequest request,
        IReadOnlyList<IFormFile> images,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();

        AddText(content, "brand", request.Brand);
        AddText(content, "series", request.Series);
        AddText(content, "model", request.Model);
        AddText(content, "modelYear", request.ModelYear.ToString(CultureInfo.InvariantCulture));
        AddText(
            content,
            "price",
            request.Price?.ToString(CultureInfo.InvariantCulture));
        AddText(content, "mileage", request.Mileage.ToString(CultureInfo.InvariantCulture));
        AddText(content, "fuelType", request.FuelType.ToString(CultureInfo.InvariantCulture));
        AddText(
            content,
            "transmissionType",
            request.TransmissionType.ToString(CultureInfo.InvariantCulture));
        AddText(content, "location", request.Location);
        AddText(content, "description", request.Description);
        AddText(content, "damageInformation", request.DamageInformation);

        foreach (var image in images)
        {
            var imageContent = new StreamContent(image.OpenReadStream());

            if (MediaTypeHeaderValue.TryParse(image.ContentType, out var mediaType))
            {
                imageContent.Headers.ContentType = mediaType;
            }

            content.Add(
                imageContent,
                "images",
                Path.GetFileName(image.FileName));
        }

        using var response = await _httpClient.PostAsync(
            "api/listing-analyses/manual",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ListingAnalysisResponse>(
                   cancellationToken)
               ?? throw new InvalidOperationException(
                   "API returned an empty analysis response.");
    }

    public async Task<ListingImageContent?> GetImageAsync(
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/listing-images/{imageId}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return new ListingImageContent(
            await response.Content.ReadAsByteArrayAsync(cancellationToken),
            response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
    }

    private static void AddText(
        MultipartFormDataContent content,
        string name,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            content.Add(new StringContent(value), name);
        }
    }
}
