using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.CancelEfawateercomTransaction;

public class CancelEfawateercomTransactionCommandHandler : IRequestHandler<CancelEfawateercomTransactionCommand, Unit>
{
    private readonly IEfawateercomTransactionRepository _transactionRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public CancelEfawateercomTransactionCommandHandler(
        IEfawateercomTransactionRepository transactionRepository,
        ICurrentUserContext currentUserContext)
    {
        _transactionRepository = transactionRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(CancelEfawateercomTransactionCommand request, CancellationToken cancellationToken)
    {
        // FOR UPDATE serializes this against concurrent callbacks, expiry, or duplicate cancel calls.
        var transaction = await _transactionRepository.GetByIdForUpdateAsync(request.TransactionId, cancellationToken);
        if (transaction == null)
            throw new NotFoundException($"eFAWATEERcom transaction with ID '{request.TransactionId}' was not found.");

        // Idempotency: already-terminal transactions cannot be further transitioned.
        if (transaction.TransactionStatus == EfawateercomStatus.Success  ||
            transaction.TransactionStatus == EfawateercomStatus.Failed   ||
            transaction.TransactionStatus == EfawateercomStatus.Timeout  ||
            transaction.TransactionStatus == EfawateercomStatus.Cancelled)
        {
            return Unit.Value;
        }

        // Transition to Cancelled. Domain enforces invariants (non-terminal source state).
        try
        {
            transaction.UpdateStatus(
                status: EfawateercomStatus.Cancelled,
                responseTime: DateTimeOffset.UtcNow,
                responseCode: "OPERATOR_CANCEL",
                responseMessage: request.Reason ?? "Transaction cancelled by operator.",
                rawResponse: null,
                now: DateTimeOffset.UtcNow,
                updatedBy: _currentUserContext.UserId
            );
        }
        catch (InvalidOperationException ex)
        {
            // Domain state-machine violation (terminal status cannot transition)
            throw new BusinessRuleException(ex.Message, "EFAWATEERCOM_INVALID_TRANSITION");
        }

        // TransactionBehavior owns SaveChangesAsync and COMMIT.
        return Unit.Value;
    }
}
