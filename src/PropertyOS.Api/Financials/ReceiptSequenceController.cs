using System;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PropertyOS.Api.Models.Financials;
using PropertyOS.Application.Financials.Commands.UpdateCompanyReceiptSequence;
using PropertyOS.Application.Financials.Security;

namespace PropertyOS.Api.Financials;

/// <summary>
/// API controller managing the current company's official receipt numbering sequence (Module 7).
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class ReceiptSequenceController : ControllerBase
{
    private readonly ISender _mediator;

    /// <summary>
    /// Initializes a new instance of ReceiptSequenceController.
    /// </summary>
    /// <param name="mediator">The MediatR sender instance.</param>
    public ReceiptSequenceController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Updates the current company's receipt numbering sequence settings.
    /// </summary>
    /// <param name="request">Receipt sequence parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("api/v{version:apiVersion}/companies/current/receipt-sequence")]
    [Authorize(Policy = FinancialsPermissions.ReceiptsIssue)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        [FromBody] UpdateCompanyReceiptSequenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateCompanyReceiptSequenceCommand(
            Prefix: request.Prefix,
            PaddingLength: request.PaddingLength,
            ResetPolicy: request.ResetPolicy
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
