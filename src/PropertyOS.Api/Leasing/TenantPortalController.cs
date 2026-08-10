using System;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Application.Common.Security;
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
}
