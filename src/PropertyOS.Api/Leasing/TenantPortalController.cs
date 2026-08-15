using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Application.Common.Security;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials.Queries.GetMyPayments;
using PropertyOS.Application.Leasing.Queries.GetMyActiveLease;
using PropertyOS.Application.Leasing.Queries.GetMyTenantProfile;
using PropertyOS.Application.Leasing.Queries.GetTenantById;

namespace PropertyOS.Api.Leasing;

/// <summary>
/// API controller providing self-service portal capabilities for authenticated Tenants.
/// Context is resolved server-side from ITenantContext and ICurrentUserContext.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/tenant-portal")]
[Authorize(Policy = PlatformPermissions.TenantPortalAccess)]
[Produces("application/json", "application/problem+json")]
public class TenantPortalController : ControllerBase
{
    private readonly ISender _mediator;

    public TenantPortalController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Gets the profile details for the currently authenticated tenant.
    /// Client-supplied tenant IDs are rejected; identity is resolved from JWT claims.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tenant detail profile.</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(TenantDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken = default)
    {
        var query = new GetMyTenantProfileQuery();
        var result = await _mediator.Send(query, cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Gets the current active lease contract for the currently authenticated tenant.
    /// Client-supplied parameters are rejected; identity is resolved from JWT claims.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant's current active lease details, or 404 if no active lease exists.</returns>
    [HttpGet("lease")]
    [ProducesResponseType(typeof(TenantLeaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyActiveLease(CancellationToken cancellationToken = default)
    {
        var query = new GetMyActiveLeaseQuery();
        var result = await _mediator.Send(query, cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Gets all rent payments for the currently authenticated tenant.
    /// Client-supplied tenant IDs are rejected; identity is resolved server-side from JWT claims.
    /// Returns an empty array when no payments exist (never 404).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of rent payment records belonging to the authenticated tenant.</returns>
    [HttpGet("payments")]
    [ProducesResponseType(typeof(List<RentPaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyPayments(CancellationToken cancellationToken = default)
    {
        var query = new GetMyPaymentsQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
