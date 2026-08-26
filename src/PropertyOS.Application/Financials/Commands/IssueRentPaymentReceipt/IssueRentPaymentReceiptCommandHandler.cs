using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Files;
using PropertyOS.Application.Files.Services;
using PropertyOS.Application.Financials;
using PropertyOS.Domain.Files.Entities;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.IssueRentPaymentReceipt;

public class IssueRentPaymentReceiptCommandHandler : IRequestHandler<IssueRentPaymentReceiptCommand, string>
{
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly ICompanyReceiptSequenceRepository _sequenceRepository;
    private readonly IReceiptPdfGenerator _pdfGenerator;
    private readonly IFileStorageRepository _fileStorageRepository;
    private readonly IStorageProvider _storageProvider;
    private readonly IApplicationDbContext _context;
    private readonly IBusinessClock _clock;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public IssueRentPaymentReceiptCommandHandler(
        IRentPaymentRepository rentPaymentRepository,
        ICompanyReceiptSequenceRepository sequenceRepository,
        IReceiptPdfGenerator pdfGenerator,
        IFileStorageRepository fileStorageRepository,
        IStorageProvider storageProvider,
        IApplicationDbContext context,
        IBusinessClock clock,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _rentPaymentRepository = rentPaymentRepository;
        _sequenceRepository = sequenceRepository;
        _pdfGenerator = pdfGenerator;
        _fileStorageRepository = fileStorageRepository;
        _storageProvider = storageProvider;
        _context = context;
        _clock = clock;
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

        // Check domain preconditions upfront before reserving a receipt sequence number or generating artifacts
        if (payment.Receipt != null && payment.Receipt.DeletedAt == null)
            throw new BusinessRuleException("A receipt has already been issued for this payment.", "RECEIPT_ISSUE_INVALID_STATE");

        if (payment.DueDateStatus == DueDateStatus.Cancelled)
            throw new BusinessRuleException("Cannot issue a receipt for a cancelled payment.", "RECEIPT_ISSUE_INVALID_STATE");

        if (payment.PaymentPurpose == PaymentPurpose.ScheduledInstallment)
        {
            if (payment.DueDateStatus != DueDateStatus.Paid || payment.AmountPaid != payment.AmountDue)
                throw new BusinessRuleException("Cannot issue a receipt for an installment that is not fully paid.", "RECEIPT_ISSUE_INVALID_STATE");
        }
        else if (payment.PaymentPurpose == PaymentPurpose.UnallocatedReceipt)
        {
            if (payment.AmountDue <= 0)
                throw new BusinessRuleException("Cannot issue a receipt for a received payment with non-positive AmountDue.", "RECEIPT_ISSUE_INVALID_STATE");
        }
        else
        {
            throw new BusinessRuleException("Cannot issue a receipt for an adjustment record.", "RECEIPT_ISSUE_INVALID_STATE");
        }

        // Single atomic round-trip: lock sequence row, evaluate reset policy, increment, format.
        var receiptNumber = await _sequenceRepository.ReserveAndFormatNextReceiptNumberAsync(
            payment.CompanyId, cancellationToken);

        // Fetch display entities for PDF rendering
        var tenant = await _context.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == payment.TenantId, cancellationToken);
        var building = await _context.Buildings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == payment.BuildingId, cancellationToken);
        var apartment = await _context.Apartments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == payment.ApartmentId, cancellationToken);
        var contract = await _context.LeaseContracts.AsNoTracking().FirstOrDefaultAsync(lc => lc.Id == payment.LeaseContractId, cancellationToken);

        var submission = payment.Submissions?.OrderByDescending(s => s.CreatedAt).FirstOrDefault();

        var methodStr = submission?.PaymentMethod.ToString() ?? payment.PaymentMethod?.ToString() ?? "Cash";
        var refNumber = submission?.ReferenceNumber ?? payment.PaymentReferenceNumber;
        var now = _clock.UtcNow;

        var pdfModel = new ReceiptPdfModel(
            ReceiptNumber: receiptNumber,
            IssueDate: now,
            AmountPaid: payment.AmountPaid > 0 ? payment.AmountPaid : payment.AmountDue,
            Currency: string.IsNullOrWhiteSpace(payment.Currency) ? "JOD" : payment.Currency,
            PaymentMethod: methodStr,
            ReferenceNumber: refNumber,
            PaymentPurpose: payment.PaymentPurpose.ToString(),
            BillingPeriod: payment.BillingPeriodStart.HasValue && payment.BillingPeriodEnd.HasValue
                ? $"{payment.BillingPeriodStart.Value:dd/MM/yyyy} - {payment.BillingPeriodEnd.Value:dd/MM/yyyy}"
                : null,
            DueDate: payment.DueDate?.ToString("dd/MM/yyyy"),
            DueDateStatus: payment.DueDateStatus.ToString(),
            TenantName: tenant?.Name ?? "Tenant",
            TenantPhone: tenant?.Phone,
            PropertyName: building?.Name ?? "Property",
            UnitNumber: apartment?.UnitNumber ?? "Unit",
            ContractNumber: contract?.ContractNumber ?? "Contract",
            ChequeNumber: submission?.ChequeNumber,
            BankName: submission?.BankName,
            ChequeIssueDate: submission?.ChequeIssueDate?.ToString("dd/MM/yyyy"),
            ChequeDueDate: submission?.ChequeDueDate?.ToString("dd/MM/yyyy")
        );

        // Generate PDF
        var pdfBytes = await _pdfGenerator.GenerateReceiptPdfAsync(payment, pdfModel, cancellationToken);

        // Save PDF to physical storage
        var fileId = Guid.CreateVersion7();
        var storageKey = $"receipts/{payment.CompanyId}/{fileId}.pdf";

        using var stream = new MemoryStream(pdfBytes);
        await _storageProvider.SaveAsync(storageKey, stream, "application/pdf", cancellationToken);

        // Create FileStorage record (Metadata)
        var fileStorage = FileStorage.Create(
            companyId: payment.CompanyId,
            uploadedBy: _currentUserContext.UserId,
            originalFilename: $"Receipt_{receiptNumber}.pdf",
            mimeType: "application/pdf",
            sizeBytes: pdfBytes.Length,
            storageKey: storageKey,
            now: now,
            createdBy: _currentUserContext.UserId,
            id: fileId
        );
        await _fileStorageRepository.AddAsync(fileStorage, cancellationToken);

        // Issue Receipt and link to FileStorage
        RentPaymentReceipt receipt;
        try
        {
            receipt = payment.IssueReceipt(
                receiptNumber: receiptNumber,
                issuedAt: now,
                issuedBy: _currentUserContext.UserId,
                notes: "Manually issued receipt",
                fileId: fileId
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
