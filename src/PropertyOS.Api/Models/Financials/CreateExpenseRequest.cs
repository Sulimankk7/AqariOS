using System;
using System.Collections.Generic;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Api.Models.Financials;

/// <summary>
/// Request model for a receipt attached at expense creation time.
/// </summary>
public record CreateExpenseReceiptRequest(
    Guid FileId,
    decimal Amount,
    DateOnly IssuedAt,
    string? Description = null
);

/// <summary>
/// Request model for creating a new operational expense.
/// </summary>
public record CreateExpenseRequest(
    Guid? BuildingId,
    ExpenseCategory Category,
    decimal Amount,
    DateOnly ExpenseDate,
    ExpensePaymentMethod PaymentMethod,
    string Description,
    string? VendorName = null,
    string? InvoiceNumber = null,
    string? Notes = null,
    List<CreateExpenseReceiptRequest>? Receipts = null
);
