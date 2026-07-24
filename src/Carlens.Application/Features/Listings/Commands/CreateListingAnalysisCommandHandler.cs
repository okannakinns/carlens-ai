using System.Security.Cryptography;
using System.Text;
using Carlens.Application.Common.Exceptions;
using Carlens.Application.Common.Mappings;
using Carlens.Application.Interfaces;
using Carlens.Contracts.Events;
using Carlens.Contracts.Responses;
using Carlens.Domain.Entities;
using FluentValidation;

namespace Carlens.Application.Features.Listings.Commands;

public sealed class CreateListingAnalysisCommandHandler
{
    private static readonly TimeSpan DuplicateProtectionDuration = TimeSpan.FromHours(24);

    private readonly ICarListingRepository _carListingRepository;
    private readonly IListingAnalysisRepository _listingAnalysisRepository;
    private readonly IAnalysisRequestPublisher _analysisRequestPublisher;
    private readonly IValidator<CreateListingAnalysisCommand> _validator;
    private readonly IAnalysisCacheService _analysisCacheService;

    public CreateListingAnalysisCommandHandler(
        ICarListingRepository carListingRepository,
        IListingAnalysisRepository listingAnalysisRepository,
        IAnalysisRequestPublisher analysisRequestPublisher,
        IValidator<CreateListingAnalysisCommand> validator,
        IAnalysisCacheService analysisCacheService)
    {
        _carListingRepository = carListingRepository;
        _listingAnalysisRepository = listingAnalysisRepository;
        _analysisRequestPublisher = analysisRequestPublisher;
        _validator = validator;
        _analysisCacheService = analysisCacheService;
    }

    public async Task<ListingAnalysisResponse> HandleAsync(
        CreateListingAnalysisCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var normalizedUrl = NormalizeListingUrl(command.ListingUrl);
        var cacheKey = CreateCacheKey(normalizedUrl);

        var reservationCreated = await _analysisCacheService.TryReserveAsync(
            cacheKey,
            DuplicateProtectionDuration,
            cancellationToken);

        if (!reservationCreated)
        {
            throw new DuplicateAnalysisRequestException(
                "Bu ilan için yakın zamanda bir analiz isteği oluşturuldu.");
        }

        try
        {
            var carListing = new CarListing(normalizedUrl);
            var listingAnalysis = new ListingAnalysis(carListing.Id);

            await _carListingRepository.AddAsync(carListing, cancellationToken);
            await _listingAnalysisRepository.AddAsync(listingAnalysis, cancellationToken);

            var analysisRequestedEvent = new AnalyzeListingRequestedEvent(
                listingAnalysis.Id,
                carListing.Id,
                DateTime.UtcNow);

            await _analysisRequestPublisher.PublishAsync(
                analysisRequestedEvent,
                cancellationToken);

            return listingAnalysis.ToResponse(carListing);
        }
        catch
        {
            await _analysisCacheService.RemoveAsync(cacheKey, CancellationToken.None);
            throw;
        }
    }

    private static string NormalizeListingUrl(string listingUrl)
    {
        var uri = new Uri(listingUrl.Trim(), UriKind.Absolute);
        var builder = new UriBuilder(uri)
        {
            Host = uri.Host.ToLowerInvariant(),
            Query = string.Empty,
            Fragment = string.Empty
        };

        return builder.Uri.AbsoluteUri.TrimEnd('/');
    }

    private static string CreateCacheKey(string normalizedUrl)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedUrl));
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return $"listing-analysis:{hash}";
    }
}
