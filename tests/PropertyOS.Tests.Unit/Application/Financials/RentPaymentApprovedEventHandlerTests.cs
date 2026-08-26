using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Models;
using PropertyOS.Application.Files;
using PropertyOS.Application.Files.Services;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.EventHandlers;
using PropertyOS.Application.Notifications;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Domain.Financials.Events;
using PropertyOS.Domain.Notifications;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials;

public class RentPaymentApprovedEventHandlerTests
{
    private static PropertyOsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PropertyOsDbContext(options);
    }

    [Fact]
    public async Task Handle_PartialPaymentApproval_GeneratesReceiptForExactSubmittedAmount()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var pdfGenerator = Substitute.For<IReceiptPdfGenerator>();
        var fileStorageRepo = Substitute.For<IFileStorageRepository>();
        var storageProvider = Substitute.For<IStorageProvider>();
        var sequenceRepo = Substitute.For<ICompanyReceiptSequenceRepository>();
        var rentPaymentRepo = Substitute.For<IRentPaymentRepository>();
        var notificationRepo = Substitute.For<INotificationRepository>();
        var postCommitRegistrar = Substitute.For<IPostCommitRegistrar>();
        var mediator = Substitute.For<ISender>();
        var clock = Substitute.For<IBusinessClock>();
        var logger = Substitute.For<ILogger<RentPaymentApprovedEventHandler>>();

        clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        sequenceRepo.ReserveAndFormatNextReceiptNumberAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns("REC-PARTIAL-001");
        pdfGenerator.GenerateReceiptPdfAsync(Arg.Any<RentPayment>(), Arg.Any<ReceiptPdfModel>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 1, 2, 3, 4 });

        var companyId = Guid.NewGuid();
        var rentPayment = RentPayment.Create(
            companyId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment, 1000, "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateTimeOffset.UtcNow, null);

        rentPayment.SubmitForVerification(300, PaymentMethod.BankTransfer, "REF_PARTIAL", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var submission = rentPayment.Submissions.First();

        await dbContext.RentPayments.AddAsync(rentPayment);
        await dbContext.SaveChangesAsync();

        rentPaymentRepo.AddReceiptAsync(Arg.Any<RentPaymentReceipt>(), Arg.Any<CancellationToken>())
            .Returns(ci => dbContext.RentPaymentReceipts.AddAsync(ci.Arg<RentPaymentReceipt>(), ci.Arg<CancellationToken>()).AsTask());

        notificationRepo.AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .Returns(ci => dbContext.Notifications.AddAsync(ci.Arg<Notification>(), ci.Arg<CancellationToken>()).AsTask());

        var handler = new RentPaymentApprovedEventHandler(
            dbContext, pdfGenerator, fileStorageRepo, storageProvider,
            sequenceRepo, rentPaymentRepo, notificationRepo, postCommitRegistrar,
            mediator, clock, logger);

        var domainEvent = new RentPaymentApprovedEvent(rentPayment.Id, submission.Id, Guid.NewGuid());
        var notification = new DomainEventNotification<RentPaymentApprovedEvent>(domainEvent);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - receipt sequence and PDF generation must be invoked for the 300 JOD transaction
        await sequenceRepo.Received(1).ReserveAndFormatNextReceiptNumberAsync(companyId, Arg.Any<CancellationToken>());
        await pdfGenerator.Received(1).GenerateReceiptPdfAsync(
            Arg.Any<RentPayment>(),
            Arg.Is<ReceiptPdfModel>(m => m.AmountPaid == 300 && m.ReceiptNumber == "REC-PARTIAL-001"),
            Arg.Any<CancellationToken>());
        await rentPaymentRepo.Received(1).AddReceiptAsync(
            Arg.Is<RentPaymentReceipt>(r => r.Amount == 300 && r.ReceiptNumber == "REC-PARTIAL-001"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IdempotentApproval_DoesNotGenerateDuplicateReceipt()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var pdfGenerator = Substitute.For<IReceiptPdfGenerator>();
        var fileStorageRepo = Substitute.For<IFileStorageRepository>();
        var storageProvider = Substitute.For<IStorageProvider>();
        var sequenceRepo = Substitute.For<ICompanyReceiptSequenceRepository>();
        var rentPaymentRepo = Substitute.For<IRentPaymentRepository>();
        var notificationRepo = Substitute.For<INotificationRepository>();
        var postCommitRegistrar = Substitute.For<IPostCommitRegistrar>();
        var mediator = Substitute.For<ISender>();
        var clock = Substitute.For<IBusinessClock>();
        var logger = Substitute.For<ILogger<RentPaymentApprovedEventHandler>>();

        var companyId = Guid.NewGuid();
        var rentPayment = RentPayment.Create(
            companyId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment, 1000, "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateTimeOffset.UtcNow, null);

        rentPayment.SubmitForVerification(300, PaymentMethod.BankTransfer, "REF_PARTIAL", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var submission = rentPayment.Submissions.First();

        // Issue receipt beforehand
        rentPayment.IssueReceipt("REC-EXISTING", DateTimeOffset.UtcNow, Guid.NewGuid(), amount: 300);

        await dbContext.RentPayments.AddAsync(rentPayment);
        await dbContext.SaveChangesAsync();

        var handler = new RentPaymentApprovedEventHandler(
            dbContext, pdfGenerator, fileStorageRepo, storageProvider,
            sequenceRepo, rentPaymentRepo, notificationRepo, postCommitRegistrar,
            mediator, clock, logger);

        var domainEvent = new RentPaymentApprovedEvent(rentPayment.Id, submission.Id, Guid.NewGuid());
        var notification = new DomainEventNotification<RentPaymentApprovedEvent>(domainEvent);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - must skip generation
        await sequenceRepo.DidNotReceive().ReserveAndFormatNextReceiptNumberAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await pdfGenerator.DidNotReceive().GenerateReceiptPdfAsync(Arg.Any<RentPayment>(), Arg.Any<ReceiptPdfModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SecondReceivingPayment_GeneratesDistinctReceiptForSecondTransaction()
    {
        // Arrange: Installment 320 JOD
        // Transaction 1 was 200 JOD (has receipt)
        // Transaction 2 is 120 JOD (receivingPayment 2)
        using var dbContext = CreateInMemoryDbContext();
        var pdfGenerator = Substitute.For<IReceiptPdfGenerator>();
        var fileStorageRepo = Substitute.For<IFileStorageRepository>();
        var storageProvider = Substitute.For<IStorageProvider>();
        var sequenceRepo = Substitute.For<ICompanyReceiptSequenceRepository>();
        var rentPaymentRepo = Substitute.For<IRentPaymentRepository>();
        var notificationRepo = Substitute.For<INotificationRepository>();
        var postCommitRegistrar = Substitute.For<IPostCommitRegistrar>();
        var mediator = Substitute.For<ISender>();
        var clock = Substitute.For<IBusinessClock>();
        var logger = Substitute.For<ILogger<RentPaymentApprovedEventHandler>>();

        clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        sequenceRepo.ReserveAndFormatNextReceiptNumberAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns("REC-002");
        pdfGenerator.GenerateReceiptPdfAsync(Arg.Any<RentPayment>(), Arg.Any<ReceiptPdfModel>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 1, 2, 3, 4 });

        var companyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();

        // 1. Installment (320 JOD)
        var installment = RentPayment.Create(
            companyId, leaseId, tenantId, buildingId, apartmentId,
            PaymentPurpose.ScheduledInstallment, 320, "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateTimeOffset.UtcNow, null);

        installment.SubmitForVerification(120, PaymentMethod.BankTransfer, "REF_120", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var submission2 = installment.Submissions.First();

        // 2. Receiving Payment #2 (UnallocatedReceipt for 120 JOD)
        var receivingPayment2 = RentPayment.Create(
            companyId, leaseId, tenantId, buildingId, apartmentId,
            PaymentPurpose.UnallocatedReceipt, 120, "JOD",
            null, null, null, DateTimeOffset.UtcNow, null);

        await dbContext.RentPayments.AddRangeAsync(installment, receivingPayment2);
        await dbContext.SaveChangesAsync();

        rentPaymentRepo.AddReceiptAsync(Arg.Any<RentPaymentReceipt>(), Arg.Any<CancellationToken>())
            .Returns(ci => dbContext.RentPaymentReceipts.AddAsync(ci.Arg<RentPaymentReceipt>(), ci.Arg<CancellationToken>()).AsTask());
        notificationRepo.AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .Returns(ci => dbContext.Notifications.AddAsync(ci.Arg<Notification>(), ci.Arg<CancellationToken>()).AsTask());

        var handler = new RentPaymentApprovedEventHandler(
            dbContext, pdfGenerator, fileStorageRepo, storageProvider,
            sequenceRepo, rentPaymentRepo, notificationRepo, postCommitRegistrar,
            mediator, clock, logger);

        var domainEvent = new RentPaymentApprovedEvent(installment.Id, submission2.Id, Guid.NewGuid(), ReceivingPaymentId: receivingPayment2.Id);
        var notification = new DomainEventNotification<RentPaymentApprovedEvent>(domainEvent);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - receipt generated for exactly 120 JOD on receivingPayment2
        await sequenceRepo.Received(1).ReserveAndFormatNextReceiptNumberAsync(companyId, Arg.Any<CancellationToken>());
        await pdfGenerator.Received(1).GenerateReceiptPdfAsync(
            Arg.Is<RentPayment>(p => p.Id == receivingPayment2.Id),
            Arg.Is<ReceiptPdfModel>(m => m.AmountPaid == 120 && m.ReceiptNumber == "REC-002"),
            Arg.Any<CancellationToken>());
        await rentPaymentRepo.Received(1).AddReceiptAsync(
            Arg.Is<RentPaymentReceipt>(r => r.RentPaymentId == receivingPayment2.Id && r.Amount == 120 && r.ReceiptNumber == "REC-002"),
            Arg.Any<CancellationToken>());
    }
}
