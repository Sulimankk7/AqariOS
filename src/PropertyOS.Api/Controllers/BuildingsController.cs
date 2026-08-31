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
using PropertyOS.Application.Properties.Buildings.Commands.ArchiveBuilding;
using PropertyOS.Application.Properties.Buildings.Commands.CreateBuilding;
using PropertyOS.Application.Properties.Buildings.Commands.UpdateBuilding;
using PropertyOS.Application.Properties.Buildings.Queries.Common;
using PropertyOS.Application.Properties.Buildings.Queries.GetBuildingById;
using PropertyOS.Application.Properties.Buildings.Queries.ListBuildings;
using PropertyOS.Application.Properties.Security;
using PropertyOS.Application.Common.Numbering;

namespace PropertyOS.Api.Controllers;

/// <summary>
/// API controller managing building inventory and property details.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/buildings")]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class BuildingsController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<BuildingsController> _logger;

    /// <summary>
    /// Initializes a new instance of BuildingsController.
    /// </summary>
    /// <param name="mediator">The MediatR sender instance.</param>
    /// <param name="logger">The logger instance.</param>
    public BuildingsController(ISender mediator, ILogger<BuildingsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("next-code")]
    [Authorize(Policy = PropertyPermissions.Create)]
    public async Task<ActionResult<IdentifierSuggestionDto>> GetNextCode(CancellationToken cancellationToken = default) =>
        Ok(await _mediator.Send(new GetNextIdentifierSuggestionQuery(IdentifierSuggestionKind.Building), cancellationToken));

    /// <summary>
    /// Creates a new building within the authenticated tenant's portfolio.
    /// </summary>
    /// <param name="request">Building creation parameters.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Building created successfully.</response>
    /// <response code="400">If request payload validation fails.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="403">If user lacks properties.create permission.</response>
    /// <response code="409">If internal code conflicts within company portfolio.</response>
    /// <response code="422">If business rule validation fails.</response>
    [HttpPost]
    [Authorize(Policy = PropertyPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBuildingRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[API] POST /api/v1/buildings UserId={UserId} CompanyId={CompanyId}", 
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, 
            User.FindFirst("company_id")?.Value);

        var command = new CreateBuildingCommand(
            Name: request.Name,
            TotalFloors: request.TotalFloors,
            BuildingType: request.BuildingType,
            InternalCode: request.InternalCode,
            ConstructionYear: request.ConstructionYear,
            GpsLatitude: request.GpsLatitude,
            GpsLongitude: request.GpsLongitude,
            AddressGovernorate: request.AddressGovernorate,
            AddressCity: request.AddressCity,
            AddressNeighborhood: request.AddressNeighborhood,
            AddressStreet: request.AddressStreet,
            AddressPostalCode: request.AddressPostalCode
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Retrieves a building by its unique identifier.
    /// </summary>
    /// <param name="id">Building unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>Building details.</returns>
    /// <response code="200">Returns building details.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="403">If user lacks properties.read permission.</response>
    /// <response code="404">If building is not found or belongs to another tenant.</response>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = PropertyPermissions.Read)]
    [ProducesResponseType(typeof(BuildingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BuildingDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[API] GET /api/v1/buildings/{Id} BuildingId={BuildingId} UserId={UserId} CompanyId={CompanyId}", 
            id, id, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, 
            User.FindFirst("company_id")?.Value);

        var query = new GetBuildingByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lists all active buildings in the authenticated tenant's portfolio.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>List of building DTOs.</returns>
    /// <response code="200">Returns list of buildings.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="403">If user lacks properties.read permission.</response>
    [HttpGet]
    [Authorize(Policy = PropertyPermissions.Read)]
    [ProducesResponseType(typeof(List<BuildingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<BuildingDto>>> List(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[API] GET /api/v1/buildings UserId={UserId} CompanyId={CompanyId}", 
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, 
            User.FindFirst("company_id")?.Value);

        var query = new ListBuildingsQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates an existing building's details.
    /// </summary>
    /// <param name="id">Building unique identifier.</param>
    /// <param name="request">Building update payload.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Building updated successfully.</response>
    /// <response code="400">If request payload validation fails.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="403">If user lacks properties.update permission.</response>
    /// <response code="404">If building is not found or belongs to another tenant.</response>
    /// <response code="409">If updated internal code conflicts with another building.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = PropertyPermissions.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateBuildingRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[API] PUT /api/v1/buildings/{Id} BuildingId={BuildingId} UserId={UserId} CompanyId={CompanyId}", 
            id, id, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, 
            User.FindFirst("company_id")?.Value);

        var command = new UpdateBuildingCommand(
            Id: id,
            Name: request.Name,
            BuildingType: request.BuildingType,
            InternalCode: request.InternalCode,
            ConstructionYear: request.ConstructionYear,
            GpsLatitude: request.GpsLatitude,
            GpsLongitude: request.GpsLongitude,
            AddressGovernorate: request.AddressGovernorate,
            AddressCity: request.AddressCity,
            AddressNeighborhood: request.AddressNeighborhood,
            AddressStreet: request.AddressStreet,
            AddressPostalCode: request.AddressPostalCode
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Soft-deletes/archives a building.
    /// </summary>
    /// <param name="id">Building unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Building archived successfully.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="403">If user lacks properties.delete permission.</response>
    /// <response code="404">If building is not found or belongs to another tenant.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PropertyPermissions.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[API] DELETE /api/v1/buildings/{Id} BuildingId={BuildingId} UserId={UserId} CompanyId={CompanyId}", 
            id, id, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, 
            User.FindFirst("company_id")?.Value);

        var command = new ArchiveBuildingCommand(id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
