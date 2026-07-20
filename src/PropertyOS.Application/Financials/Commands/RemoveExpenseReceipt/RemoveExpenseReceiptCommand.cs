using System;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Financials.Commands.RemoveExpenseReceipt;

public record RemoveExpenseReceiptCommand(
    Guid ExpenseId,
    Guid ReceiptId
) : ICommand;
