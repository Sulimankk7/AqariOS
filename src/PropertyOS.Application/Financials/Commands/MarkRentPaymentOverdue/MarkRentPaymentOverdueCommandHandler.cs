using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.MarkRentPaymentOverdue;

public class MarkRentPaymentOverdueCommandHandler : IRequestHandler<MarkRentPaymentOverdueCommand, Unit>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly IBusinessClock _businessClock;
    private readonly ICurrentUserContext _currentUserContext;

    public MarkRentPaymentOverdueCommandHandler(
        IRentPaymentRepository rentPaymentRepository,
        IBusinessClock businessClock,
        ICurrentUserContext currentUserContext)
    {
        _rentPaymentRepository = rentPaymentRepository;
        _businessClock = businessClock;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(MarkRentPaymentOverdueCommand request, CancellationToken cancellationToken)
    {
        // Concurrency protocol: lock the payment row (SELECT ... FOR UPDATE) before reading
        // its settlement state, exactly like every allocation writer. This serializes the
        // overdue sweep against concurrent record/reverse/cheque-bounce writers so we never
        // stomp an in-flight settlement update with a stale status.
        var lockedPayments = await _rentPaymentRepository.GetByIdsForUpdateAsync(
            new[] { request.RentPaymentId }, cancellationToken);

        if (lockedPayments.Count == 0)
            throw new NotFoundException($"RentPayment with ID {request.RentPaymentId} was not found.");

        var payment = lockedPayments[0];

        // Scope guard + idempotency (doc §6.1): only Pending and PartiallyPaid scheduled
        // installments can transition (Pending -> OverdueUnpaid, PartiallyPaid -> Late once
        // past the grace-adjusted due date). Paid/Late/OverdueUnpaid are terminal for the
        // sweep, Cancelled is sticky, non-installment purposes are out of scope — all
        // silent no-ops so the sweep can safely re-dispatch candidates.
        if (payment.PaymentPurpose != PaymentPurpose.ScheduledInstallment ||
            (payment.DueDateStatus != DueDateStatus.Pending &&
             payment.DueDateStatus != DueDateStatus.PartiallyPaid))
        {
            return Unit.Value;
        }

        var effectiveAsOf = request.AsOf ?? _businessClock.UtcNow;
        var jordanBusinessDate = _businessClock.GetJordanBusinessDate(effectiveAsOf);

        // Grace-period-aware derivation: the company's configured grace period offsets
        // the late/overdue boundary (company_settings.rent_grace_period_days).
        var graceDays = await _rentPaymentRepository.GetRentGracePeriodDaysAsync(payment.CompanyId, cancellationToken);

        var derivedStatus = AllocationSettlement.DeriveStatus(
            payment.AmountPaid, payment.AmountDue, payment.DueDate, jordanBusinessDate, graceDays);

        if (derivedStatus != payment.DueDateStatus)
        {
            payment.UpdateAllocationSync(
                payment.AmountPaid, derivedStatus, _businessClock.UtcNow, _currentUserContext.UserId);
        }

        // TransactionBehavior owns SaveChangesAsync and COMMIT.
        return Unit.Value;
    }
}
