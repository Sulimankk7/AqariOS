using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.RecordChequeStatusChange;

public class RecordChequeStatusChangeCommandHandler : IRequestHandler<RecordChequeStatusChangeCommand, Unit>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public RecordChequeStatusChangeCommandHandler(
        IRentPaymentRepository rentPaymentRepository,
        ICurrentUserContext currentUserContext)
    {
        _rentPaymentRepository = rentPaymentRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(RecordChequeStatusChangeCommand request, CancellationToken cancellationToken)
    {
        var cheque = await _rentPaymentRepository.LoadChequeAsync(request.ChequeId, cancellationToken);
        if (cheque == null)
            throw new KeyNotFoundException($"ChequeDetails with ID {request.ChequeId} was not found.");

        switch (request.NewStatus)
        {
            case ChequeStatus.Received:
                cheque.Receive(request.ActionDate, DateTimeOffset.UtcNow, _currentUserContext.UserId);
                break;
            case ChequeStatus.Deposited:
                cheque.Deposit(request.ActionDate, DateTimeOffset.UtcNow, _currentUserContext.UserId);
                break;
            case ChequeStatus.Cleared:
                cheque.Clear(request.ActionDate, DateTimeOffset.UtcNow, _currentUserContext.UserId);
                break;
            case ChequeStatus.Bounced:
                if (string.IsNullOrWhiteSpace(request.BounceReason))
                    throw new InvalidOperationException("Bounce reason is required when status is Bounced.");
                cheque.Bounce(request.ActionDate, request.BounceReason, request.BounceFeeCharged, DateTimeOffset.UtcNow, _currentUserContext.UserId);
                break;
            case ChequeStatus.Cancelled:
                if (string.IsNullOrWhiteSpace(request.CancellationReason))
                    throw new InvalidOperationException("Cancellation reason is required when status is Cancelled.");
                cheque.Cancel(request.CancellationReason, request.ReplacementChequeId, DateTimeOffset.UtcNow, _currentUserContext.UserId);
                break;
            default:
                throw new InvalidOperationException($"Unsupported status transition: {request.NewStatus}");
        }

        // If the cheque bounced or was cancelled, we must reverse all active allocations originating from this cheque's parent payment
        if (request.NewStatus == ChequeStatus.Bounced || request.NewStatus == ChequeStatus.Cancelled)
        {
            var allocations = await _rentPaymentRepository.GetAllocationsByReceivingIdAsync(cheque.RentPaymentId, cancellationToken);
            foreach (var allocation in allocations.Where(a => a.AllocationStatus == AllocationStatus.Active))
            {
                var reason = request.NewStatus == ChequeStatus.Bounced
                    ? $"Cheque Bounced: Cheque {cheque.ChequeNumber} bounced on {request.ActionDate}."
                    : $"Cheque Cancelled: Cheque {cheque.ChequeNumber} cancelled on {request.ActionDate}.";

                allocation.Reverse(reason, DateTimeOffset.UtcNow, _currentUserContext.UserId);

                // Recompute parent obligation aggregate synchronization
                var obligation = await _rentPaymentRepository.GetByIdAsync(allocation.ObligationPaymentId, cancellationToken);
                if (obligation != null)
                {
                    var obligationAllocations = await _rentPaymentRepository.GetAllocationsByObligationIdAsync(obligation.Id, cancellationToken);
                    decimal updatedObligationPaid = obligationAllocations
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
            }
        }

        return Unit.Value;
    }
}
