using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
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
        var receivingPayment = await _rentPaymentRepository.GetByIdAsync(request.ReceivingPaymentId, cancellationToken);
        if (receivingPayment == null)
            throw new KeyNotFoundException($"Receiving RentPayment with ID {request.ReceivingPaymentId} was not found.");

        if (receivingPayment.PaymentPurpose == PaymentPurpose.ScheduledInstallment)
            throw new InvalidOperationException("Cannot allocate funds out of a scheduled installment.");

        var existingReceivingAllocations = await _rentPaymentRepository.GetAllocationsByReceivingIdAsync(receivingPayment.Id, cancellationToken);
        var totalAllocated = existingReceivingAllocations
            .Where(a => a.AllocationStatus == AllocationStatus.Active)
            .Sum(a => a.AllocatedAmount);

        decimal remainingFunds = receivingPayment.AmountDue - totalAllocated;
        decimal newAllocationsSum = request.Allocations.Sum(a => a.Amount);

        if (newAllocationsSum > remainingFunds)
            throw new InvalidOperationException($"Total requested allocations ({newAllocationsSum} JOD) exceeds available receiving payment funds ({remainingFunds} JOD).");

        foreach (var allocationDetail in request.Allocations)
        {
            if (receivingPayment.Id == allocationDetail.ObligationPaymentId)
                throw new InvalidOperationException("Self-allocation is forbidden.");

            var obligation = await _rentPaymentRepository.GetByIdAsync(allocationDetail.ObligationPaymentId, cancellationToken);
            if (obligation == null)
                throw new KeyNotFoundException($"Obligation RentPayment with ID {allocationDetail.ObligationPaymentId} was not found.");

            if (obligation.PaymentPurpose != PaymentPurpose.ScheduledInstallment)
                throw new InvalidOperationException("Funds can only be allocated to scheduled installments.");

            if (obligation.DueDateStatus == DueDateStatus.Cancelled)
                throw new InvalidOperationException("Cannot allocate funds to a cancelled installment.");

            var existingObligationAllocations = await _rentPaymentRepository.GetAllocationsByObligationIdAsync(obligation.Id, cancellationToken);
            var currentAllocated = existingObligationAllocations
                .Where(a => a.AllocationStatus == AllocationStatus.Active)
                .Sum(a => a.AllocatedAmount);

            decimal outstanding = obligation.AmountDue - currentAllocated;
            if (allocationDetail.Amount > outstanding)
                throw new InvalidOperationException($"Allocation amount {allocationDetail.Amount} JOD exceeds outstanding obligation balance of {outstanding} JOD.");

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

            // Synchronize the target obligation aggregate's in-memory cache
            decimal updatedObligationPaid = currentAllocated + allocationDetail.Amount;
            var status = DueDateStatus.Pending;

            if (updatedObligationPaid >= obligation.AmountDue)
            {
                status = DueDateStatus.Paid;
            }
            else if (updatedObligationPaid > 0)
            {
                status = DueDateStatus.PartiallyPaid;
            }
            else
            {
                if (obligation.DueDate.HasValue && DateOnly.FromDateTime(DateTime.UtcNow) > obligation.DueDate.Value)
                {
                    status = DueDateStatus.Late;
                }
            }

            obligation.UpdateAllocationSync(updatedObligationPaid, status, DateTimeOffset.UtcNow, _currentUserContext.UserId);
        }

        return Unit.Value;
    }
}
