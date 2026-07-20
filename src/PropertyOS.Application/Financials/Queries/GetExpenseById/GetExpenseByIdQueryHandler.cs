using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.GetExpenseById;

public class GetExpenseByIdQueryHandler : IRequestHandler<GetExpenseByIdQuery, ExpenseDetailDto?>
{
    private readonly IExpenseRepository _expenseRepository;

    public GetExpenseByIdQueryHandler(IExpenseRepository expenseRepository)
    {
        _expenseRepository = expenseRepository;
    }

    public Task<ExpenseDetailDto?> Handle(GetExpenseByIdQuery request, CancellationToken cancellationToken)
    {
        return _expenseRepository.GetDetailByIdAsync(request.Id, cancellationToken);
    }
}
