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
using PropertyOS.Application.Leasing.Commands.CreateTenantFamilyMember;
using PropertyOS.Application.Leasing.Commands.DeleteTenantFamilyMember;
using PropertyOS.Application.Leasing.Commands.UpdateTenantFamilyMember;
using PropertyOS.Application.Leasing.Queries.GetFamilyMemberById;
using PropertyOS.Application.Leasing.Queries.GetFamilyMembersForTenant;
using PropertyOS.Application.Leasing.Queries.GetTenantById;
using PropertyOS.Application.Leasing.Security;

namespace PropertyOS.Api.Leasing;

/// <summary>
/// API controller managing family member child entities for tenants.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class TenantFamilyMembersController : ControllerBase
{
    private readonly ISender _mediator;

    public TenantFamilyMembersController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Gets all family members for a specific tenant.
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/leasing/tenants/{tenantId:guid}/family-members")]
    [ProducesResponseType(typeof(List<TenantFamilyMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFamilyMembers(
        [FromRoute] Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetFamilyMembersForTenantQuery(TenantId: tenantId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a specific family member by unique identifier for a tenant.
    /// </summary>
    [HttpGet("api/v{version:apiVersion}/leasing/tenants/{tenantId:guid}/family-members/{familyMemberId:guid}")]
    [ProducesResponseType(typeof(TenantFamilyMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFamilyMemberById(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid familyMemberId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetFamilyMemberByIdQuery(TenantId: tenantId, FamilyMemberId: familyMemberId);
        var result = await _mediator.Send(query, cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Adds a new family member to a tenant profile.
    /// </summary>
    [HttpPost("api/v{version:apiVersion}/leasing/tenants/{tenantId:guid}/family-members")]
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateFamilyMember(
        [FromRoute] Guid tenantId,
        [FromBody] CreateTenantFamilyMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateTenantFamilyMemberCommand(
            TenantId: tenantId,
            Name: request.Name,
            RelationshipType: request.RelationshipType,
            AgeBracket: request.AgeBracket
        );

        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetFamilyMemberById), new { tenantId, familyMemberId = id }, id);
    }

    /// <summary>
    /// Updates an existing family member record.
    /// </summary>
    [HttpPut("api/v{version:apiVersion}/leasing/tenants/{tenantId:guid}/family-members/{familyMemberId:guid}")]
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFamilyMember(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid familyMemberId,
        [FromBody] UpdateTenantFamilyMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateTenantFamilyMemberCommand(
            TenantId: tenantId,
            FamilyMemberId: familyMemberId,
            Name: request.Name,
            RelationshipType: request.RelationshipType,
            AgeBracket: request.AgeBracket
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Soft-deletes a family member record.
    /// </summary>
    [HttpDelete("api/v{version:apiVersion}/leasing/tenants/{tenantId:guid}/family-members/{familyMemberId:guid}")]
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFamilyMember(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid familyMemberId,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteTenantFamilyMemberCommand(
            TenantId: tenantId,
            FamilyMemberId: familyMemberId
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
