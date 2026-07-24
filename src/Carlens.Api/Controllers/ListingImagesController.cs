using Carlens.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Carlens.Api.Controllers;

[ApiController]
[Route("api/listing-images")]
public sealed class ListingImagesController : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Client)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        Guid id,
        [FromServices] ICarListingRepository repository,
        CancellationToken cancellationToken)
    {
        var image = await repository.GetImageByIdAsync(id, cancellationToken);

        if (image?.Content is null || string.IsNullOrWhiteSpace(image.ContentType))
        {
            return NotFound();
        }

        return File(image.Content, image.ContentType);
    }
}
