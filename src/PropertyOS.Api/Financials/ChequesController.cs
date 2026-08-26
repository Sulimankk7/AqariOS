using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Api.Models.Financials;
using PropertyOS.Application.Financials.Commands.RecordChequeStatusChange;
using PropertyOS.Application.Financials.Queries.GetChequesByStatus;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;
using PropertyOS.Application.Financials.Queries.GetUpcomingCheques;
using PropertyOS.Application.Financials.Security;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Api.Financials;

/// <summary>
/// API controller managing cheque lifecycle and cheque queries (Module 6).
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class ChequesController : ControllerBase
{
    private readonly ISender _mediator;

    /// <summary>
    /// Initializes a new instance of ChequesController.
    /// </summary>
    /// <param name="mediator">The MediatR sender instance.</param>
    public ChequesController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Records a cheque lifecycle status change (received, deposited, cleared, bounced, cancelled).
    /// </summary>
    /// <param name="id">Cheque unique identifier.</param>
    /// <param name="request">Status change parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("api/v{version:apiVersion}/cheques/{id:guid}/status")]
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RecordStatusChange(
        [FromRoute] Guid id,
        [FromBody] RecordChequeStatusChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new RecordChequeStatusChangeCommand(
            ChequeId: id,
            NewStatus: request.NewStatus,
            ActionDate: request.ActionDate,
            BounceReason: request.BounceReason,
            BounceFeeCharged: request.BounceFeeCharged,
            CancellationReason: request.CancellationReason,
            ReplacementChequeId: request.ReplacementChequeId,
            Notes: request.Notes
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets cheques optionally filtered by lifecycle status. When status is omitted, returns cheques across all statuses.
    /// </summary>
    /// <param name="status">Optional cheque status filter.</param>
    /// <param name="pageSize">Maximum number of results to return (default 50, max 200).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of cheques for the authenticated company.</returns>
    [HttpGet("api/v{version:apiVersion}/cheques")]
    [Authorize(Policy = FinancialsPermissions.ChequesRead)]
    [ProducesResponseType(typeof(List<ChequeDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCheques(
        [FromQuery] ChequeStatus? status = null,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetChequesQuery(Status: status, PageSize: pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets cheques whose due date falls within the specified number of days ahead.
    /// </summary>
    /// <param name="daysAhead">Number of days to look ahead (default 30).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of upcoming cheques.</returns>
    [HttpGet("api/v{version:apiVersion}/cheques/upcoming")]
    [Authorize(Policy = FinancialsPermissions.ChequesRead)]
    [ProducesResponseType(typeof(List<ChequeDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUpcoming(
        [FromQuery] int daysAhead = 30,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUpcomingChequesQuery(DaysAhead: daysAhead);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
