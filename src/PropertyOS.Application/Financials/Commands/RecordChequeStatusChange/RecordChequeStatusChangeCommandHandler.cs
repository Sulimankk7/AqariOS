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
            throw new NotFoundException($"ChequeDetails with ID {request.ChequeId} was not found.");

        try
        {
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
                        throw new BusinessRuleException("Bounce reason is required when status is Bounced.", "CHEQUE_BOUNCE_REASON_REQUIRED");
                    cheque.Bounce(request.ActionDate, request.BounceReason, request.BounceFeeCharged, DateTimeOffset.UtcNow, _currentUserContext.UserId);
                    break;
                case ChequeStatus.Cancelled:
                    if (string.IsNullOrWhiteSpace(request.CancellationReason))
                        throw new BusinessRuleException("Cancellation reason is required when status is Cancelled.", "CHEQUE_CANCEL_REASON_REQUIRED");
                    cheque.Cancel(request.CancellationReason, request.ReplacementChequeId, DateTimeOffset.UtcNow, _currentUserContext.UserId);
                    break;
                default:
                    throw new BusinessRuleException($"Unsupported status transition: {request.NewStatus}", "CHEQUE_UNSUPPORTED_TRANSITION");
            }
        }
        catch (InvalidOperationException ex)
        {
            // Domain state-machine violation (e.g. Clear before Deposit)
            throw new BusinessRuleException(ex.Message, "CHEQUE_INVALID_TRANSITION");
        }

        // If the cheque bounced or was cancelled, reverse all active allocations originating
        // from this cheque's parent payment — a single multi-table transaction.
        if (request.NewStatus == ChequeStatus.Bounced || request.NewStatus == ChequeStatus.Cancelled)
        {
            var allocations = await _rentPaymentRepository.GetAllocationsByReceivingIdAsync(cheque.RentPaymentId, cancellationToken);
            var activeAllocations = allocations.Where(a => a.AllocationStatus == AllocationStatus.Active).ToList();

            if (activeAllocations.Count > 0)
            {
                // Shared locking protocol: lock the parent payment and every affected
                // obligation row (sorted FOR UPDATE) before recomputing settlement sums,
                // then RE-READ the allocation set — it is stable once the payment rows are
                // locked because all allocation writers acquire these locks first.
                var lockIds = activeAllocations
                    .Select(a => a.ObligationPaymentId)
                    .Append(cheque.RentPaymentId)
                    .Distinct()
                    .ToList();
                var lockedPayments = await _rentPaymentRepository.GetByIdsForUpdateAsync(lockIds, cancellationToken);
                var paymentsById = lockedPayments.ToDictionary(p => p.Id);

                allocations = await _rentPaymentRepository.GetAllocationsByReceivingIdAsync(cheque.RentPaymentId, cancellationToken);
                activeAllocations = allocations.Where(a => a.AllocationStatus == AllocationStatus.Active).ToList();

                var reason = request.NewStatus == ChequeStatus.Bounced
                    ? $"Cheque Bounced: Cheque {cheque.ChequeNumber} bounced on {request.ActionDate}."
                    : $"Cheque Cancelled: Cheque {cheque.ChequeNumber} cancelled on {request.ActionDate}.";

                foreach (var allocation in activeAllocations)
                {
                    allocation.Reverse(reason, DateTimeOffset.UtcNow, _currentUserContext.UserId);
                }

                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var graceDays = await _rentPaymentRepository.GetRentGracePeriodDaysAsync(cheque.CompanyId, cancellationToken);
                foreach (var obligationId in activeAllocations.Select(a => a.ObligationPaymentId).Distinct())
                {
                    if (!paymentsById.TryGetValue(obligationId, out var obligation))
                        continue;

                    // Cancelled is sticky (doc §6.1): never revived by a bounce recompute.
                    if (obligation.DueDateStatus == DueDateStatus.Cancelled)
                        continue;

                    // In-memory reversals above are visible here: EF identity resolution
                    // returns the same tracked allocation instances.
                    var obligationAllocations = await _rentPaymentRepository.GetAllocationsByObligationIdAsync(obligationId, cancellationToken);
                    decimal updatedObligationPaid = obligationAllocations
                        .Where(a => a.AllocationStatus == AllocationStatus.Active)
                        .Sum(a => a.AllocatedAmount);

                    var status = AllocationSettlement.DeriveStatus(updatedObligationPaid, obligation.AmountDue, obligation.DueDate, today, graceDays);
                    obligation.UpdateAllocationSync(updatedObligationPaid, status, DateTimeOffset.UtcNow, _currentUserContext.UserId);
                }
            }
        }

        return Unit.Value;
    }
}
