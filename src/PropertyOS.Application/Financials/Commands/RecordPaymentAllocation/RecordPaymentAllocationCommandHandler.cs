using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.RecordPaymentAllocation;

public class RecordPaymentAllocationCommandHandler : IRequestHandler<RecordPaymentAllocationCommand, Unit>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public RecordPaymentAllocationCommandHandler(
        IRentPaymentRepository rentPaymentRepository,
        ICurrentUserContext currentUserContext)
    {
        _rentPaymentRepository = rentPaymentRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(RecordPaymentAllocationCommand request, CancellationToken cancellationToken)
    {
        // Concurrency protocol: lock every involved rent_payments row (receiving payment +
        // all obligations) in a single deterministic FOR UPDATE query BEFORE reading any
        // allocation sums. All allocation writers (record / reverse / cheque bounce cascade)
        // follow this protocol, which serializes concurrent settlement of the same payments
        // and makes the read-modify-write below safe. The database trigger
        // trg_payment_allocations_enforce_limits is the backstop for writers that bypass it.
        var involvedIds = request.Allocations
            .Select(a => a.ObligationPaymentId)
            .Append(request.ReceivingPaymentId)
            .Distinct()
            .ToList();

        var lockedPayments = await _rentPaymentRepository.GetByIdsForUpdateAsync(involvedIds, cancellationToken);
        var paymentsById = lockedPayments.ToDictionary(p => p.Id);

        if (!paymentsById.TryGetValue(request.ReceivingPaymentId, out var receivingPayment))
            throw new NotFoundException($"Receiving RentPayment with ID {request.ReceivingPaymentId} was not found.");

        if (receivingPayment.PaymentPurpose == PaymentPurpose.ScheduledInstallment)
            throw new BusinessRuleException("Cannot allocate funds out of a scheduled installment.", "ALLOCATION_SOURCE_IS_INSTALLMENT");

        var existingReceivingAllocations = await _rentPaymentRepository.GetAllocationsByReceivingIdAsync(receivingPayment.Id, cancellationToken);
        var totalAllocated = existingReceivingAllocations
            .Where(a => a.AllocationStatus == AllocationStatus.Active)
            .Sum(a => a.AllocatedAmount);

        decimal remainingFunds = receivingPayment.AmountDue - totalAllocated;
        decimal newAllocationsSum = request.Allocations.Sum(a => a.Amount);

        if (newAllocationsSum > remainingFunds)
            throw new BusinessRuleException(
                $"Total requested allocations ({newAllocationsSum} {receivingPayment.Currency}) exceeds available receiving payment funds ({remainingFunds} {receivingPayment.Currency}).",
                "ALLOCATION_EXCEEDS_SOURCE_FUNDS");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Grace-period-aware status derivation (doc §6.1): late/overdue determination is
        // offset by the owning company's configured rent grace period.
        var graceDays = await _rentPaymentRepository.GetRentGracePeriodDaysAsync(receivingPayment.CompanyId, cancellationToken);

        // Running totals per obligation: a batch may legitimately contain several entries
        // for the same obligation; re-querying the DB inside the loop would not see the
        // not-yet-saved allocations added earlier in this batch.
        var runningAllocatedByObligation = new Dictionary<Guid, decimal>();

        foreach (var allocationDetail in request.Allocations)
        {
            if (receivingPayment.Id == allocationDetail.ObligationPaymentId)
                throw new BusinessRuleException("Self-allocation is forbidden.", "ALLOCATION_SELF_REFERENCE");

            if (!paymentsById.TryGetValue(allocationDetail.ObligationPaymentId, out var obligation))
                throw new NotFoundException($"Obligation RentPayment with ID {allocationDetail.ObligationPaymentId} was not found.");

            if (obligation.PaymentPurpose != PaymentPurpose.ScheduledInstallment)
                throw new BusinessRuleException("Funds can only be allocated to scheduled installments.", "ALLOCATION_TARGET_NOT_INSTALLMENT");

            if (obligation.DueDateStatus == DueDateStatus.Cancelled)
                throw new BusinessRuleException("Cannot allocate funds to a cancelled installment.", "ALLOCATION_TARGET_CANCELLED");

            if (!string.Equals(obligation.Currency, receivingPayment.Currency, StringComparison.OrdinalIgnoreCase))
                throw new BusinessRuleException(
                    $"Currency mismatch: receiving payment is {receivingPayment.Currency} but the obligation is {obligation.Currency}.",
                    "ALLOCATION_CURRENCY_MISMATCH");

            if (!runningAllocatedByObligation.TryGetValue(obligation.Id, out var currentAllocated))
            {
                var existingObligationAllocations = await _rentPaymentRepository.GetAllocationsByObligationIdAsync(obligation.Id, cancellationToken);
                currentAllocated = existingObligationAllocations
                    .Where(a => a.AllocationStatus == AllocationStatus.Active)
                    .Sum(a => a.AllocatedAmount);
            }

            decimal outstanding = obligation.AmountDue - currentAllocated;
            if (allocationDetail.Amount > outstanding)
                throw new BusinessRuleException(
                    $"Allocation amount {allocationDetail.Amount} {obligation.Currency} exceeds outstanding obligation balance of {outstanding} {obligation.Currency}.",
                    "ALLOCATION_EXCEEDS_OBLIGATION_BALANCE");

            var allocation = PaymentAllocation.Create(
                companyId: receivingPayment.CompanyId,
                receivingPaymentId: receivingPayment.Id,
                obligationPaymentId: obligation.Id,
                allocatedAmount: allocationDetail.Amount,
                allocationDate: request.AllocationDate,
                createdAt: DateTimeOffset.UtcNow,
                createdBy: _currentUserContext.UserId,
                notes: request.Notes
            );

            await _rentPaymentRepository.AddAllocationAsync(allocation, cancellationToken);

            // Synchronize the target obligation aggregate's cached settlement state
            decimal updatedObligationPaid = currentAllocated + allocationDetail.Amount;
            runningAllocatedByObligation[obligation.Id] = updatedObligationPaid;

            var status = AllocationSettlement.DeriveStatus(updatedObligationPaid, obligation.AmountDue, obligation.DueDate, today, graceDays);
            obligation.UpdateAllocationSync(updatedObligationPaid, status, DateTimeOffset.UtcNow, _currentUserContext.UserId);
        }

        return Unit.Value;
    }
}
