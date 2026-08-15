using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PropertyOS.Api.Models.Financials;
using PropertyOS.Application.Financials.Commands.CancelRentPayment;
using PropertyOS.Application.Financials.Commands.GenerateScheduledInstallments;
using PropertyOS.Application.Financials.Commands.IssueRentPaymentReceipt;
using PropertyOS.Application.Financials.Commands.RecordManualRentPayment;
using PropertyOS.Application.Financials.Commands.RecordPaymentAllocation;
using PropertyOS.Application.Financials.Commands.RemindRentPayment;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials.Queries.GetOutstandingRentPayments;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;
using PropertyOS.Application.Financials.Queries.GetRentPaymentReceiptByRentPaymentId;
using PropertyOS.Application.Financials.Queries.GetRentPaymentReceipts;
using PropertyOS.Application.Financials.Queries.GetRentPayments;
using PropertyOS.Application.Financials.Queries.GetRentPaymentsForLease;
using PropertyOS.Application.Financials.Queries.GetRentPaymentsForTenant;
using PropertyOS.Application.Financials.Queries.SearchRentPayments;
using PropertyOS.Application.Financials.Security;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Api.Financials;

/// <summary>
/// API controller managing rent payments, manual money-in, installments, and official receipts (Module 6).
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class RentPaymentsController : ControllerBase
{
    private readonly ISender _mediator;

    /// <summary>
    /// Initializes a new instance of RentPaymentsController.
    /// </summary>
    /// <param name="mediator">The MediatR sender instance.</param>
    public RentPaymentsController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Gets a rent payment by unique identifier, including cheque details and allocations.
    /// </summary>
    /// <param name="id">Rent payment unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Rent payment details.</returns>
    [HttpGet("api/v{version:apiVersion}/rent-payments/{id:guid}")]
    [ProducesResponseType(typeof(RentPaymentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRentPaymentByIdQuery(Id: id);
        var result = await _mediator.Send(query, cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Gets all rent payments for a specific lease contract.
    /// </summary>
    /// <param name="leaseId">Lease contract unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of rent payments for the lease contract.</returns>
    [HttpGet("api/v{version:apiVersion}/leases/{leaseId:guid}/rent-payments")]
    [ProducesResponseType(typeof(List<RentPaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetForLease(
        [FromRoute] Guid leaseId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRentPaymentsForLeaseQuery(LeaseContractId: leaseId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets all rent payments for a specific tenant.
    /// </summary>
    /// <param name="tenantId">Tenant unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of rent payments for the tenant.</returns>
    [HttpGet("api/v{version:apiVersion}/tenants/{tenantId:guid}/rent-payments")]
    [ProducesResponseType(typeof(List<RentPaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetForTenant(
        [FromRoute] Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRentPaymentsForTenantQuery(TenantId: tenantId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Searches rent payments by search term.
    /// </summary>
    /// <param name="searchTerm">Search query string.</param>
    /// <param name="pageSize">Maximum number of results to return (default 50, max 200).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching rent payments.</returns>
    [HttpGet("api/v{version:apiVersion}/rent-payments/search")]
    [ProducesResponseType(typeof(List<RentPaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search(
        [FromQuery] string searchTerm = "",
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchRentPaymentsQuery(SearchTerm: searchTerm, PageSize: pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets outstanding (not fully settled) rent payments for the current company.
    /// </summary>
    /// <param name="pageSize">Maximum number of results to return (default 50, max 200).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of outstanding rent payments.</returns>
    [HttpGet("api/v{version:apiVersion}/rent-payments/outstanding")]
    [ProducesResponseType(typeof(List<RentPaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetOutstanding(
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetOutstandingRentPaymentsQuery(PageSize: pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets rent payments, keyset-paginated (DueDate DESC, Id ASC) with optional filters.
    /// </summary>
    /// <param name="buildingId">Optional building filter.</param>
    /// <param name="status">Optional payment due date status filter.</param>
    /// <param name="dateFrom">Optional due-date lower bound (inclusive).</param>
    /// <param name="dateTo">Optional due-date upper bound (inclusive).</param>
    /// <param name="searchTerm">Optional free-text search term matching tenant name, contract number, or receipt number.</param>
    /// <param name="lastSeenId">Keyset cursor: ID of the last payment on the previous page.</param>
    /// <param name="lastSeenDueDate">Keyset cursor: due date of the last payment on the previous page.</param>
    /// <param name="pageSize">Page size (1..200, default 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Page of rent payments.</returns>
    [HttpGet("api/v{version:apiVersion}/rent-payments")]
    [ProducesResponseType(typeof(List<RentPaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(
        [FromQuery] Guid? buildingId = null,
        [FromQuery] DueDateStatus? status = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] Guid? lastSeenId = null,
        [FromQuery] DateOnly? lastSeenDueDate = null,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRentPaymentsQuery(
            BuildingId: buildingId,
            Status: status,
            DateFrom: dateFrom,
            DateTo: dateTo,
            SearchTerm: searchTerm,
            LastSeenId: lastSeenId,
            LastSeenDueDate: lastSeenDueDate,
            PageSize: pageSize
        );

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Records a manual money-in payment (Cash / BankTransfer / Cheque) against a lease contract.
    /// </summary>
    /// <param name="request">Manual payment parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created rent payment.</returns>
    [HttpPost("api/v{version:apiVersion}/rent-payments/manual")]
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RecordManual(
        [FromBody] RecordManualRentPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new RecordManualRentPaymentCommand(
            LeaseContractId: request.LeaseContractId,
            Amount: request.Amount,
            PaymentMethod: request.PaymentMethod,
            PaymentReferenceNumber: request.PaymentReferenceNumber,
            Notes: request.Notes,
            Cheque: request.Cheque == null
                ? null
                : new ManualChequeDetails(
                    ChequeNumber: request.Cheque.ChequeNumber,
                    BankName: request.Cheque.BankName,
                    BankBranch: request.Cheque.BankBranch,
                    IssueDate: request.Cheque.IssueDate,
                    DueDate: request.Cheque.DueDate,
                    ReceivedDate: request.Cheque.ReceivedDate),
            Allocations: request.Allocations?.ConvertAll(
                a => new AllocationDetail(a.ObligationPaymentId, a.Amount))
        );

        var paymentId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = paymentId }, paymentId);
    }

    /// <summary>
    /// Cancels a rent payment that has no active allocations referencing it.
    /// </summary>
    /// <param name="id">Rent payment unique identifier.</param>
    /// <param name="request">Optional cancellation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("api/v{version:apiVersion}/rent-payments/{id:guid}/cancel")]
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancel(
        [FromRoute] Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CancelRentPaymentRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var command = new CancelRentPaymentCommand(
            RentPaymentId: id,
            Reason: request?.Reason
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Issues the official receipt for a fully-paid rent payment.
    /// </summary>
    /// <param name="id">Rent payment unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The formatted receipt number (e.g. "REC-00042").</returns>
    [HttpPost("api/v{version:apiVersion}/rent-payments/{id:guid}/receipt")]
    [Authorize(Policy = FinancialsPermissions.ReceiptsIssue)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> IssueReceipt(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new IssueRentPaymentReceiptCommand(RentPaymentId: id);
        var receiptNumber = await _mediator.Send(command, cancellationToken);
        return Ok(receiptNumber);
    }

    /// <summary>
    /// Gets the official receipt issued for a rent payment.
    /// </summary>
    /// <param name="id">Rent payment unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Receipt details.</returns>
    [HttpGet("api/v{version:apiVersion}/rent-payments/{id:guid}/receipt")]
    [ProducesResponseType(typeof(RentPaymentReceiptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReceipt(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRentPaymentReceiptByRentPaymentIdQuery(RentPaymentId: id);
        var result = await _mediator.Send(query, cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Gets rent payment receipts, keyset-paginated (IssueDate DESC, Id ASC) with optional filters.
    /// </summary>
    /// <param name="leaseContractId">Optional lease contract filter.</param>
    /// <param name="tenantId">Optional tenant filter.</param>
    /// <param name="dateFrom">Optional issue-date lower bound (inclusive).</param>
    /// <param name="dateTo">Optional issue-date upper bound (inclusive).</param>
    /// <param name="lastSeenId">Keyset cursor: ID of the last receipt on the previous page.</param>
    /// <param name="lastSeenIssueDate">Keyset cursor: issue date of the last receipt on the previous page.</param>
    /// <param name="pageSize">Page size (default 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Page of rent payment receipts.</returns>
    [HttpGet("api/v{version:apiVersion}/rent-payment-receipts")]
    [ProducesResponseType(typeof(List<RentPaymentReceiptDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetReceipts(
        [FromQuery] Guid? leaseContractId = null,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        [FromQuery] Guid? lastSeenId = null,
        [FromQuery] DateOnly? lastSeenIssueDate = null,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRentPaymentReceiptsQuery(
            LeaseContractId: leaseContractId,
            TenantId: tenantId,
            DateFrom: dateFrom,
            DateTo: dateTo,
            LastSeenId: lastSeenId,
            LastSeenIssueDate: lastSeenIssueDate,
            PageSize: pageSize
        );

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Generates the scheduled rent installments for an active lease contract.
    /// </summary>
    /// <param name="leaseId">Lease contract unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost("api/v{version:apiVersion}/leases/{leaseId:guid}/installments/generate")]
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GenerateInstallments(
        [FromRoute] Guid leaseId,
        CancellationToken cancellationToken = default)
    {
        var command = new GenerateScheduledInstallmentsCommand(LeaseContractId: leaseId);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Sends an outstanding rent payment reminder to the associated tenant.
    /// </summary>
    /// <param name="id">Rent payment unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Reminder dispatch confirmation response.</returns>
    [HttpPost("api/v{version:apiVersion}/rent-payments/{id:guid}/remind")]
    [Authorize(Policy = FinancialsPermissions.PaymentsApprove)]
    [ProducesResponseType(typeof(RemindRentPaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RemindTenant(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new RemindRentPaymentCommand(RentPaymentId: id);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
