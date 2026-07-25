using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Api.Models.Properties;
using PropertyOS.Application.Properties.ParkingSpots.Commands.ArchiveParkingSpot;
using PropertyOS.Application.Properties.ParkingSpots.Commands.CreateParkingSpot;
using PropertyOS.Application.Properties.ParkingSpots.Commands.UpdateParkingSpot;
using PropertyOS.Application.Properties.ParkingSpots.Queries.Common;
using PropertyOS.Application.Properties.ParkingSpots.Queries.GetParkingSpotById;
using PropertyOS.Application.Properties.ParkingSpots.Queries.ListParkingSpots;
using PropertyOS.Application.Properties.Security;

namespace PropertyOS.Api.Controllers;

/// <summary>
/// API controller managing building parking spots.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class ParkingSpotsController : ControllerBase
{
    private readonly ISender _mediator;

    /// <summary>
    /// Initializes a new instance of ParkingSpotsController.
    /// </summary>
    /// <param name="mediator">The MediatR sender instance.</param>
    public ParkingSpotsController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Creates a new parking spot inside a building.
    /// </summary>
    /// <param name="buildingId">Parent building unique identifier.</param>
    /// <param name="request">Parking spot creation parameters.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Parking spot created successfully.</response>
    /// <response code="400">If request payload validation fails.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="403">If user lacks properties.create permission.</response>
    /// <response code="404">If building is not found or belongs to another tenant.</response>
    /// <response code="409">If spot code conflicts in the building.</response>
    /// <response code="422">If business rule validation fails (e.g. invalid default apartment).</response>
    [HttpPost("api/v{version:apiVersion}/buildings/{buildingId:guid}/parking-spots")]
    [Authorize(Policy = PropertyPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromRoute] Guid buildingId,
        [FromBody] CreateParkingSpotRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateParkingSpotCommand(
            BuildingId: buildingId,
            SpotCode: request.SpotCode,
            ParkingType: request.ParkingType,
            DefaultApartmentId: request.DefaultApartmentId,
            LocationDescription: request.LocationDescription
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Lists all parking spots belonging to a specified building.
    /// </summary>
    /// <param name="buildingId">Parent building unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>List of parking spot DTOs.</returns>
    /// <response code="200">Returns list of parking spots.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="403">If user lacks properties.read permission.</response>
    /// <response code="404">If building is not found or belongs to another tenant.</response>
    [HttpGet("api/v{version:apiVersion}/buildings/{buildingId:guid}/parking-spots")]
    [Authorize(Policy = PropertyPermissions.Read)]
    [ProducesResponseType(typeof(List<ParkingSpotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ParkingSpotDto>>> ListByBuilding(
        [FromRoute] Guid buildingId,
        CancellationToken cancellationToken = default)
    {
        var query = new ListParkingSpotsQuery(buildingId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a parking spot by its unique identifier.
    /// </summary>
    /// <param name="id">Parking spot unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>Parking spot details.</returns>
    /// <response code="200">Returns parking spot details.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="403">If user lacks properties.read permission.</response>
    /// <response code="404">If spot is not found or belongs to another tenant.</response>
    [HttpGet("api/v{version:apiVersion}/parking-spots/{id:guid}")]
    [Authorize(Policy = PropertyPermissions.Read)]
    [ProducesResponseType(typeof(ParkingSpotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParkingSpotDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetParkingSpotByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates a parking spot's details.
    /// </summary>
    /// <param name="id">Parking spot unique identifier.</param>
    /// <param name="request">Parking spot update payload.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Parking spot updated successfully.</response>
    /// <response code="400">If request payload validation fails.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="403">If user lacks properties.update permission.</response>
    /// <response code="404">If spot is not found or belongs to another tenant.</response>
    [HttpPut("api/v{version:apiVersion}/parking-spots/{id:guid}")]
    [Authorize(Policy = PropertyPermissions.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateParkingSpotRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateParkingSpotCommand(
            Id: id,
            SpotCode: request.SpotCode,
            ParkingType: request.ParkingType,
            DefaultApartmentId: request.DefaultApartmentId,
            LocationDescription: request.LocationDescription
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Soft-deletes/archives a parking spot.
    /// </summary>
    /// <param name="id">Parking spot unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Parking spot archived successfully.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="403">If user lacks properties.delete permission.</response>
    /// <response code="404">If spot is not found or belongs to another tenant.</response>
    [HttpDelete("api/v{version:apiVersion}/parking-spots/{id:guid}")]
    [Authorize(Policy = PropertyPermissions.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new ArchiveParkingSpotCommand(id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
