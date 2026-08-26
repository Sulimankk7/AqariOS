using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Application.UtilityBills.Commands.LinkUtilityAccount;
using PropertyOS.Application.UtilityBills.Commands.UnlinkUtilityAccount;
using PropertyOS.Application.UtilityBills.Commands.RequestUtilityAccountSync;
using PropertyOS.Application.UtilityBills.Commands.ReplaceUtilityAccount;
using PropertyOS.Application.UtilityBills.Queries.GetUtilityAccountById;
using PropertyOS.Application.UtilityBills.Queries.GetManagementUtilityAccounts;
using PropertyOS.Application.UtilityBills.Queries.GetManagementUtilityBills;
using PropertyOS.Application.UtilityBills.Security;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Api.UtilityBills;

/// <summary>
/// API endpoints for managing utility account linkage on lease contracts.
/// Used by company admins / property managers — not by tenants directly.
/// Tenants view their bills via TenantPortalUtilityBillsController.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/utility-bills/accounts")]
[Produces("application/json", "application/problem+json")]
public sealed class UtilityAccountsController : ControllerBase
{
    private readonly ISender _mediator;

    public UtilityAccountsController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>Lists utility accounts within the authenticated company scope.</summary>
    [HttpGet]
    [Authorize(Policy = UtilityBillsPermissions.Manage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = 50,
        [FromQuery] UtilityType? utilityType = null,
        [FromQuery] UtilitySyncStatus? syncStatus = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] Guid? leaseContractId = null,
        [FromQuery] bool includeUnlinked = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetManagementUtilityAccountsQuery(
                cursor, pageSize, utilityType, syncStatus, isActive,
                leaseContractId, includeUnlinked),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Links an electricity or water utility account to a lease contract.
    /// A lease contract may have at most one electricity account and one water account.
    /// If the lease already has a linked account of the specified type, returns 409 Conflict.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = UtilityBillsPermissions.Manage)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Link(
        [FromBody] LinkUtilityAccountRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LinkUtilityAccountCommand(
            request.LeaseContractId,
            request.UtilityType,
            request.AccountNumber,
            request.MeterNumber);

        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPost("{id:guid}/replace")]
    [Authorize(Policy = UtilityBillsPermissions.Manage)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Replace(
        Guid id,
        [FromBody] ReplaceUtilityAccountRequest request,
        CancellationToken cancellationToken)
    {
        var resultingAccountId = await _mediator.Send(
            new ReplaceUtilityAccountCommand(
                id, request.AccountNumber, request.MeterNumber),
            cancellationToken);
        return AcceptedAtAction(nameof(GetById), new { id = resultingAccountId }, resultingAccountId);
    }

    /// <summary>
    /// Soft-deletes a utility account, stopping all future automatic synchronisation.
    /// Existing bill history is preserved for audit purposes.
    /// Cannot unlink while a sync is in progress.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = UtilityBillsPermissions.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Unlink(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UnlinkUtilityAccountCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = UtilityBillsPermissions.Manage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetUtilityAccountByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns bounded bill history for a company-scoped utility account.</summary>
    [HttpGet("{id:guid}/bills")]
    [Authorize(Policy = UtilityBillsPermissions.Manage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBills(
        Guid id,
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = 50,
        [FromQuery] UtilityBillPaymentStatus? paymentStatus = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetManagementUtilityBillsQuery(id, cursor, pageSize, paymentStatus),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/sync")]
    [Authorize(Policy = UtilityBillsPermissions.Manage)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Sync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new RequestUtilityAccountSyncCommand(id), cancellationToken);
        return Accepted();
    }
}
