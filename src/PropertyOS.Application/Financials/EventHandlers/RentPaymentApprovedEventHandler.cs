using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Models;
using PropertyOS.Application.Files;
using PropertyOS.Application.Files.Services;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Notifications;
using PropertyOS.Application.Notifications.Commands.DispatchNotification;
using PropertyOS.Domain.Files.Entities;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Domain.Financials.Events;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Application.Financials.EventHandlers;

public class RentPaymentApprovedEventHandler : INotificationHandler<DomainEventNotification<RentPaymentApprovedEvent>>
{
    private readonly IApplicationDbContext _context;
    private readonly IReceiptPdfGenerator _pdfGenerator;
    private readonly IFileStorageRepository _fileStorageRepository;
    private readonly IStorageProvider _storageProvider;
    private readonly ICompanyReceiptSequenceRepository _sequenceRepository;
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IPostCommitRegistrar _postCommitRegistrar;
    private readonly ISender _mediator;
    private readonly IBusinessClock _clock;
    private readonly ILogger<RentPaymentApprovedEventHandler> _logger;

    public RentPaymentApprovedEventHandler(
        IApplicationDbContext context,
        IReceiptPdfGenerator pdfGenerator,
        IFileStorageRepository fileStorageRepository,
        IStorageProvider storageProvider,
        ICompanyReceiptSequenceRepository sequenceRepository,
        IRentPaymentRepository rentPaymentRepository,
        INotificationRepository notificationRepository,
        IPostCommitRegistrar postCommitRegistrar,
        ISender mediator,
        IBusinessClock clock,
        ILogger<RentPaymentApprovedEventHandler> logger)
    {
        _context = context;
        _pdfGenerator = pdfGenerator;
        _fileStorageRepository = fileStorageRepository;
        _storageProvider = storageProvider;
        _sequenceRepository = sequenceRepository;
        _rentPaymentRepository = rentPaymentRepository;
        _notificationRepository = notificationRepository;
        _postCommitRegistrar = postCommitRegistrar;
        _mediator = mediator;
        _clock = clock;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<RentPaymentApprovedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        
        // Load the ScheduledInstallment obligation
        var rentPayment = await _context.RentPayments
            .Include(rp => rp.Submissions)
            .Include(rp => rp.Receipt)
            .FirstOrDefaultAsync(rp => rp.Id == domainEvent.RentPaymentId, cancellationToken);
            
        if (rentPayment == null)
        {
            _logger.LogWarning("RentPayment {Id} not found when handling RentPaymentApprovedEvent", domainEvent.RentPaymentId);
            return;
        }

        // Determine the authoritative money-in transaction (ReceivingPayment)
        RentPayment? receivingPayment = null;
        if (domainEvent.ReceivingPaymentId.HasValue)
        {
            receivingPayment = await _context.RentPayments
                .Include(rp => rp.Receipt)
                .FirstOrDefaultAsync(rp => rp.Id == domainEvent.ReceivingPaymentId.Value, cancellationToken);
        }

        if (receivingPayment == null)
        {
            // Fallback: locate the latest allocated receiving payment for this obligation
            var allocation = await _context.PaymentAllocations
                .Where(a => a.ObligationPaymentId == rentPayment.Id && a.AllocationStatus == AllocationStatus.Active && a.DeletedAt == null)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (allocation != null)
            {
                receivingPayment = await _context.RentPayments
                    .Include(rp => rp.Receipt)
                    .FirstOrDefaultAsync(rp => rp.Id == allocation.ReceivingPaymentId, cancellationToken);
            }
        }

        var targetPayment = receivingPayment ?? rentPayment;

        // Idempotency: Skip if active receipt is already issued for this transaction
        if (targetPayment.Receipt != null && targetPayment.Receipt.DeletedAt == null)
        {
            _logger.LogInformation("Receipt already exists for Payment {Id}; skipping duplicate generation.", targetPayment.Id);
            return;
        }

        try
        {
            // 1. Determine exact approved transaction amount and context
            var submission = rentPayment.Submissions.FirstOrDefault(s => s.Id == domainEvent.SubmissionId)
                             ?? rentPayment.Submissions.OrderByDescending(s => s.CreatedAt).FirstOrDefault();

            var transactionAmount = submission?.Amount 
                                   ?? (targetPayment != rentPayment ? targetPayment.AmountDue : (rentPayment.AmountPaid > 0 ? rentPayment.AmountPaid : rentPayment.AmountDue));

            if (transactionAmount <= 0)
            {
                _logger.LogWarning("Invalid transaction amount {Amount} for RentPayment {Id}; skipping receipt generation.", transactionAmount, rentPayment.Id);
                return;
            }

            var installmentTotal = rentPayment.AmountDue;
            var currentTotalPaid = rentPayment.AmountPaid > 0 ? rentPayment.AmountPaid : transactionAmount;
            var previouslyPaid = Math.Max(0, currentTotalPaid - transactionAmount);
            var remainingAfter = Math.Max(0, installmentTotal - currentTotalPaid);

            // 2. Generate Receipt Number
            var receiptNumber = await _sequenceRepository.ReserveAndFormatNextReceiptNumberAsync(rentPayment.CompanyId, cancellationToken);
            
            // 3. Fetch Display Entities for Clean PDF Rendering
            var tenant = await _context.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == rentPayment.TenantId, cancellationToken);
            var building = await _context.Buildings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == rentPayment.BuildingId, cancellationToken);
            var apartment = await _context.Apartments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == rentPayment.ApartmentId, cancellationToken);
            var contract = await _context.LeaseContracts.AsNoTracking().FirstOrDefaultAsync(lc => lc.Id == rentPayment.LeaseContractId, cancellationToken);

            var methodStr = submission?.PaymentMethod.ToString() ?? targetPayment.PaymentMethod?.ToString() ?? rentPayment.PaymentMethod?.ToString() ?? "Cash";
            var refNumber = submission?.ReferenceNumber ?? targetPayment.PaymentReferenceNumber ?? rentPayment.PaymentReferenceNumber;
            var now = _clock.UtcNow;
            var currency = string.IsNullOrWhiteSpace(rentPayment.Currency) ? "JOD" : rentPayment.Currency;

            var pdfModel = new ReceiptPdfModel(
                ReceiptNumber: receiptNumber,
                IssueDate: now,
                AmountPaid: transactionAmount,
                Currency: currency,
                PaymentMethod: methodStr,
                ReferenceNumber: refNumber,
                PaymentPurpose: targetPayment.PaymentPurpose.ToString(),
                BillingPeriod: rentPayment.BillingPeriodStart.HasValue && rentPayment.BillingPeriodEnd.HasValue
                    ? $"{rentPayment.BillingPeriodStart.Value:dd/MM/yyyy} - {rentPayment.BillingPeriodEnd.Value:dd/MM/yyyy}"
                    : null,
                DueDate: rentPayment.DueDate?.ToString("dd/MM/yyyy"),
                DueDateStatus: rentPayment.DueDateStatus.ToString(),
                TenantName: tenant?.Name ?? "Tenant",
                TenantPhone: tenant?.Phone,
                PropertyName: building?.Name ?? "Property",
                UnitNumber: apartment?.UnitNumber ?? "Unit",
                ContractNumber: contract?.ContractNumber ?? "Contract",
                ChequeNumber: submission?.ChequeNumber,
                BankName: submission?.BankName,
                ChequeIssueDate: submission?.ChequeIssueDate?.ToString("dd/MM/yyyy"),
                ChequeDueDate: submission?.ChequeDueDate?.ToString("dd/MM/yyyy"),
                InstallmentTotal: installmentTotal,
                PreviouslyPaid: previouslyPaid,
                RemainingAfter: remainingAfter
            );

            // 4. Generate PDF
            var pdfBytes = await _pdfGenerator.GenerateReceiptPdfAsync(targetPayment, pdfModel, cancellationToken);
            
            // 5. Save PDF to actual physical storage
            var fileId = Guid.CreateVersion7();
            var storageKey = $"receipts/{rentPayment.CompanyId}/{fileId}.pdf";
            
            using var stream = new MemoryStream(pdfBytes);
            await _storageProvider.SaveAsync(storageKey, stream, "application/pdf", cancellationToken);
            
            // 6. Create FileStorage record (Metadata)
            var fileStorage = FileStorage.Create(
                companyId: rentPayment.CompanyId,
                uploadedBy: domainEvent.VerifiedBy,
                originalFilename: $"Receipt_{receiptNumber}.pdf",
                mimeType: "application/pdf",
                sizeBytes: pdfBytes.Length,
                storageKey: storageKey,
                now: now,
                createdBy: domainEvent.VerifiedBy,
                id: fileId
            );
            await _fileStorageRepository.AddAsync(fileStorage, cancellationToken);
            
            // 7. Issue Receipt and Link to File on the transaction entity
            var receipt = targetPayment.IssueReceipt(
                receiptNumber: receiptNumber,
                issuedAt: now,
                issuedBy: domainEvent.VerifiedBy,
                notes: "Automatically generated upon payment approval",
                fileId: fileId,
                amount: transactionAmount
            );
            await _rentPaymentRepository.AddReceiptAsync(receipt, cancellationToken);

            // Maintain receipt number on installment if not yet populated
            if (string.IsNullOrWhiteSpace(rentPayment.ReceiptNumber))
            {
                rentPayment.SetPaymentReceiptDetails(
                    submission?.PaymentMethod ?? targetPayment.PaymentMethod ?? PaymentMethod.BankTransfer,
                    refNumber,
                    receiptNumber,
                    now,
                    domainEvent.VerifiedBy
                );
            }
            
            // 8. In-App Notification for Tenant
            Guid? notificationId = null;
            if (tenant?.UserId != null)
            {
                var subject = "تم اعتماد إثبات الدفع وإصدار سند القبض | Payment Approved & Receipt Issued";
                var body = $"تم اعتماد إثبات الدفع بقيمة {transactionAmount:N2} {currency} للقسط المستحق بنجاح. رقم سند القبض: {receiptNumber}. | Your payment of {transactionAmount:N2} {currency} was approved. Receipt Number: {receiptNumber}.";

                var notificationEntity = Notification.Create(
                    companyId: rentPayment.CompanyId,
                    recipientUserId: tenant.UserId.Value,
                    templateId: null,
                    notificationType: NotificationType.RentPaid,
                    subject: subject,
                    body: body,
                    priority: NotificationPriority.Normal,
                    createdAt: now,
                    createdBy: domainEvent.VerifiedBy
                );
                notificationEntity.AddDeliveryChannel(DeliveryChannel.InApp, now);
                if (!string.IsNullOrWhiteSpace(tenant.Email))
                {
                    notificationEntity.AddDeliveryChannel(DeliveryChannel.Email, now);
                }
                if (!string.IsNullOrWhiteSpace(tenant.Phone))
                {
                    notificationEntity.AddDeliveryChannel(DeliveryChannel.Sms, now);
                }

                await _notificationRepository.AddAsync(notificationEntity, cancellationToken);
                notificationId = notificationEntity.Id;
            }

            // Save all infrastructure changes
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Successfully generated receipt {ReceiptNumber} and PDF for RentPayment {Id} (Amount={Amount})", 
                receiptNumber, rentPayment.Id, transactionAmount);

            // 9. Post-commit dispatch of notification
            if (notificationId.HasValue)
            {
                var notifId = notificationId.Value;
                _postCommitRegistrar.RegisterPostCommitAction(async (ct) =>
                {
                    try
                    {
                        await _mediator.Send(new DispatchNotificationCommand(notifId), ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed post-commit dispatch for approval notification {NotificationId}", notifId);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate receipt or notify tenant for RentPayment {Id}", rentPayment.Id);
            throw; // Re-throw to ensure the transaction/message fails
        }
    }
}
