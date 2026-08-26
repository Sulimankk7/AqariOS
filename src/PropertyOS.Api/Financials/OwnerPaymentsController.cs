using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Application.Financials.Commands.ApprovePaymentSubmission;
using PropertyOS.Application.Financials.Commands.RejectPaymentSubmission;

namespace PropertyOS.Api.Financials;

[ApiController]
[Route("api/v1/owner/payments")]
[Authorize]
public class OwnerPaymentsController : ControllerBase
{
    private readonly ISender _sender;

    public OwnerPaymentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("pending-verifications")]
    [Authorize(Policy = PropertyOS.Application.Common.Security.PlatformPermissions.PaymentsApprove)]
    [ProducesResponseType(typeof(PropertyOS.Application.Common.Models.KeysetPage<PropertyOS.Application.Financials.Queries.GetPendingPaymentVerifications.PaymentVerificationQueueItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetPendingVerifications(
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = 50)
    {
        var query = new PropertyOS.Application.Financials.Queries.GetPendingPaymentVerifications.GetPendingPaymentVerificationsQuery(cursor, pageSize);
        var result = await _sender.Send(query);
        return Ok(result);
    }

    [HttpPost("{id:guid}/submissions/{submissionId:guid}/approve")]
    [Authorize(Policy = PropertyOS.Application.Common.Security.PlatformPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ApproveSubmission(
        [FromRoute] Guid id,
        [FromRoute] Guid submissionId)
    {
        var command = new ApprovePaymentSubmissionCommand(submissionId);
        await _sender.Send(command);
        return NoContent();
    }

    [HttpPost("{id:guid}/submissions/{submissionId:guid}/reject")]
    [Authorize(Policy = PropertyOS.Application.Common.Security.PlatformPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RejectSubmission(
        [FromRoute] Guid id,
        [FromRoute] Guid submissionId,
        [FromBody] RejectPaymentSubmissionRequest request)
    {
        var command = new RejectPaymentSubmissionCommand(submissionId, request.Reason);
        await _sender.Send(command);
        return NoContent();
    }
}

public record RejectPaymentSubmissionRequest(string Reason);
