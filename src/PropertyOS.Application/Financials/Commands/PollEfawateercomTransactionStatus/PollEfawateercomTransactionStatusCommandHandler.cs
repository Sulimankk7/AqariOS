using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Commands.ReceiveEfawateercomCallback;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.PollEfawateercomTransactionStatus;

public class PollEfawateercomTransactionStatusCommandHandler
    : IRequestHandler<PollEfawateercomTransactionStatusCommand, Unit>
{
    private readonly IEfawateercomTransactionRepository _transactionRepository;
    private readonly IEfawateercomGateway _gateway;
    private readonly IMediator _mediator;

    public PollEfawateercomTransactionStatusCommandHandler(
        IEfawateercomTransactionRepository transactionRepository,
        IEfawateercomGateway gateway,
        IMediator mediator)
    {
        _transactionRepository = transactionRepository;
        _gateway = gateway;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(PollEfawateercomTransactionStatusCommand request, CancellationToken cancellationToken)
    {
        // ── Step 1: Check current local state without a lock. ─────────────────────────
        // If already terminal, no outbound call is needed. Read-only, no contention.
        var localTransaction = await _transactionRepository.GetByExternalIdAsync(
            request.ExternalTransactionId, cancellationToken);

        if (localTransaction == null)
            throw new NotFoundException(
                $"eFAWATEERcom transaction with external ID '{request.ExternalTransactionId}' was not found.");

        // If already in a terminal state, polling is a no-op.
        if (localTransaction.TransactionStatus == EfawateercomStatus.Success  ||
            localTransaction.TransactionStatus == EfawateercomStatus.Failed   ||
            localTransaction.TransactionStatus == EfawateercomStatus.Timeout  ||
            localTransaction.TransactionStatus == EfawateercomStatus.Cancelled)
        {
            return Unit.Value;
        }

        // ── Step 2: Query the external gateway. ────────────────────────────────────────
        // IEfawateercomGateway is a pure I/O adapter — no business logic.
        // NullEfawateercomGateway returns null, which is handled below.
        var gatewayResult = await _gateway.GetTransactionStatusAsync(
            request.ExternalTransactionId, cancellationToken);

        if (gatewayResult == null)
        {
            // Gateway does not recognise this transaction ID. No state change.
            // This can happen during development (NullGateway) or if the external
            // provider has not yet processed the transaction.
            return Unit.Value;
        }

        // ── Step 3: Re-use the callback pipeline. ──────────────────────────────────────
        // ReceiveEfawateercomCallbackCommandHandler owns:
        //   - FOR UPDATE idempotency lock
        //   - Status transition (domain)
        //   - UnallocatedReceipt creation (on Success)
        //   - RecordPaymentAllocation dispatch (on Success)
        //   - IssueReceipt (on Success + fully paid)
        //
        // Dispatching it here means the poll path and the webhook path are exactly
        // identical in business behaviour — no duplication, no divergence risk.
        //
        // TransactionBehavior sees an ICommand nested inside an ICommand and skips
        // opening a second transaction (CurrentTransaction != null guard).
        var callbackCommand = new ReceiveEfawateercomCallbackCommand(
            ExternalTransactionId: request.ExternalTransactionId,
            Status: gatewayResult.Status,
            ResponseTime: gatewayResult.ResponseTime,
            ResponseCode: gatewayResult.ResponseCode,
            ResponseMessage: gatewayResult.ResponseMessage,
            RawResponse: gatewayResult.RawResponse
        );

        await _mediator.Send(callbackCommand, cancellationToken);

        return Unit.Value;
    }
}
