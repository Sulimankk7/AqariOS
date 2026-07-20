using System;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Financials.Commands.AttachExpenseReceipt;

public record AttachExpenseReceiptCommand(
    Guid ExpenseId,
    Guid FileId,
    decimal Amount,
    DateOnly IssuedAt,
    string? Description = null
) : ICommand<Guid>;
