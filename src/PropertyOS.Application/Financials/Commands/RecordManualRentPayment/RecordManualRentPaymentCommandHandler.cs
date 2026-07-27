using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Commands.RecordPaymentAllocation;
using PropertyOS.Application.Leasing;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.RecordManualRentPayment;

public class RecordManualRentPaymentCommandHandler : IRequestHandler<RecordManualRentPaymentCommand, Guid>
{
    private readonly ILeaseContractRepository _leaseContractRepository;
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ISender _sender;

    public RecordManualRentPaymentCommandHandler(
        ILeaseContractRepository leaseContractRepository,
        IRentPaymentRepository rentPaymentRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        ISender sender)
    {
        _leaseContractRepository = leaseContractRepository;
        _rentPaymentRepository = rentPaymentRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _sender = sender;
    }

    public async Task<Guid> Handle(RecordManualRentPaymentCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        // Efawateercom money-in is created exclusively by the gateway callback pipeline
        // (ReceiveEfawateercomCallbackCommand); it can never be recorded manually.
        if (request.Method == PaymentMethod.Efawateercom)
            throw new BusinessRuleException(
                "Efawateercom payments cannot be recorded manually; they are created by the gateway callback pipeline.",
                "MANUAL_PAYMENT_METHOD_INVALID");

        var contract = await _leaseContractRepository.GetByIdAsync(request.LeaseContractId, cancellationToken);

        // Cross-tenant access is masked as not-found (same message as the plain not-found case).
        if (contract == null || contract.CompanyId != companyId)
            throw new NotFoundException($"LeaseContract with ID {request.LeaseContractId} was not found.");

        var payment = RentPayment.Create(
            companyId: contract.CompanyId,
            leaseContractId: contract.Id,
            tenantId: contract.TenantId,
            buildingId: contract.BuildingId,
            apartmentId: contract.ApartmentId,
            purpose: PaymentPurpose.UnallocatedReceipt,
            amountDue: request.Amount,
            currency: contract.Currency,
            billingPeriodStart: null,
            billingPeriodEnd: null,
            dueDate: null,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: _currentUserContext.UserId,
            notes: request.Notes
        );

        payment.SetPaymentReceiptDetails(
            method: request.Method,
            reference: request.PaymentReferenceNumber,
            receiptNumber: null,
            updatedAt: DateTimeOffset.UtcNow,
            updatedBy: _currentUserContext.UserId
        );

        await _rentPaymentRepository.AddAsync(payment, cancellationToken);

        if (request.Method == PaymentMethod.Cheque)
        {
            // The validator enforces the cheque block; this is a defensive backstop for
            // non-HTTP dispatch paths that bypass the FluentValidation pipeline.
            var chequeInput = request.Cheque
                ?? throw new BusinessRuleException(
                    "Cheque details are required when the payment method is Cheque.",
                    "MANUAL_PAYMENT_CHEQUE_DETAILS_REQUIRED");

            var cheque = ChequeDetails.Create(
                companyId: contract.CompanyId,
                rentPaymentId: payment.Id,
                leaseContractId: contract.Id,
                tenantId: contract.TenantId,
                chequeNumber: chequeInput.ChequeNumber,
                bankName: chequeInput.BankName,
                bankBranch: chequeInput.BankBranch,
                issueDate: chequeInput.IssueDate,
                dueDate: chequeInput.DueDate,
                amount: request.Amount,
                currency: contract.Currency,
                receivedDate: chequeInput.ReceivedDate,
                createdAt: DateTimeOffset.UtcNow,
                createdBy: _currentUserContext.UserId
            );

            await _rentPaymentRepository.AddChequeAsync(cheque, cancellationToken);
        }

        if (request.Allocations != null && request.Allocations.Count > 0)
        {
            // Nested dispatch joins the already-open transaction: TransactionBehavior detects
            // CurrentTransaction != null and skips Begin, so the receipt, cheque, and
            // allocations commit (or roll back) atomically.
            var allocationCommand = new RecordPaymentAllocationCommand(
                ReceivingPaymentId: payment.Id,
                Allocations: request.Allocations,
                AllocationDate: DateOnly.FromDateTime(DateTime.UtcNow)
            );

            await _sender.Send(allocationCommand, cancellationToken);
        }

        // Persistence is owned by TransactionBehavior; SaveChangesAsync is not called here.
        return payment.Id;
    }
}
