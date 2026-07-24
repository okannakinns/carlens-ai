using System.Net;
using Carlens.Contracts.Requests;
using Carlens.Contracts.Responses;
using Carlens.Web.Security;
using Carlens.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Carlens.Web.Controllers;

[ApiController]
[Route("api/analyses")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AnalysesGatewayController : ControllerBase
{
    private const long MaximumUrlRequestSizeBytes = 16 * 1024;
    private const long MaximumManualRequestSizeBytes = 16 * 1024 * 1024;

    private readonly IListingAnalysisApiClient _apiClient;
    private readonly IAnalysisAccessStore _accessStore;

    public AnalysesGatewayController(
        IListingAnalysisApiClient apiClient,
        IAnalysisAccessStore accessStore)
    {
        _apiClient = apiClient;
        _accessStore = accessStore;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ListingAnalysisResponse>>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var analyses = await _apiClient.GetAllAsync(cancellationToken);
            var accessibleAnalyses = analyses
                .Where(analysis => _accessStore.CanAccessAnalysis(
                    HttpContext.Session,
                    analysis.AnalysisId))
                .ToList();

            foreach (var analysis in accessibleAnalyses)
            {
                _accessStore.Grant(HttpContext.Session, analysis);
            }

            return Ok(accessibleAnalyses);
        }
        catch (HttpRequestException)
        {
            return ApiUnavailable();
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ListingAnalysisResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!_accessStore.CanAccessAnalysis(HttpContext.Session, id))
        {
            return NotFound();
        }

        try
        {
            var analysis = await _apiClient.GetByIdAsync(id, cancellationToken);

            if (analysis is not null)
            {
                _accessStore.Grant(HttpContext.Session, analysis);
            }

            return analysis is null ? NotFound() : Ok(analysis);
        }
        catch (HttpRequestException)
        {
            return ApiUnavailable();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(SecurityPolicyNames.AnalysisCreation)]
    [RequestSizeLimit(MaximumUrlRequestSizeBytes)]
    public async Task<ActionResult<ListingAnalysisResponse>> CreateAsync(
        CreateListingAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var analysis = await _apiClient.CreateAsync(request, cancellationToken);
            _accessStore.Grant(HttpContext.Session, analysis);
            return StatusCode(StatusCodes.Status201Created, analysis);
        }
        catch (HttpRequestException exception)
            when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Bu ilan zaten analiz edildi",
                Detail = "Son 24 saat içinde oluşturulan analiz sonucunu açabilirsiniz."
            });
        }
        catch (HttpRequestException exception)
            when (exception.StatusCode == HttpStatusCode.BadRequest)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "İlan bağlantısı geçersiz",
                Detail = "Geçerli bir Arabam.com ilan bağlantısı girin."
            });
        }
        catch (HttpRequestException)
        {
            return ApiUnavailable();
        }
    }

    [HttpPost("manual")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(SecurityPolicyNames.AnalysisCreation)]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumManualRequestSizeBytes)]
    [RequestSizeLimit(MaximumManualRequestSizeBytes)]
    public async Task<ActionResult<ListingAnalysisResponse>> CreateManualAsync(
        [FromForm] CreateManualVehicleAnalysisRequest request,
        [FromForm(Name = "images")] List<IFormFile> images,
        CancellationToken cancellationToken)
    {
        try
        {
            var analysis = await _apiClient.CreateManualAsync(
                request,
                images,
                cancellationToken);
            _accessStore.Grant(HttpContext.Session, analysis);
            return StatusCode(StatusCodes.Status201Created, analysis);
        }
        catch (HttpRequestException exception)
            when (exception.StatusCode == HttpStatusCode.BadRequest)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Araç bilgileri geçersiz",
                Detail = "Zorunlu araç bilgilerini ve 1-5 fotoğrafı kontrol edin."
            });
        }
        catch (HttpRequestException)
        {
            return ApiUnavailable();
        }
    }

    private ObjectResult ApiUnavailable()
    {
        return StatusCode(
            StatusCodes.Status502BadGateway,
            new ProblemDetails
            {
                Status = StatusCodes.Status502BadGateway,
                Title = "Analiz servisine ulaşılamıyor",
                Detail = "Servis şu anda yanıt vermiyor. Biraz sonra tekrar deneyin."
            });
    }
}
