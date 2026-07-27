using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using PropertyOS.Api.Models.Financials;
using PropertyOS.Application.Financials.Commands.CancelEfawateercomTransaction;
using PropertyOS.Application.Financials.Commands.CreateEfawateercomTransaction;
using PropertyOS.Application.Financials.Commands.ExpireEfawateercomTransaction;
using PropertyOS.Application.Financials.Commands.MarkEfawateercomTransactionSent;
using PropertyOS.Application.Financials.Commands.PollEfawateercomTransactionStatus;
using PropertyOS.Application.Financials.Commands.ReceiveEfawateercomCallback;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials.Queries.GetEfawateercomTransactionById;
using PropertyOS.Application.Financials.Queries.GetEfawateercomTransactions;
using PropertyOS.Application.Financials.Security;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Api.Financials;

/// <summary>
/// API controller managing eFAWATEERcom gateway transactions and the inbound payment webhook (Modules 6–7).
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class EfawateercomController : ControllerBase
{
    private const string SignatureHeaderName = "X-Efawateercom-Signature";

    private static readonly JsonSerializerOptions WebhookJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ISender _mediator;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of EfawateercomController.
    /// </summary>
    /// <param name="mediator">The MediatR sender instance.</param>
    /// <param name="configuration">Application configuration (webhook secret source).</param>
    public EfawateercomController(ISender mediator, IConfiguration configuration)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Registers an outbound eFAWATEERcom transaction against a rent payment.
    /// </summary>
    /// <param name="request">Transaction creation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created on success (transaction is looked up by its external transaction ID).</returns>
    [HttpPost("api/v{version:apiVersion}/efawateercom/transactions")]
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateEfawateercomTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateEfawateercomTransactionCommand(
            RentPaymentId: request.RentPaymentId,
            ExternalTransactionId: request.ExternalTransactionId,
            Amount: request.Amount,
            PaymentReference: request.PaymentReference
        );

        await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    /// Marks a pending eFAWATEERcom transaction as sent to the gateway.
    /// </summary>
    /// <param name="id">Transaction unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("api/v{version:apiVersion}/efawateercom/transactions/{id:guid}/sent")]
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> MarkSent(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new MarkEfawateercomTransactionSentCommand(TransactionId: id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Cancels a non-terminal (Pending or Sent) eFAWATEERcom transaction.
    /// </summary>
    /// <param name="id">Transaction unique identifier.</param>
    /// <param name="request">Optional cancellation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("api/v{version:apiVersion}/efawateercom/transactions/{id:guid}/cancel")]
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancel(
        [FromRoute] Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CancelEfawateercomTransactionRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var command = new CancelEfawateercomTransactionCommand(
            TransactionId: id,
            Reason: request?.Reason
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Marks a stale (Pending or Sent) eFAWATEERcom transaction as timed out.
    /// </summary>
    /// <param name="id">Transaction unique identifier.</param>
    /// <param name="request">Optional gateway response details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("api/v{version:apiVersion}/efawateercom/transactions/{id:guid}/expire")]
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Expire(
        [FromRoute] Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ExpireEfawateercomTransactionRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var command = new ExpireEfawateercomTransactionCommand(
            TransactionId: id,
            ResponseCode: request?.ResponseCode,
            ResponseMessage: request?.ResponseMessage
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Polls the eFAWATEERcom gateway for the current status of a transaction by external ID.
    /// </summary>
    /// <param name="externalId">Gateway external transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success (no-op if the provider has no result yet).</returns>
    [HttpPost("api/v{version:apiVersion}/efawateercom/transactions/{externalId}/poll")]
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Poll(
        [FromRoute] string externalId,
        CancellationToken cancellationToken = default)
    {
        var command = new PollEfawateercomTransactionStatusCommand(ExternalTransactionId: externalId);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Gets eFAWATEERcom transactions, keyset-paginated (RequestTime DESC, Id ASC) with optional filters.
    /// </summary>
    /// <param name="status">Optional transaction status filter.</param>
    /// <param name="paymentReference">Optional payment reference filter.</param>
    /// <param name="rentPaymentId">Optional linked rent payment filter.</param>
    /// <param name="lastSeenId">Keyset cursor: ID of the last transaction on the previous page.</param>
    /// <param name="lastSeenRequestTime">Keyset cursor: request time of the last transaction on the previous page.</param>
    /// <param name="pageSize">Page size (default 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Page of eFAWATEERcom transactions.</returns>
    [HttpGet("api/v{version:apiVersion}/efawateercom/transactions")]
    [ProducesResponseType(typeof(List<EfawateercomTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(
        [FromQuery] EfawateercomStatus? status = null,
        [FromQuery] string? paymentReference = null,
        [FromQuery] Guid? rentPaymentId = null,
        [FromQuery] Guid? lastSeenId = null,
        [FromQuery] DateTimeOffset? lastSeenRequestTime = null,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEfawateercomTransactionsQuery(
            Status: status,
            PaymentReference: paymentReference,
            RentPaymentId: rentPaymentId,
            LastSeenId: lastSeenId,
            LastSeenRequestTime: lastSeenRequestTime,
            PageSize: pageSize
        );

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets an eFAWATEERcom transaction by unique identifier, including the raw webhook response.
    /// </summary>
    /// <param name="id">Transaction unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transaction details.</returns>
    [HttpGet("api/v{version:apiVersion}/efawateercom/transactions/{id:guid}")]
    [ProducesResponseType(typeof(EfawateercomTransactionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEfawateercomTransactionByIdQuery(Id: id);
        var result = await _mediator.Send(query, cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Inbound eFAWATEERcom payment callback webhook.
    /// Verifies the HMAC-SHA256 signature of the raw request body against the configured
    /// webhook secret (header <c>X-Efawateercom-Signature</c>, hex or base64 encoded).
    /// The payload shape is PROVISIONAL pending the real gateway specification.
    /// Fail-closed: returns 503 when no webhook secret is configured.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>202 Accepted when the callback has been verified and processed.</returns>
    [HttpPost("api/v{version:apiVersion}/webhooks/efawateercom")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Webhook(
        CancellationToken cancellationToken = default)
    {
        // Fail-closed: without a configured secret no signature can be verified,
        // so the webhook must not process anything.
        var secret = _configuration["Efawateercom:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "webhook not configured",
                Detail = "The eFAWATEERcom webhook secret is not configured; callbacks cannot be verified."
            });
        }

        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            rawBody = await reader.ReadToEndAsync(cancellationToken);
        }

        var signatureHeader = Request.Headers[SignatureHeaderName].ToString();
        if (string.IsNullOrWhiteSpace(signatureHeader) ||
            !VerifySignature(rawBody, signatureHeader, secret))
        {
            return Unauthorized();
        }

        // PROVISIONAL payload contract pending the real eFAWATEERcom gateway specification:
        // { externalTransactionId, status, responseTime, responseCode, responseMessage }.
        EfawateercomWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<EfawateercomWebhookPayload>(rawBody, WebhookJsonOptions);
        }
        catch (JsonException)
        {
            return Problem(
                title: "Invalid webhook payload.",
                detail: "The request body is not valid JSON.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (payload == null ||
            string.IsNullOrWhiteSpace(payload.ExternalTransactionId) ||
            string.IsNullOrWhiteSpace(payload.Status))
        {
            return Problem(
                title: "Invalid webhook payload.",
                detail: "Fields 'externalTransactionId' and 'status' are required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!Enum.TryParse<EfawateercomStatus>(payload.Status, ignoreCase: true, out var status))
        {
            return Problem(
                title: "Invalid webhook payload.",
                detail: $"Unrecognized transaction status '{payload.Status}'.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var command = new ReceiveEfawateercomCallbackCommand(
            ExternalTransactionId: payload.ExternalTransactionId,
            Status: status,
            ResponseTime: payload.ResponseTime ?? DateTimeOffset.UtcNow,
            ResponseCode: payload.ResponseCode,
            ResponseMessage: payload.ResponseMessage,
            RawResponse: rawBody
        );

        await _mediator.Send(command, cancellationToken);
        return Accepted();
    }

    /// <summary>
    /// Verifies the HMAC-SHA256 signature of the raw webhook body in constant time.
    /// Accepts the signature encoded as either hex or base64.
    /// </summary>
    private static bool VerifySignature(string rawBody, string signatureHeader, string secret)
    {
        byte[] expected;
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
        }

        var provided = signatureHeader.Trim();

        // Attempt 1: hex-encoded signature.
        if (TryDecodeHex(provided, out var hexBytes) &&
            CryptographicOperations.FixedTimeEquals(hexBytes, expected))
        {
            return true;
        }

        // Attempt 2: base64-encoded signature.
        var buffer = new byte[provided.Length];
        if (Convert.TryFromBase64String(provided, buffer, out var bytesWritten) &&
            CryptographicOperations.FixedTimeEquals(buffer.AsSpan(0, bytesWritten), expected))
        {
            return true;
        }

        return false;
    }

    private static bool TryDecodeHex(string value, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromHexString(value);
            return true;
        }
        catch (FormatException)
        {
            bytes = Array.Empty<byte>();
            return false;
        }
    }
}
