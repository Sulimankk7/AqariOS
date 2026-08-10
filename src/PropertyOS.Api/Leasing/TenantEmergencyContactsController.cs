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
using PropertyOS.Application.Leasing.Commands.CreateTenantEmergencyContact;
using PropertyOS.Application.Leasing.Commands.DeleteTenantEmergencyContact;
using PropertyOS.Application.Leasing.Commands.UpdateTenantEmergencyContact;
using PropertyOS.Application.Leasing.Queries.GetEmergencyContactById;
using PropertyOS.Application.Leasing.Queries.GetEmergencyContactsForTenant;
using PropertyOS.Application.Leasing.Queries.GetTenantById;
using PropertyOS.Application.Leasing.Security;

namespace PropertyOS.Api.Leasing;

/// <summary>
/// API controller managing emergency contact child entities for tenants.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class TenantEmergencyContactsController : ControllerBase
{
    private readonly ISender _mediator;

    public TenantEmergencyContactsController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Gets all emergency contacts for a specific tenant.
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/leasing/tenants/{tenantId:guid}/emergency-contacts")]
    [ProducesResponseType(typeof(List<TenantEmergencyContactDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmergencyContacts(
        [FromRoute] Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEmergencyContactsForTenantQuery(TenantId: tenantId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a specific emergency contact by unique identifier for a tenant.
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/leasing/tenants/{tenantId:guid}/emergency-contacts/{contactId:guid}")]
    [ProducesResponseType(typeof(TenantEmergencyContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmergencyContactById(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid contactId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEmergencyContactByIdQuery(TenantId: tenantId, ContactId: contactId);
        var result = await _mediator.Send(query, cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Adds a new emergency contact to a tenant profile.
    /// </summary>
    [HttpPost("api/v{version:apiVersion}/leasing/tenants/{tenantId:guid}/emergency-contacts")]
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateEmergencyContact(
        [FromRoute] Guid tenantId,
        [FromBody] CreateTenantEmergencyContactRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateTenantEmergencyContactCommand(
            TenantId: tenantId,
            Name: request.Name,
            RelationshipType: request.RelationshipType,
            Phone: request.Phone
        );

        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetEmergencyContactById), new { tenantId, contactId = id }, id);
    }

    /// <summary>
    /// Updates an existing emergency contact record.
    /// </summary>
    [HttpPut("api/v{version:apiVersion}/leasing/tenants/{tenantId:guid}/emergency-contacts/{contactId:guid}")]
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEmergencyContact(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid contactId,
        [FromBody] UpdateTenantEmergencyContactRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateTenantEmergencyContactCommand(
            TenantId: tenantId,
            ContactId: contactId,
            Name: request.Name,
            RelationshipType: request.RelationshipType,
            Phone: request.Phone
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Soft-deletes an emergency contact record.
    /// </summary>
    [HttpDelete("api/v{version:apiVersion}/leasing/tenants/{tenantId:guid}/emergency-contacts/{contactId:guid}")]
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEmergencyContact(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid contactId,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteTenantEmergencyContactCommand(
            TenantId: tenantId,
            ContactId: contactId
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
