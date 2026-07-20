using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.ExpireEfawateercomTransaction;

public class ExpireEfawateercomTransactionCommandHandler : IRequestHandler<ExpireEfawateercomTransactionCommand, Unit>
{
    private readonly IEfawateercomTransactionRepository _transactionRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public ExpireEfawateercomTransactionCommandHandler(
        IEfawateercomTransactionRepository transactionRepository,
        ICurrentUserContext currentUserContext)
    {
        _transactionRepository = transactionRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(ExpireEfawateercomTransactionCommand request, CancellationToken cancellationToken)
    {
        // FOR UPDATE serializes concurrent expiry/callback/cancel operations on the same row.
        var transaction = await _transactionRepository.GetByIdForUpdateAsync(request.TransactionId, cancellationToken);
        if (transaction == null)
            throw new KeyNotFoundException($"eFAWATEERcom transaction with ID '{request.TransactionId}' was not found.");

        // Idempotency: if the transaction is already in any terminal state, this is a no-op.
        // This can happen when a callback arrives just before the expiry sweep runs.
        if (transaction.TransactionStatus == EfawateercomStatus.Success  ||
            transaction.TransactionStatus == EfawateercomStatus.Failed   ||
            transaction.TransactionStatus == EfawateercomStatus.Timeout  ||
            transaction.TransactionStatus == EfawateercomStatus.Cancelled)
        {
            return Unit.Value;
        }

        // Transition to Timeout. The domain enforces that only non-terminal states can transition.
        transaction.UpdateStatus(
            status: EfawateercomStatus.Timeout,
            responseTime: DateTimeOffset.UtcNow,
            responseCode: request.ResponseCode ?? "TIMEOUT",
            responseMessage: request.ResponseMessage ?? "Transaction expired without a gateway response.",
            rawResponse: null,
            now: DateTimeOffset.UtcNow,
            updatedBy: _currentUserContext.UserId
        );

        // TransactionBehavior owns SaveChangesAsync and COMMIT.
        return Unit.Value;
    }
}
