using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Financials.Queries.Common;

namespace PropertyOS.Application.Financials.Queries.GetEfawateercomTransactions;

public class GetEfawateercomTransactionsQueryHandler
    : IRequestHandler<GetEfawateercomTransactionsQuery, List<EfawateercomTransactionDto>>
{
    private readonly IEfawateercomTransactionRepository _transactionRepository;

    public GetEfawateercomTransactionsQueryHandler(IEfawateercomTransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public Task<List<EfawateercomTransactionDto>> Handle(
        GetEfawateercomTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new EfawateercomTransactionFilterOptions(
            Status: request.Status,
            PaymentReference: request.PaymentReference,
            RentPaymentId: request.RentPaymentId,
            LastSeenId: request.LastSeenId,
            LastSeenRequestTime: request.LastSeenRequestTime,
            PageSize: request.PageSize
        );

        return _transactionRepository.GetTransactionsAsync(filter, cancellationToken);
    }
}
