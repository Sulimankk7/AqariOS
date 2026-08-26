using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Application.UtilityBills.Queries.GetMyUtilityAccounts;
using PropertyOS.Application.UtilityBills.Queries.GetMyUtilityBills;
using PropertyOS.Application.UtilityBills.Queries.GetUtilityDashboardSummary;
using PropertyOS.Application.UtilityBills.Commands.TenantLinkUtilityAccount;
using PropertyOS.Application.UtilityBills.Commands.TenantReplaceUtilityAccount;
using PropertyOS.Application.UtilityBills.Commands.TenantRequestUtilityAccountSync;
using PropertyOS.Application.UtilityBills.Commands.TenantUnlinkUtilityAccount;
using PropertyOS.Application.UtilityBills.Queries.Common;
using PropertyOS.Application.Common.Security;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Api.UtilityBills;

/// <summary>
/// Tenant-facing endpoints for viewing utility bills and account information.
///
/// Security: all queries derive tenantId from JWT claims (ITenantContext).
/// No tenant ID is accepted from request parameters — tenants can only see their own data.
///
/// All responses are read from PostgreSQL; no provider calls are triggered.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize(Roles = "TENANT", Policy = PlatformPermissions.TenantPortalAccess)]
[Route("api/v{version:apiVersion}/utility-bills/my")]
[Produces("application/json", "application/problem+json")]
public sealed class TenantPortalUtilityBillsController : ControllerBase
{
    private readonly ISender _mediator;

    public TenantPortalUtilityBillsController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Returns all linked utility accounts for the requesting tenant with their latest bill.
    /// Returns an empty list if no accounts are linked.
    /// </summary>
    [HttpGet("accounts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyUtilityAccounts(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyUtilityAccountsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Links an electricity or water account to the authenticated tenant's single
    /// eligible active lease. Tenant, company, lease, and property identifiers are
    /// resolved exclusively by the server from authenticated context.
    /// </summary>
    [HttpPost("accounts")]
    [ProducesResponseType(typeof(UtilityAccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> LinkMyUtilityAccount(
        [FromBody] TenantLinkUtilityAccountRequest request,
        CancellationToken cancellationToken)
    {
        var accountId = await _mediator.Send(
            new TenantLinkUtilityAccountCommand(
                request.UtilityType,
                request.AccountNumber,
                request.MeterNumber),
            cancellationToken);

        var accounts = await _mediator.Send(
            new GetMyUtilityAccountsQuery(), cancellationToken);
        var account = accounts.Single(a => a.Id == accountId);

        return CreatedAtAction(nameof(GetMyUtilityAccounts), account);
    }

    [HttpPost("accounts/{id:guid}/replace")]
    [ProducesResponseType(typeof(UtilityAccountDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ReplaceMyUtilityAccount(
        Guid id,
        [FromBody] ReplaceUtilityAccountRequest request,
        CancellationToken cancellationToken)
    {
        var resultingAccountId = await _mediator.Send(
            new TenantReplaceUtilityAccountCommand(
                id, request.AccountNumber, request.MeterNumber),
            cancellationToken);
        var accounts = await _mediator.Send(
            new GetMyUtilityAccountsQuery(), cancellationToken);
        return Accepted(accounts.Single(account => account.Id == resultingAccountId));
    }

    [HttpDelete("accounts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UnlinkMyUtilityAccount(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new TenantUnlinkUtilityAccountCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("accounts/{id:guid}/sync")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SyncMyUtilityAccount(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new TenantRequestUtilityAccountSyncCommand(id), cancellationToken);
        return Accepted();
    }

    /// <summary>
    /// Returns utility bill history for the requesting tenant (newest first).
    /// Optionally filters by electricity or water.
    /// </summary>
    [HttpGet("bills")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyUtilityBills(
        [FromQuery] UtilityType? utilityType = null,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetMyUtilityBillsQuery(utilityType, pageSize, cursor),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns a lightweight utility summary for the tenant dashboard widget.
    /// Indicates whether each utility type is linked and shows the latest bill.
    /// Never triggers provider calls — reads from PostgreSQL only.
    /// </summary>
    [HttpGet("dashboard-summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUtilityDashboardSummary(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUtilityDashboardSummaryQuery(), cancellationToken);
        return Ok(result);
    }
}
