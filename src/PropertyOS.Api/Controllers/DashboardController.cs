using System;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Application.Dashboard.DTOs;
using PropertyOS.Application.Dashboard.Queries.GetDashboardSummary;

namespace PropertyOS.Api.Controllers;

/// <summary>
/// API controller providing high-performance, read-only Dashboard summary KPI metrics.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of DashboardController.
    /// </summary>
    /// <param name="mediator">MediatR dispatcher instance.</param>
    public DashboardController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Retrieves aggregated dashboard summary metrics for the current company tenant.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token passed from HTTP request pipeline.</param>
    /// <returns>Dashboard summary metrics DTO containing property, leasing, payment, and financial KPIs.</returns>
    /// <response code="200">Returns aggregated dashboard summary DTO.</response>
    /// <response code="401">If unauthenticated or tenant company context is missing/invalid.</response>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken cancellationToken = default)
    {
        var query = new GetDashboardSummaryQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
