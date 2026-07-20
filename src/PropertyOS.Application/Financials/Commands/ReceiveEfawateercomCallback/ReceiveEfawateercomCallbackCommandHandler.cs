using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Commands.RecordPaymentAllocation;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.ReceiveEfawateercomCallback;

public class ReceiveEfawateercomCallbackCommandHandler : IRequestHandler<ReceiveEfawateercomCallbackCommand, Unit>
{
    private readonly IEfawateercomTransactionRepository _transactionRepository;
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly ICompanyReceiptSequenceRepository _sequenceRepository;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IMediator _mediator;

    public ReceiveEfawateercomCallbackCommandHandler(
        IEfawateercomTransactionRepository transactionRepository,
        IRentPaymentRepository rentPaymentRepository,
        ICompanyReceiptSequenceRepository sequenceRepository,
        ICurrentUserContext currentUserContext,
        IMediator mediator)
    {
        _transactionRepository = transactionRepository;
        _rentPaymentRepository = rentPaymentRepository;
        _sequenceRepository = sequenceRepository;
        _currentUserContext = currentUserContext;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(ReceiveEfawateercomCallbackCommand request, CancellationToken cancellationToken)
    {
        // ── Step 1: Acquire a row-level exclusive lock on the transaction row. ──────────
        // SELECT ... FOR UPDATE serializes concurrent callbacks for the same external ID.
        // The second caller will block at the DB level until the first transaction commits
        // or rolls back, guaranteeing that the idempotency check below reads committed state.
        var transaction = await _transactionRepository.GetByExternalIdForUpdateAsync(
            request.ExternalTransactionId, cancellationToken);

        if (transaction == null)
            throw new KeyNotFoundException(
                $"eFAWATEERcom transaction with external ID '{request.ExternalTransactionId}' was not found.");

        // ── Step 2: Idempotency guard. ────────────────────────────────────────────────
        // After acquiring the lock, re-check the current DB status.
        // If already in any terminal state, this is a duplicate callback — safe no-op.
        if (transaction.TransactionStatus == EfawateercomStatus.Success   ||
            transaction.TransactionStatus == EfawateercomStatus.Failed    ||
            transaction.TransactionStatus == EfawateercomStatus.Timeout   ||
            transaction.TransactionStatus == EfawateercomStatus.Cancelled)
        {
            return Unit.Value;
        }

        // ── Step 3: Transition the gateway transaction status in the domain model. ──────
        transaction.UpdateStatus(
            status: request.Status,
            responseTime: request.ResponseTime,
            responseCode: request.ResponseCode,
            responseMessage: request.ResponseMessage,
            rawResponse: request.RawResponse,
            now: DateTimeOffset.UtcNow,
            updatedBy: _currentUserContext.UserId
        );

        // ── Step 4: On success, run the full settlement pipeline. ─────────────────────
        if (request.Status == EfawateercomStatus.Success)
        {
            // 4a. Load the scheduled installment (the obligation being settled).
            var installment = await _rentPaymentRepository.GetByIdAsync(transaction.RentPaymentId, cancellationToken);
            if (installment == null)
                throw new KeyNotFoundException(
                    $"Linked RentPayment installment with ID '{transaction.RentPaymentId}' was not found.");

            // 4b. Create the incoming cash receipt (UnallocatedReceipt) representing the funds received.
            // This is a new RentPayment record within the existing Module 6 aggregate model.
            var receivedFundsPayment = RentPayment.Create(
                companyId: installment.CompanyId,
                leaseContractId: installment.LeaseContractId,
                tenantId: installment.TenantId,
                buildingId: installment.BuildingId,
                apartmentId: installment.ApartmentId,
                purpose: PaymentPurpose.UnallocatedReceipt,
                amountDue: transaction.Amount,
                currency: transaction.Currency,
                billingPeriodStart: null,
                billingPeriodEnd: null,
                dueDate: null,
                createdAt: DateTimeOffset.UtcNow,
                createdBy: _currentUserContext.UserId
            );

            receivedFundsPayment.SetPaymentReceiptDetails(
                method: PaymentMethod.Efawateercom,
                reference: transaction.ExternalTransactionId,
                receiptNumber: null,
                updatedAt: DateTimeOffset.UtcNow,
                updatedBy: _currentUserContext.UserId
            );

            await _rentPaymentRepository.AddAsync(receivedFundsPayment, cancellationToken);

            // 4c. Execute the existing Module 6 allocation pipeline to link the received funds
            //     to the scheduled installment and update its DueDateStatus.
            //     TransactionBehavior recognises a nested ICommand and re-uses the existing
            //     open transaction (line 26 of TransactionBehavior: CurrentTransaction != null → skip Begin).
            var allocationCommand = new RecordPaymentAllocationCommand(
                ReceivingPaymentId: receivedFundsPayment.Id,
                Allocations: new List<AllocationDetail>
                {
                    new AllocationDetail(transaction.RentPaymentId, transaction.Amount)
                },
                AllocationDate: DateOnly.FromDateTime(request.ResponseTime.UtcDateTime),
                Notes: $"Auto-allocated via eFAWATEERcom callback '{request.ExternalTransactionId}'"
            );

            await _mediator.Send(allocationCommand, cancellationToken);

            // 4d. Issue the tenant receipt if the installment is now fully paid.
            // After RecordPaymentAllocationCommand runs, the installment entity tracked by the
            // same DbContext has its DueDateStatus and AmountPaid updated in-memory via
            // obligation.UpdateAllocationSync(). We reload the fresh in-memory state.
            if (installment.DueDateStatus == DueDateStatus.Paid)
            {
                // Single atomic round-trip: lock sequence row, evaluate reset, increment, format.
                var receiptNumber = await _sequenceRepository.ReserveAndFormatNextReceiptNumberAsync(
                    installment.CompanyId, cancellationToken);

                // IssueReceipt enforces all immutability invariants:
                // - Must be fully paid (DueDateStatus == Paid, AmountPaid == AmountDue)
                // - No duplicate active receipt (Receipt == null or soft-deleted)
                // The resulting RentPaymentReceipt is tracked by EF and saved by TransactionBehavior.
                installment.IssueReceipt(
                    receiptNumber: receiptNumber,
                    issuedAt: DateTimeOffset.UtcNow,
                    issuedBy: _currentUserContext.UserId
                );
            }
        }

        // ── Step 5: Persistence is owned exclusively by TransactionBehavior. ────────────
        // SaveChangesAsync is NOT called here. All mutations (gateway tx status, received
        // funds payment, allocation, receipt) are committed atomically when TransactionBehavior
        // calls SaveChangesAsync and CommitTransactionAsync on exit.
        return Unit.Value;
    }
}
