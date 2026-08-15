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
using PropertyOS.Application.Leasing.Commands.CreateTenant;
using PropertyOS.Application.Leasing.Commands.DeleteTenant;
using PropertyOS.Application.Leasing.Commands.UpdateTenant;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetLeaseHistoryForTenant;
using PropertyOS.Application.Leasing.Queries.GetTenantById;
using PropertyOS.Application.Leasing.Queries.SearchTenants;
using PropertyOS.Application.Leasing.Security;
using Microsoft.Extensions.Hosting;
using PropertyOS.Application.Leasing.Commands.ProvisionTenantAccount;

namespace PropertyOS.Api.Leasing;

/// <summary>
/// API controller managing tenant person records and tenant lease history lookup.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class TenantsController : ControllerBase
{
    private readonly ISender _mediator;

    /// <summary>
    /// Initializes a new instance of TenantsController.
    /// </summary>
    /// <param name="mediator">The MediatR sender instance.</param>
    public TenantsController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Creates a new tenant person record.
    /// </summary>
    /// <param name="request">Tenant creation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created tenant.</returns>
    [HttpPost("api/v{version:apiVersion}/leasing/tenants")]
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateTenantCommand(
            Name: request.Name,
            NationalId: request.NationalId,
            Phone: request.Phone,
            Email: request.Email,
            Occupation: request.Occupation,
            Employer: request.Employer
        );

        var tenantId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { tenantId }, tenantId);
    }

    /// <summary>
    /// Gets a tenant person record by unique identifier.
    /// </summary>
    /// <param name="tenantId">Tenant unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tenant details including family members, emergency contacts, and vehicles.</returns>
    [HttpGet("api/v{version:apiVersion}/leasing/tenants/{tenantId:guid}")]
    [ProducesResponseType(typeof(TenantDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTenantByIdQuery(TenantId: tenantId);
        var result = await _mediator.Send(query, cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Updates an existing tenant person record.
    /// </summary>
    /// <param name="tenantId">Tenant unique identifier.</param>
    /// <param name="request">Tenant update parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("api/v{version:apiVersion}/leasing/tenants/{tenantId:guid}")]
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid tenantId,
        [FromBody] UpdateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateTenantCommand(
            TenantId: tenantId,
            Name: request.Name,
            NationalId: request.NationalId,
            Phone: request.Phone,
            Email: request.Email,
            Occupation: request.Occupation,
            Employer: request.Employer
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Soft-deletes a tenant person record. Rejected while the tenant is referenced by any
    /// draft, pending, or active lease contract.
    /// </summary>
    /// <param name="tenantId">Tenant unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("api/v{version:apiVersion}/leasing/tenants/{tenantId:guid}")]
    [Authorize(Policy = LeasingPermissions.Create)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteTenantCommand(TenantId: tenantId);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Searches tenants by name, national ID, or phone.
    /// </summary>
    /// <param name="searchTerm">Search query string; empty returns all tenants.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching tenants.</returns>
    [HttpGet("api/v{version:apiVersion}/leasing/tenants")]
    [ProducesResponseType(typeof(List<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search(
        [FromQuery] string searchTerm = "",
        CancellationToken cancellationToken = default)
    {
        var query = new SearchTenantsQuery(SearchTerm: searchTerm);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets lease contract history for a specific tenant.
    /// </summary>
    /// <param name="tenantId">Tenant unique identifier.</param>
    /// <param name="pageSize">Maximum number of results to return (default 50, max 200).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of historical lease contracts for the tenant.</returns>
    [HttpGet("api/v{version:apiVersion}/leasing/tenants/{tenantId:guid}/leases")]
    [ProducesResponseType(typeof(List<LeaseContractDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetLeaseHistoryByTenant(
        [FromRoute] Guid tenantId,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetLeaseHistoryForTenantQuery(TenantId: tenantId, PageSize: pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Provisions a User identity account for an existing tenant person record.
    /// Staff/Manager authorized endpoint.
    /// </summary>
    /// <param name="tenantId">Tenant person record unique identifier.</param>
    /// <param name="request">Account provisioning options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Provisioning response containing tenant user identifiers and activation state.</returns>
    [HttpPost("api/v{version:apiVersion}/leasing/tenants/{tenantId:guid}/account")]
    [Authorize(Policy = PropertyOS.Application.Leasing.Security.LeasingPermissions.Create)]
    [ProducesResponseType(typeof(ProvisionTenantAccountResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ProvisionAccount(
        [FromRoute] Guid tenantId,
        [FromBody] ProvisionTenantAccountRequest? request,
        CancellationToken cancellationToken = default)
    {
        var command = new ProvisionTenantAccountCommand(
            TenantId: tenantId,
            ContactMethod: request?.ContactMethod ?? TenantProvisioningContactMethod.Phone,
            Phone: request?.Phone,
            Email: request?.Email
        );

        var response = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { tenantId = response.TenantId }, response);
    }
}
