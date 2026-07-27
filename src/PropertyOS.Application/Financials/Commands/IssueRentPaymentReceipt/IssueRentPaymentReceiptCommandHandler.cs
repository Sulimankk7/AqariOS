using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Domain.Financials;

namespace PropertyOS.Application.Financials.Commands.IssueRentPaymentReceipt;

public class IssueRentPaymentReceiptCommandHandler : IRequestHandler<IssueRentPaymentReceiptCommand, string>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly ICompanyReceiptSequenceRepository _sequenceRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public IssueRentPaymentReceiptCommandHandler(
        IRentPaymentRepository rentPaymentRepository,
        ICompanyReceiptSequenceRepository sequenceRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _rentPaymentRepository = rentPaymentRepository;
        _sequenceRepository = sequenceRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<string> Handle(IssueRentPaymentReceiptCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        // Lock the payment row (SELECT ... FOR UPDATE): concurrent issue attempts for the
        // same payment serialize at the DB level, so the duplicate-receipt invariant in
        // RentPayment.IssueReceipt always evaluates committed state.
        var lockedPayments = await _rentPaymentRepository.GetByIdsForUpdateAsync(
            new[] { request.RentPaymentId }, cancellationToken);
        var payment = lockedPayments.FirstOrDefault(p => p.Id == request.RentPaymentId);

        // Cross-tenant access is masked as not-found (same message as the plain not-found case).
        if (payment == null || payment.CompanyId != companyId)
            throw new NotFoundException($"RentPayment with ID {request.RentPaymentId} was not found.");

        // Single atomic round-trip: lock sequence row, evaluate reset policy, increment, format.
        var receiptNumber = await _sequenceRepository.ReserveAndFormatNextReceiptNumberAsync(
            payment.CompanyId, cancellationToken);

        RentPaymentReceipt receipt;
        try
        {
            receipt = payment.IssueReceipt(
                receiptNumber: receiptNumber,
                issuedAt: DateTimeOffset.UtcNow,
                issuedBy: _currentUserContext.UserId
            );
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleException(ex.Message, "RECEIPT_ISSUE_INVALID_STATE");
        }

        // Explicit Add is mandatory: the receipt carries a client-generated ID, so
        // navigation-only discovery would track it as Modified (assumed existing).
        await _rentPaymentRepository.AddReceiptAsync(receipt, cancellationToken);

        // Persistence is owned by TransactionBehavior; SaveChangesAsync is not called here.
        return receiptNumber;
    }
}
