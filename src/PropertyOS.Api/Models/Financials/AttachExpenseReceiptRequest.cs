using System;

namespace PropertyOS.Api.Models.Financials;

/// <summary>
/// Request model for attaching a receipt to an existing expense.
/// </summary>
public record AttachExpenseReceiptRequest(
    Guid FileId,
    decimal Amount,
    DateOnly IssuedAt,
    string? Description = null
);
