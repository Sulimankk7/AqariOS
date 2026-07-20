using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.CreateExpense;

public record CreateExpenseCommand(
    Guid? BuildingId,
    ExpenseCategory Category,
    decimal Amount,
    DateOnly ExpenseDate,
    ExpensePaymentMethod PaymentMethod,
    string Description,
    string? VendorName = null,
    string? InvoiceNumber = null,
    string? Notes = null,
    List<CreateExpenseReceiptDto>? Receipts = null
) : ICommand<Guid>;

public record CreateExpenseReceiptDto(
    Guid FileId,
    decimal Amount,
    DateOnly IssuedAt,
    string? Description
);
