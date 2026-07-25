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
using PropertyOS.Application.Properties.Apartments.Commands.ArchiveApartment;
using PropertyOS.Application.Properties.Apartments.Commands.CreateApartment;
using PropertyOS.Application.Properties.Apartments.Commands.UpdateApartment;
using PropertyOS.Application.Properties.Apartments.Queries.Common;
using PropertyOS.Application.Properties.Apartments.Queries.GetApartmentById;
using PropertyOS.Application.Properties.Apartments.Queries.ListApartments;
using PropertyOS.Application.Properties.Security;

namespace PropertyOS.Api.Controllers;

/// <summary>
/// API controller managing building apartments/units.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class ApartmentsController : ControllerBase
{
    private readonly ISender _mediator;

    /// <summary>
    /// Initializes a new instance of ApartmentsController.
    /// </summary>
    /// <param name="mediator">The MediatR sender instance.</param>
    public ApartmentsController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Creates a new apartment/unit inside a floor.
    /// </summary>
    /// <param name="floorId">Parent floor unique identifier.</param>
    /// <param name="request">Apartment creation parameters.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Apartment created successfully.</response>
    /// <response code="400">If request payload validation fails.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="403">If user lacks properties.create permission.</response>
    /// <response code="404">If floor is not found or belongs to another tenant.</response>
    /// <response code="409">If unit number conflicts on the floor.</response>
    /// <response code="422">If business rule validation fails (e.g. invalid ownership data).</response>
    [HttpPost("api/v{version:apiVersion}/floors/{floorId:guid}/apartments")]
    [Authorize(Policy = PropertyPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromRoute] Guid floorId,
        [FromBody] CreateApartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateApartmentCommand(
            FloorId: floorId,
            UnitNumber: request.UnitNumber,
            AreaSqm: request.AreaSqm,
            OwnershipStatus: request.OwnershipStatus,
            ExternalOwnerName: request.ExternalOwnerName,
            ExternalOwnerPhone: request.ExternalOwnerPhone,
            Bedrooms: request.Bedrooms,
            Bathrooms: request.Bathrooms,
            BaseRentAmount: request.BaseRentAmount,
            BaseRentCurrency: request.BaseRentCurrency
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Lists apartments across floors, optionally filtered by buildingId or floorId.
    /// </summary>
    /// <param name="buildingId">Optional building filter.</param>
    /// <param name="floorId">Optional floor filter.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>List of apartment DTOs.</returns>
    /// <response code="200">Returns list of apartments.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="403">If user lacks properties.read permission.</response>
    [HttpGet("api/v{version:apiVersion}/apartments")]
    [Authorize(Policy = PropertyPermissions.Read)]
    [ProducesResponseType(typeof(List<ApartmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<ApartmentDto>>> List(
        [FromQuery] Guid? buildingId = null,
        [FromQuery] Guid? floorId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ListApartmentsQuery(buildingId, floorId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves an apartment by its unique identifier.
    /// </summary>
    /// <param name="id">Apartment unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>Apartment details.</returns>
    /// <response code="200">Returns apartment details.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="403">If user lacks properties.read permission.</response>
    /// <response code="404">If apartment is not found or belongs to another tenant.</response>
    [HttpGet("api/v{version:apiVersion}/apartments/{id:guid}")]
    [Authorize(Policy = PropertyPermissions.Read)]
    [ProducesResponseType(typeof(ApartmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApartmentDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetApartmentByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates an apartment's details.
    /// </summary>
    /// <param name="id">Apartment unique identifier.</param>
    /// <param name="request">Apartment update payload.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Apartment updated successfully.</response>
    /// <response code="400">If request payload validation fails.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="403">If user lacks properties.update permission.</response>
    /// <response code="404">If apartment is not found or belongs to another tenant.</response>
    [HttpPut("api/v{version:apiVersion}/apartments/{id:guid}")]
    [Authorize(Policy = PropertyPermissions.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateApartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateApartmentCommand(
            Id: id,
            BaseRentAmount: request.BaseRentAmount,
            BaseRentCurrency: request.BaseRentCurrency
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Soft-deletes/archives an apartment.
    /// </summary>
    /// <param name="id">Apartment unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token passed from request.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Apartment archived successfully.</response>
    /// <response code="401">If unauthenticated.</response>
    /// <response code="403">If user lacks properties.delete permission.</response>
    /// <response code="404">If apartment is not found or belongs to another tenant.</response>
    [HttpDelete("api/v{version:apiVersion}/apartments/{id:guid}")]
    [Authorize(Policy = PropertyPermissions.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new ArchiveApartmentCommand(id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
