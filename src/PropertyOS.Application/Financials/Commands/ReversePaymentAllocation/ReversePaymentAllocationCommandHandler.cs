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

namespace PropertyOS.Application.Financials.Commands.ReversePaymentAllocation;

public class ReversePaymentAllocationCommandHandler : IRequestHandler<ReversePaymentAllocationCommand, Unit>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public ReversePaymentAllocationCommandHandler(
        IRentPaymentRepository rentPaymentRepository,
        ICurrentUserContext currentUserContext)
    {
        _rentPaymentRepository = rentPaymentRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(ReversePaymentAllocationCommand request, CancellationToken cancellationToken)
    {
        var allocation = await _rentPaymentRepository.LoadAllocationAsync(request.AllocationId, cancellationToken);
        if (allocation == null)
            throw new NotFoundException($"PaymentAllocation with ID {request.AllocationId} was not found.");

        if (allocation.AllocationStatus != AllocationStatus.Active)
            throw new BusinessRuleException("Cannot reverse a payment allocation that is not active.", "ALLOCATION_NOT_ACTIVE");

        // Lock the obligation row BEFORE mutating so this reversal serializes with any
        // concurrent allocation recording against the same obligation (shared protocol —
        // see RecordPaymentAllocationCommandHandler).
        var lockedPayments = await _rentPaymentRepository.GetByIdsForUpdateAsync(
            new[] { allocation.ObligationPaymentId }, cancellationToken);
        var obligation = lockedPayments.FirstOrDefault();

        // Mutate allocation status and register reversal metadata
        allocation.Reverse(request.ReversalReason, DateTimeOffset.UtcNow, _currentUserContext.UserId);

        // Update parent obligation aggregate synchronization. Cancelled is sticky (doc §6.1):
        // a reversal must never "revive" a deliberately cancelled obligation.
        if (obligation != null && obligation.DueDateStatus != DueDateStatus.Cancelled)
        {
            var existingAllocations = await _rentPaymentRepository.GetAllocationsByObligationIdAsync(obligation.Id, cancellationToken);

            // Recompute target obligation totals excluding the reversed allocation
            decimal updatedObligationPaid = existingAllocations
                .Where(a => a.AllocationStatus == AllocationStatus.Active && a.Id != allocation.Id)
                .Sum(a => a.AllocatedAmount);

            var graceDays = await _rentPaymentRepository.GetRentGracePeriodDaysAsync(obligation.CompanyId, cancellationToken);
            var status = AllocationSettlement.DeriveStatus(
                updatedObligationPaid, obligation.AmountDue, obligation.DueDate, DateOnly.FromDateTime(DateTime.UtcNow), graceDays);

            obligation.UpdateAllocationSync(updatedObligationPaid, status, DateTimeOffset.UtcNow, _currentUserContext.UserId);
        }

        return Unit.Value;
    }
}
