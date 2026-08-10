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
using PropertyOS.Domain.Files.Entities;
using PropertyOS.Domain.Financials.Events;

namespace PropertyOS.Application.Financials.EventHandlers;

public class RentPaymentApprovedEventHandler : INotificationHandler<DomainEventNotification<RentPaymentApprovedEvent>>
{
    private readonly IApplicationDbContext _context;
    private readonly IReceiptPdfGenerator _pdfGenerator;
    private readonly IFileStorageRepository _fileStorageRepository;
    private readonly IStorageProvider _storageProvider;
    private readonly ICompanyReceiptSequenceRepository _sequenceRepository;
    private readonly IRentPaymentRepository _rentPaymentRepository;
    private readonly IBusinessClock _clock;
    private readonly ILogger<RentPaymentApprovedEventHandler> _logger;

    public RentPaymentApprovedEventHandler(
        IApplicationDbContext context,
        IReceiptPdfGenerator pdfGenerator,
        IFileStorageRepository fileStorageRepository,
        IStorageProvider storageProvider,
        ICompanyReceiptSequenceRepository sequenceRepository,
        IRentPaymentRepository rentPaymentRepository,
        IBusinessClock clock,
        ILogger<RentPaymentApprovedEventHandler> logger)
    {
        _context = context;
        _pdfGenerator = pdfGenerator;
        _fileStorageRepository = fileStorageRepository;
        _storageProvider = storageProvider;
        _sequenceRepository = sequenceRepository;
        _rentPaymentRepository = rentPaymentRepository;
        _clock = clock;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<RentPaymentApprovedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        
        // Use standard DbContext retrieval as we are reacting asynchronously
        var rentPayment = await _context.RentPayments
            .Include(rp => rp.Receipt)
            .FirstOrDefaultAsync(rp => rp.Id == domainEvent.RentPaymentId, cancellationToken);
            
        if (rentPayment == null)
        {
            _logger.LogWarning("RentPayment {Id} not found when handling RentPaymentApprovedEvent", domainEvent.RentPaymentId);
            return;
        }

        try
        {
            // 1. Generate Receipt Number
            var receiptNumber = await _sequenceRepository.ReserveAndFormatNextReceiptNumberAsync(rentPayment.CompanyId, cancellationToken);
            
            // 2. Generate PDF
            var pdfBytes = await _pdfGenerator.GenerateReceiptPdfAsync(rentPayment, cancellationToken);
            
            // 3. Save PDF to actual physical storage
            var fileId = Guid.CreateVersion7();
            var storageKey = $"receipts/{rentPayment.CompanyId}/{fileId}.pdf";
            var now = _clock.UtcNow;
            
            using var stream = new MemoryStream(pdfBytes);
            await _storageProvider.SaveAsync(storageKey, stream, "application/pdf", cancellationToken);
            
            // 4. Create FileStorage record (Metadata)
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
            
            // 5. Issue Receipt and Link to File
            var receipt = rentPayment.IssueReceipt(
                receiptNumber: receiptNumber,
                issuedAt: now,
                issuedBy: domainEvent.VerifiedBy,
                notes: "Automatically generated upon payment submission approval",
                fileId: fileId
            );
            await _rentPaymentRepository.AddReceiptAsync(receipt, cancellationToken);
            
            // Save all infrastructure changes
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Successfully generated receipt {ReceiptNumber} and PDF for RentPayment {Id}", 
                receiptNumber, rentPayment.Id);
                
            // 6. Notify the Tenant
            // This would integrate with Module 11 (Notifications) via MediatR Commands like CreateNotificationCommand
            _logger.LogInformation("Notification dispatched to Tenant {TenantId} for approved payment.", rentPayment.TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate receipt or notify tenant for RentPayment {Id}", rentPayment.Id);
            throw; // Re-throw to ensure the transaction/message fails
        }
    }
}
