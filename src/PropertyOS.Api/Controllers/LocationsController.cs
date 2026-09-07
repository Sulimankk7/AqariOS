using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Application.Locations;

namespace PropertyOS.Api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/locations")]
[Authorize]
public sealed class LocationsController : ControllerBase
{
    [HttpGet("reverse-geocode")]
    public async Task<ActionResult<ReverseGeocodingResult>> ReverseGeocode(
        [FromQuery] decimal latitude, [FromQuery] decimal longitude, [FromQuery] string language,
        [FromServices] IReverseGeocodingService service, CancellationToken cancellationToken)
    {
        if (latitude is < -90 or > 90) ModelState.AddModelError(nameof(latitude), "Latitude must be between -90 and 90.");
        if (longitude is < -180 or > 180) ModelState.AddModelError(nameof(longitude), "Longitude must be between -180 and 180.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await service.ReverseAsync(latitude, longitude, language, cancellationToken);
        return result is null ? Problem(statusCode: 503, title: "Location enrichment unavailable", detail: "The address could not be determined automatically.") : Ok(result);
    }
}
