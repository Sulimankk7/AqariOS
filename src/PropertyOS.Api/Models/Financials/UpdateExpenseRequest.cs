using System;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Api.Models.Financials;

/// <summary>
/// Request model for updating an existing operational expense.
/// </summary>
public record UpdateExpenseRequest(
    Guid? BuildingId,
    ExpenseCategory Category,
    decimal Amount,
    DateOnly ExpenseDate,
    ExpensePaymentMethod PaymentMethod,
    string Description,
    string? VendorName = null,
    string? InvoiceNumber = null,
    string? Notes = null
);
