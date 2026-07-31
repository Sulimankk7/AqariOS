using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.CancelRentPayment;

public class CancelRentPaymentCommandHandler : IRequestHandler<CancelRentPaymentCommand, Unit>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CancelRentPaymentCommandHandler(
        IRentPaymentRepository rentPaymentRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _rentPaymentRepository = rentPaymentRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(CancelRentPaymentCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        // Lock the payment row (SELECT ... FOR UPDATE) before reading allocation state:
        // this follows the allocation-writer locking protocol, so a concurrent allocation
        // writer cannot attach a new active allocation between the guard and the cancel.
        var lockedPayments = await _rentPaymentRepository.GetByIdsForUpdateAsync(
            new[] { request.RentPaymentId }, cancellationToken);
        var payment = lockedPayments.FirstOrDefault(p => p.Id == request.RentPaymentId);

        // Cross-tenant access is masked as not-found (same message as the plain not-found case).
        if (payment == null || payment.CompanyId != companyId)
            throw new NotFoundException($"RentPayment with ID {request.RentPaymentId} was not found.");

        // 1. Validate terminal cancellation state
        if (payment.DueDateStatus == DueDateStatus.Cancelled)
            throw new BusinessRuleException(
                "Payment is already cancelled.",
                "PAYMENT_ALREADY_CANCELLED");

        // 2. Validate active issued receipt
        var hasActiveReceipt = !string.IsNullOrWhiteSpace(payment.ReceiptNumber)
            || (payment.Receipt != null && payment.Receipt.DeletedAt == null);

        if (!hasActiveReceipt)
        {
            var receiptDto = await _rentPaymentRepository.GetReceiptByRentPaymentIdAsync(payment.Id, companyId, cancellationToken);
            hasActiveReceipt = receiptDto != null;
        }

        if (hasActiveReceipt)
            throw new BusinessRuleException(
                "Cannot cancel a payment with an active issued receipt.",
                "PAYMENT_CANCEL_HAS_RECEIPT");

        // 3. A payment referenced by any ACTIVE allocation — in either direction — cannot be
        // cancelled: the allocations must be reversed first to keep settlement math intact.
        var obligationAllocations = await _rentPaymentRepository.GetAllocationsByObligationIdAsync(
            payment.Id, cancellationToken);
        var receivingAllocations = await _rentPaymentRepository.GetAllocationsByReceivingIdAsync(
            payment.Id, cancellationToken);

        var hasActiveAllocations =
            obligationAllocations.Any(a => a.AllocationStatus == AllocationStatus.Active) ||
            receivingAllocations.Any(a => a.AllocationStatus == AllocationStatus.Active);

        if (hasActiveAllocations)
            throw new BusinessRuleException(
                "Cannot cancel a payment with active allocations; reverse them first.",
                "PAYMENT_CANCEL_HAS_ACTIVE_ALLOCATIONS");

        try
        {
            payment.Cancel(DateTimeOffset.UtcNow, _currentUserContext.UserId);
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleException(ex.Message, "PAYMENT_CANCEL_INVALID_STATE");
        }

        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            var cancellationNote = $"Cancelled: {request.Reason}";
            var combinedNotes = string.IsNullOrWhiteSpace(payment.Notes)
                ? cancellationNote
                : $"{payment.Notes}\n{cancellationNote}";

            payment.SetNotes(combinedNotes, DateTimeOffset.UtcNow, _currentUserContext.UserId);
        }

        // Persistence is owned by TransactionBehavior; SaveChangesAsync is not called here.
        return Unit.Value;
    }
}
