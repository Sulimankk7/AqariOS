using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Api.Models.Leasing;
using PropertyOS.Application.Leasing.Commands.CreateTenantVehicle;
using PropertyOS.Application.Leasing.Commands.DeleteTenantVehicle;
using PropertyOS.Application.Leasing.Commands.UpdateTenantVehicle;
using PropertyOS.Application.Leasing.Queries.GetTenantById;
using PropertyOS.Application.Leasing.Queries.GetVehicleById;
using PropertyOS.Application.Leasing.Queries.GetVehiclesForTenant;
using PropertyOS.Application.Leasing.Security;

namespace PropertyOS.Api.Leasing;

/// <summary>
/// API controller managing vehicle child entities for tenants.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class TenantVehiclesController : ControllerBase
{
    private readonly ISender _mediator;

    public TenantVehiclesController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Gets all vehicles for a specific tenant.
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/leasing/tenants/{tenantId:guid}/vehicles")]
    [ProducesResponseType(typeof(List<TenantVehicleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVehicles(
        [FromRoute] Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetVehiclesForTenantQuery(TenantId: tenantId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a specific vehicle by unique identifier for a tenant.
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/leasing/tenants/{tenantId:guid}/vehicles/{vehicleId:guid}")]
    [ProducesResponseType(typeof(TenantVehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVehicleById(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetVehicleByIdQuery(TenantId: tenantId, VehicleId: vehicleId);
        var result = await _mediator.Send(query, cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Adds a new vehicle to a tenant profile.
    /// </summary>
    [HttpPost("api/v{version:apiVersion}/leasing/tenants/{tenantId:guid}/vehicles")]
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateVehicle(
        [FromRoute] Guid tenantId,
        [FromBody] CreateTenantVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateTenantVehicleCommand(
            TenantId: tenantId,
            PlateNumber: request.PlateNumber,
            MakeModel: request.MakeModel,
            Color: request.Color
        );

        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetVehicleById), new { tenantId, vehicleId = id }, id);
    }

    /// <summary>
    /// Updates an existing vehicle record.
    /// </summary>
    [HttpPut("api/v{version:apiVersion}/leasing/tenants/{tenantId:guid}/vehicles/{vehicleId:guid}")]
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateVehicle(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid vehicleId,
        [FromBody] UpdateTenantVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateTenantVehicleCommand(
            TenantId: tenantId,
            VehicleId: vehicleId,
            PlateNumber: request.PlateNumber,
            MakeModel: request.MakeModel,
            Color: request.Color
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Soft-deletes a vehicle record.
    /// </summary>
    [HttpDelete("api/v{version:apiVersion}/leasing/tenants/{tenantId:guid}/vehicles/{vehicleId:guid}")]
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVehicle(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteTenantVehicleCommand(
            TenantId: tenantId,
            VehicleId: vehicleId
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
