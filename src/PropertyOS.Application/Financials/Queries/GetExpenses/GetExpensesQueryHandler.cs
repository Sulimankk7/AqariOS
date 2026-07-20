using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.GetExpenses;

public class GetExpensesQueryHandler : IRequestHandler<GetExpensesQuery, List<ExpenseDto>>
{
    private readonly IExpenseRepository _expenseRepository;

    public GetExpensesQueryHandler(IExpenseRepository expenseRepository)
    {
        _expenseRepository = expenseRepository;
    }

    public Task<List<ExpenseDto>> Handle(GetExpensesQuery request, CancellationToken cancellationToken)
    {
        var filter = new ExpenseFilterOptions(
            BuildingId: request.BuildingId,
            Category: request.Category,
            DateFrom: request.DateFrom,
            DateTo: request.DateTo,
            LastSeenId: request.LastSeenId,
            LastSeenExpenseDate: request.LastSeenExpenseDate,
            PageSize: request.PageSize
        );

        return _expenseRepository.GetExpensesAsync(filter, cancellationToken);
    }
}
