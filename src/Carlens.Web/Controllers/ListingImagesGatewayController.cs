using Carlens.Web.Security;
using Carlens.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Carlens.Web.Controllers;

[ApiController]
[Route("api/listing-images")]
public sealed class ListingImagesGatewayController : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GetAsync(
        Guid id,
        [FromServices] IListingAnalysisApiClient apiClient,
        [FromServices] IAnalysisAccessStore accessStore,
        CancellationToken cancellationToken)
    {
        if (!accessStore.CanAccessImage(HttpContext.Session, id))
        {
            return NotFound();
        }

        try
        {
            var image = await apiClient.GetImageAsync(id, cancellationToken);
            return image is null
                ? NotFound()
                : File(image.Content, image.ContentType);
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway);
        }
    }
}
