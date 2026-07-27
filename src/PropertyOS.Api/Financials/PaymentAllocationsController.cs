using System;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Api.Models.Financials;
using PropertyOS.Application.Financials.Commands.RecordPaymentAllocation;
using PropertyOS.Application.Financials.Commands.ReversePaymentAllocation;
using PropertyOS.Application.Financials.Security;

namespace PropertyOS.Api.Financials;

/// <summary>
/// API controller managing payment allocations between receiving payments and obligations (Module 6).
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class PaymentAllocationsController : ControllerBase
{
    private readonly ISender _mediator;

    /// <summary>
    /// Initializes a new instance of PaymentAllocationsController.
    /// </summary>
    /// <param name="mediator">The MediatR sender instance.</param>
    public PaymentAllocationsController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Allocates a receiving payment across one or more obligation payments.
    /// </summary>
    /// <param name="request">Allocation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("api/v{version:apiVersion}/payment-allocations")]
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Record(
        [FromBody] RecordPaymentAllocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new RecordPaymentAllocationCommand(
            ReceivingPaymentId: request.ReceivingPaymentId,
            Allocations: request.Allocations.ConvertAll(
                a => new AllocationDetail(a.ObligationPaymentId, a.Amount)),
            AllocationDate: request.AllocationDate,
            Notes: request.Notes
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Reverses an active payment allocation.
    /// </summary>
    /// <param name="id">Allocation unique identifier.</param>
    /// <param name="request">Reversal parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("api/v{version:apiVersion}/payment-allocations/{id:guid}/reverse")]
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reverse(
        [FromRoute] Guid id,
        [FromBody] ReversePaymentAllocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new ReversePaymentAllocationCommand(
            AllocationId: id,
            ReversalReason: request.ReversalReason,
            Notes: request.Notes
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
