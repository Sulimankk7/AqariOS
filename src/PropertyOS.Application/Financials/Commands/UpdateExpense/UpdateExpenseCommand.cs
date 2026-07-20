using System;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.UpdateExpense;

public record UpdateExpenseCommand(
    Guid Id,
    Guid? BuildingId,
    ExpenseCategory Category,
    decimal Amount,
    DateOnly ExpenseDate,
    ExpensePaymentMethod PaymentMethod,
    string Description,
    string? VendorName = null,
    string? InvoiceNumber = null,
    string? Notes = null
) : ICommand;
