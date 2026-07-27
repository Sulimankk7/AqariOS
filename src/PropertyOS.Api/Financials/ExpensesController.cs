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
using PropertyOS.Application.Financials.Commands.AttachExpenseReceipt;
using PropertyOS.Application.Financials.Commands.CreateExpense;
using PropertyOS.Application.Financials.Commands.DeleteExpense;
using PropertyOS.Application.Financials.Commands.RemoveExpenseReceipt;
using PropertyOS.Application.Financials.Commands.UpdateExpense;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials.Queries.GetExpenseById;
using PropertyOS.Application.Financials.Queries.GetExpenses;
using PropertyOS.Application.Financials.Security;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Api.Financials;

/// <summary>
/// API controller managing operational expenses and their receipts (Module 7).
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Produces("application/json", "application/problem+json")]
public class ExpensesController : ControllerBase
{
    private readonly ISender _mediator;

    /// <summary>
    /// Initializes a new instance of ExpensesController.
    /// </summary>
    /// <param name="mediator">The MediatR sender instance.</param>
    public ExpensesController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Creates a new operational expense, optionally with initial receipts.
    /// </summary>
    /// <param name="request">Expense creation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created expense.</returns>
    [HttpPost("api/v{version:apiVersion}/expenses")]
    [Authorize(Policy = FinancialsPermissions.ExpensesCreate)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateExpenseCommand(
            BuildingId: request.BuildingId,
            Category: request.Category,
            Amount: request.Amount,
            ExpenseDate: request.ExpenseDate,
            PaymentMethod: request.PaymentMethod,
            Description: request.Description,
            VendorName: request.VendorName,
            InvoiceNumber: request.InvoiceNumber,
            Notes: request.Notes,
            Receipts: request.Receipts?.ConvertAll(
                r => new CreateExpenseReceiptDto(r.FileId, r.Amount, r.IssuedAt, r.Description))
        );

        var expenseId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = expenseId }, expenseId);
    }

    /// <summary>
    /// Gets expenses, keyset-paginated (ExpenseDate DESC, Id ASC) with optional filters.
    /// </summary>
    /// <param name="buildingId">Optional building filter.</param>
    /// <param name="category">Optional expense category filter.</param>
    /// <param name="dateFrom">Optional expense-date lower bound (inclusive).</param>
    /// <param name="dateTo">Optional expense-date upper bound (inclusive).</param>
    /// <param name="lastSeenId">Keyset cursor: ID of the last expense on the previous page.</param>
    /// <param name="lastSeenExpenseDate">Keyset cursor: expense date of the last expense on the previous page.</param>
    /// <param name="pageSize">Page size (default 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Page of expenses.</returns>
    [HttpGet("api/v{version:apiVersion}/expenses")]
    [ProducesResponseType(typeof(List<ExpenseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(
        [FromQuery] Guid? buildingId = null,
        [FromQuery] ExpenseCategory? category = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        [FromQuery] Guid? lastSeenId = null,
        [FromQuery] DateOnly? lastSeenExpenseDate = null,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetExpensesQuery(
            BuildingId: buildingId,
            Category: category,
            DateFrom: dateFrom,
            DateTo: dateTo,
            LastSeenId: lastSeenId,
            LastSeenExpenseDate: lastSeenExpenseDate,
            PageSize: pageSize
        );

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets an expense by unique identifier, including attached receipts.
    /// </summary>
    /// <param name="id">Expense unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Expense details.</returns>
    [HttpGet("api/v{version:apiVersion}/expenses/{id:guid}")]
    [ProducesResponseType(typeof(ExpenseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetExpenseByIdQuery(Id: id);
        var result = await _mediator.Send(query, cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Updates an existing operational expense.
    /// </summary>
    /// <param name="id">Expense unique identifier.</param>
    /// <param name="request">Update parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("api/v{version:apiVersion}/expenses/{id:guid}")]
    [Authorize(Policy = FinancialsPermissions.ExpensesCreate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateExpenseCommand(
            Id: id,
            BuildingId: request.BuildingId,
            Category: request.Category,
            Amount: request.Amount,
            ExpenseDate: request.ExpenseDate,
            PaymentMethod: request.PaymentMethod,
            Description: request.Description,
            VendorName: request.VendorName,
            InvoiceNumber: request.InvoiceNumber,
            Notes: request.Notes
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deletes (soft-deletes) an operational expense.
    /// </summary>
    /// <param name="id">Expense unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("api/v{version:apiVersion}/expenses/{id:guid}")]
    [Authorize(Policy = FinancialsPermissions.ExpensesApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteExpenseCommand(Id: id);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Attaches a receipt to an existing expense.
    /// </summary>
    /// <param name="id">Expense unique identifier.</param>
    /// <param name="request">Receipt parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created expense receipt.</returns>
    [HttpPost("api/v{version:apiVersion}/expenses/{id:guid}/receipts")]
    [Authorize(Policy = FinancialsPermissions.ReceiptsIssue)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AttachReceipt(
        [FromRoute] Guid id,
        [FromBody] AttachExpenseReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new AttachExpenseReceiptCommand(
            ExpenseId: id,
            FileId: request.FileId,
            Amount: request.Amount,
            IssuedAt: request.IssuedAt,
            Description: request.Description
        );

        var receiptId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, receiptId);
    }

    /// <summary>
    /// Removes a receipt from an expense.
    /// </summary>
    /// <param name="id">Expense unique identifier.</param>
    /// <param name="receiptId">Expense receipt unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("api/v{version:apiVersion}/expenses/{id:guid}/receipts/{receiptId:guid}")]
    [Authorize(Policy = FinancialsPermissions.ReceiptsIssue)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RemoveReceipt(
        [FromRoute] Guid id,
        [FromRoute] Guid receiptId,
        CancellationToken cancellationToken = default)
    {
        var command = new RemoveExpenseReceiptCommand(
            ExpenseId: id,
            ReceiptId: receiptId
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
