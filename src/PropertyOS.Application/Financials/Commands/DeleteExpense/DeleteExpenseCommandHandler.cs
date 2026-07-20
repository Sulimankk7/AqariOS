using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;

namespace PropertyOS.Application.Financials.Commands.DeleteExpense;

public class DeleteExpenseCommandHandler : IRequestHandler<DeleteExpenseCommand, Unit>
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public DeleteExpenseCommandHandler(
        IExpenseRepository expenseRepository,
        ICurrentUserContext currentUserContext)
    {
        _expenseRepository = expenseRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await _expenseRepository.GetByIdAsync(request.Id, cancellationToken);
        if (expense == null)
            throw new KeyNotFoundException($"Expense with ID {request.Id} was not found.");

        expense.SoftDelete(DateTimeOffset.UtcNow, _currentUserContext.UserId);

        return Unit.Value;
    }
}
