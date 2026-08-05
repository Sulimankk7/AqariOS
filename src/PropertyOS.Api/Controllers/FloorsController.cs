using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PropertyOS.Api.Models.Properties;
using PropertyOS.Application.Properties.Floors.Commands.ArchiveFloor;
using PropertyOS.Application.Properties.Floors.Commands.CreateFloor;
using PropertyOS.Application.Properties.Floors.Commands.UpdateFloor;
using PropertyOS.Application.Properties.Floors.Queries.Common;
using PropertyOS.Application.Properties.Floors.Queries.GetFloorById;
using PropertyOS.Application.Properties.Floors.Queries.ListFloors;
using PropertyOS.Application.Properties.Security;

namespace PropertyOS.Api.Controllers;

/// <summary>
/// API controller managing building floors.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class FloorsController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<FloorsController> _logger;

    /// <summary>
    /// Initializes a new instance of FloorsController.
    /// </summary>
    /// <param name="mediator">The MediatR sender instance.</param>
    /// <param name="logger">The logger instance.</param>
    public FloorsController(ISender mediator, ILogger<FloorsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new floor inside a building.
    /// </summary>
    /// <param name="buildingId">Parent building unique identifier.</param>
    /// <param name="request">Floor creation parameters.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Floor created successfully.</response>
    /// <response code="400">If request payload validation fails.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="403">If user lacks properties.create permission.</response>
    /// <response code="404">If building is not found or belongs to another tenant.</response>
    /// <response code="409">If floor number conflicts in the building.</response>
    /// <response code="422">If business rule validation fails (e.g. building inactive).</response>
    [HttpPost("api/v{version:apiVersion}/buildings/{buildingId:guid}/floors")]
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
        [FromBody] CreateFloorRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[API] POST /api/v1/buildings/{BuildingId}/floors UserId={UserId} CompanyId={CompanyId}", 
            buildingId, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, 
            User.FindFirst("company_id")?.Value);

        var command = new CreateFloorCommand(
            BuildingId: buildingId,
            FloorNumber: request.FloorNumber,
            FloorLabel: request.FloorLabel,
            FloorType: request.FloorType
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Lists all floors belonging to a specified building.
    /// </summary>
    /// <param name="buildingId">Parent building unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>List of floor DTOs.</returns>
    /// <response code="200">Returns list of floors.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="403">If user lacks properties.read permission.</response>
    /// <response code="404">If building is not found or belongs to another tenant.</response>
    [HttpGet("api/v{version:apiVersion}/buildings/{buildingId:guid}/floors")]
    [Authorize(Policy = PropertyPermissions.Read)]
    [ProducesResponseType(typeof(List<FloorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<FloorDto>>> ListByBuilding(
        [FromRoute] Guid buildingId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[API] GET /api/v1/buildings/{BuildingId}/floors UserId={UserId} CompanyId={CompanyId}", 
            buildingId, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, 
            User.FindFirst("company_id")?.Value);

        var query = new ListFloorsQuery(buildingId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a floor by its unique identifier.
    /// </summary>
    /// <param name="id">Floor unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>Floor details.</returns>
    /// <response code="200">Returns floor details.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="403">If user lacks properties.read permission.</response>
    /// <response code="404">If floor is not found or belongs to another tenant.</response>
    [HttpGet("api/v{version:apiVersion}/floors/{id:guid}")]
    [Authorize(Policy = PropertyPermissions.Read)]
    [ProducesResponseType(typeof(FloorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FloorDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[API] GET /api/v1/floors/{Id} FloorId={FloorId} UserId={UserId} CompanyId={CompanyId}", 
            id, id, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, 
            User.FindFirst("company_id")?.Value);

        var query = new GetFloorByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates a floor's label or type.
    /// </summary>
    /// <param name="id">Floor unique identifier.</param>
    /// <param name="request">Floor update payload.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Floor updated successfully.</response>
    /// <response code="400">If request payload validation fails.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="403">If user lacks properties.update permission.</response>
    /// <response code="404">If floor is not found or belongs to another tenant.</response>
    [HttpPut("api/v{version:apiVersion}/floors/{id:guid}")]
    [Authorize(Policy = PropertyPermissions.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateFloorRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[API] PUT /api/v1/floors/{Id} FloorId={FloorId} UserId={UserId} CompanyId={CompanyId}", 
            id, id, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, 
            User.FindFirst("company_id")?.Value);

        var command = new UpdateFloorCommand(
            Id: id,
            FloorLabel: request.FloorLabel,
            FloorType: request.FloorType
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Soft-deletes/archives a floor.
    /// </summary>
    /// <param name="id">Floor unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Floor archived successfully.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="403">If user lacks properties.delete permission.</response>
    /// <response code="404">If floor is not found or belongs to another tenant.</response>
    [HttpDelete("api/v{version:apiVersion}/floors/{id:guid}")]
    [Authorize(Policy = PropertyPermissions.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[API] DELETE /api/v1/floors/{Id} FloorId={FloorId} UserId={UserId} CompanyId={CompanyId}", 
            id, id, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, 
            User.FindFirst("company_id")?.Value);

        var command = new ArchiveFloorCommand(id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
