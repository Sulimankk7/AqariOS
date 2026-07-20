using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;

namespace PropertyOS.Application.Financials.Commands.RemoveExpenseReceipt;

public class RemoveExpenseReceiptCommandHandler : IRequestHandler<RemoveExpenseReceiptCommand, Unit>
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public RemoveExpenseReceiptCommandHandler(
        IExpenseRepository expenseRepository,
        ICurrentUserContext currentUserContext)
    {
        _expenseRepository = expenseRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(RemoveExpenseReceiptCommand request, CancellationToken cancellationToken)
    {
        var expense = await _expenseRepository.GetByIdAsync(request.ExpenseId, cancellationToken);
        if (expense == null)
            throw new KeyNotFoundException($"Expense with ID {request.ExpenseId} was not found.");

        expense.RemoveReceipt(request.ReceiptId, DateTimeOffset.UtcNow, _currentUserContext.UserId);

        return Unit.Value;
    }
}
