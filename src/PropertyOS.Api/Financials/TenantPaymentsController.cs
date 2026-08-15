using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Application.Financials.Commands.SubmitPaymentRequest;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Api.Financials;

[ApiController]
[Route("api/v1/tenant/payments")]
[Authorize]
public class TenantPaymentsController : ControllerBase
{
    private readonly ISender _sender;

    public TenantPaymentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("{id:guid}/submit-verification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> SubmitVerification(
        [FromRoute] Guid id,
        [FromBody] SubmitPaymentVerificationRequest request)
    {
        var command = new SubmitPaymentRequestCommand(
            id,
            request.PaymentMethod,
            request.ReferenceNumber,
            request.ProofFileId,
            request.ChequeDetails);

        var submissionId = await _sender.Send(command);
        return Ok(submissionId);
    }
}

public record SubmitPaymentVerificationRequest(
    PaymentMethod PaymentMethod,
    string? ReferenceNumber,
    Guid? ProofFileId,
    ChequeSubmissionInput? ChequeDetails = null);
