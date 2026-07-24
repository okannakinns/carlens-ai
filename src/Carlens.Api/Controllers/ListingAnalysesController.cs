using Carlens.Application.Features.Analyses.Queries;
using Carlens.Application.Features.Listings.Commands;
using Carlens.Application.DTOs;
using Carlens.Contracts.Requests;
using Carlens.Contracts.Responses;
using Carlens.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Carlens.Api.Controllers;

[ApiController]
[Route("api/listing-analyses")]
public class ListingAnalysesController : ControllerBase
{
    private const long MaximumUrlRequestSizeBytes = 16 * 1024;
    private const long MaximumManualRequestSizeBytes = 16 * 1024 * 1024;

    private readonly GetListingAnalysisByIdQueryHandler _getByIdHandler;
    private readonly GetListingAnalysesQueryHandler _getAllHandler;

    public ListingAnalysesController(
        GetListingAnalysisByIdQueryHandler getByIdHandler,
        GetListingAnalysesQueryHandler getAllHandler)
    {
        _getByIdHandler = getByIdHandler;
        _getAllHandler = getAllHandler;
    }

    [HttpPost]
    [RequestSizeLimit(MaximumUrlRequestSizeBytes)]
    [ProducesResponseType(typeof(ListingAnalysisResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync(
        CreateListingAnalysisRequest request,
        [FromServices] CreateListingAnalysisCommandHandler createHandler,
        CancellationToken cancellationToken)
    {
        var command = new CreateListingAnalysisCommand(request.ListingUrl);

        var response = await createHandler.HandleAsync(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("manual")]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumManualRequestSizeBytes)]
    [RequestSizeLimit(MaximumManualRequestSizeBytes)]
    [ProducesResponseType(typeof(ListingAnalysisResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> CreateManualAsync(
        [FromForm] CreateManualVehicleAnalysisRequest request,
        [FromForm(Name = "images")] List<IFormFile> images,
        [FromServices] CreateManualVehicleAnalysisCommandHandler createHandler,
        CancellationToken cancellationToken)
    {
        var uploadedImages = new List<UploadedVehicleImage>(images.Count);

        foreach (var image in images)
        {
            await using var stream = image.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            uploadedImages.Add(new UploadedVehicleImage(
                Path.GetFileName(image.FileName),
                buffer.ToArray()));
        }

        var command = new CreateManualVehicleAnalysisCommand(
            request.Brand,
            request.Series,
            request.Model,
            request.ModelYear,
            request.Price,
            request.Mileage,
            (FuelType)request.FuelType,
            (TransmissionType)request.TransmissionType,
            request.Location,
            request.Description,
            request.DamageInformation,
            uploadedImages);

        var response = await createHandler.HandleAsync(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ListingAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ListingAnalysisResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetListingAnalysisByIdQuery(id);

        var response = await _getByIdHandler.HandleAsync(query, cancellationToken);

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ListingAnalysisResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        var query = new GetListingAnalysesQuery();

        var response = await _getAllHandler.HandleAsync(query, cancellationToken);

        return Ok(response);
    }

}
