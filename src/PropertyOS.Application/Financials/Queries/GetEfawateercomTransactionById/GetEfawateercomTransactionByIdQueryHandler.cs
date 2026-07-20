using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.GetEfawateercomTransactionById;

public class GetEfawateercomTransactionByIdQueryHandler
    : IRequestHandler<GetEfawateercomTransactionByIdQuery, EfawateercomTransactionDetailDto?>
{
    private readonly IEfawateercomTransactionRepository _transactionRepository;

    public GetEfawateercomTransactionByIdQueryHandler(IEfawateercomTransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public Task<EfawateercomTransactionDetailDto?> Handle(
        GetEfawateercomTransactionByIdQuery request,
        CancellationToken cancellationToken)
    {
        return _transactionRepository.GetDetailByIdAsync(request.Id, cancellationToken);
    }
}
