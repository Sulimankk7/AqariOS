using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.MarkEfawateercomTransactionSent;

public class MarkEfawateercomTransactionSentCommandHandler : IRequestHandler<MarkEfawateercomTransactionSentCommand, Unit>
{
    private readonly IEfawateercomTransactionRepository _transactionRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public MarkEfawateercomTransactionSentCommandHandler(
        IEfawateercomTransactionRepository transactionRepository,
        ICurrentUserContext currentUserContext)
    {
        _transactionRepository = transactionRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(MarkEfawateercomTransactionSentCommand request, CancellationToken cancellationToken)
    {
        // FOR UPDATE ensures no concurrent process races to cancel or expire the same row.
        var transaction = await _transactionRepository.GetByIdForUpdateAsync(request.TransactionId, cancellationToken);
        if (transaction == null)
            throw new NotFoundException($"eFAWATEERcom transaction with ID '{request.TransactionId}' was not found.");

        // Domain transition: Pending → Sent.
        // UpdateStatus enforces that terminal states cannot be re-entered.
        // If this is called twice (duplicate dispatch), Sent → Sent is a no-op per UpdateStatus idempotency.
        try
        {
            transaction.MarkSent(DateTimeOffset.UtcNow, _currentUserContext.UserId);
        }
        catch (InvalidOperationException ex)
        {
            // Domain state-machine violation (e.g. marking a terminal transaction as Sent)
            throw new BusinessRuleException(ex.Message, "EFAWATEERCOM_INVALID_TRANSITION");
        }

        // TransactionBehavior owns SaveChangesAsync and COMMIT.
        return Unit.Value;
    }
}
