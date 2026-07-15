using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
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
            throw new KeyNotFoundException($"PaymentAllocation with ID {request.AllocationId} was not found.");

        if (allocation.AllocationStatus != AllocationStatus.Active)
            throw new InvalidOperationException("Cannot reverse a payment allocation that is not active.");

        // Mutate allocation status and register reversal metadata
        allocation.Reverse(request.ReversalReason, DateTimeOffset.UtcNow, _currentUserContext.UserId);

        // Update parent obligation aggregate synchronization
        var obligation = await _rentPaymentRepository.GetByIdAsync(allocation.ObligationPaymentId, cancellationToken);
        if (obligation != null)
        {
            var existingAllocations = await _rentPaymentRepository.GetAllocationsByObligationIdAsync(obligation.Id, cancellationToken);
            
            // Recompute target obligation totals excluding the reversed allocation
            decimal updatedObligationPaid = existingAllocations
                .Where(a => a.AllocationStatus == AllocationStatus.Active && a.Id != allocation.Id)
                .Sum(a => a.AllocatedAmount);

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
