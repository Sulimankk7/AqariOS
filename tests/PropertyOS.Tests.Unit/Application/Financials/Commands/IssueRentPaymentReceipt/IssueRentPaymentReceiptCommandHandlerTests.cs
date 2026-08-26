using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Files;
using PropertyOS.Application.Files.Services;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Commands.IssueRentPaymentReceipt;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;
using PropertyOS.Domain.Files.Entities;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials.Commands.IssueRentPaymentReceipt;

public class IssueRentPaymentReceiptCommandHandlerTests
{
    private static PropertyOsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PropertyOsDbContext(options);
    }

    private class FakeRentPaymentRepository : IRentPaymentRepository
    {
        public List<RentPayment> Payments { get; } = new();
        public List<RentPaymentReceipt> AddedReceipts { get; } = new();

        public Task<RentPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Payments.FirstOrDefault(p => p.Id == id));

        public Task AddAsync(RentPayment rentPayment, CancellationToken cancellationToken = default)
        {
            Payments.Add(rentPayment);
            return Task.CompletedTask;
        }

        public Task<List<RentPayment>> GetByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
            => Task.FromResult(Payments.Where(p => ids.Contains(p.Id)).OrderBy(p => p.Id).ToList());

        public Task AddReceiptAsync(RentPaymentReceipt receipt, CancellationToken cancellationToken = default)
        {
            AddedReceipts.Add(receipt);
            return Task.CompletedTask;
        }

        public int RentGracePeriodDays { get; set; } = 5;
        public Task<int> GetRentGracePeriodDaysAsync(Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult(RentGracePeriodDays);
        public Task AddAllocationAsync(PaymentAllocation allocation, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<PaymentAllocation>> GetAllocationsByObligationIdAsync(Guid obligationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<PaymentAllocation>> GetAllocationsByReceivingIdAsync(Guid receivingId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddRangeAsync(IEnumerable<RentPayment> rentPayments, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<BillingPeriod>> GetScheduledInstallmentPeriodsAsync(Guid leaseContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasScheduledInstallmentAsync(Guid leaseContractId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Guid>> GetOverdueCandidateIdsAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ChequeDetails?> LoadChequeAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddChequeAsync(ChequeDetails cheque, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PaymentAllocation?> LoadAllocationAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<RentPaymentDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> GetPaymentsForLeaseAsync(Guid leaseContractId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> GetPaymentsForTenantAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> SearchPaymentsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> GetOutstandingPaymentsAsync(Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> GetPaymentsAsync(RentPaymentFilterOptions filter, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PropertyOS.Application.Common.Models.KeysetPage<PropertyOS.Application.Financials.Queries.GetPendingPaymentVerifications.PaymentVerificationQueueItemDto>> GetPendingVerificationsAsync(Guid companyId, int pageSize, DateTimeOffset? lastSeenSubmittedAt, Guid? lastSeenId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<ChequeDetailDto>> GetChequesAsync(ChequeStatus? status, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<ChequeDetailDto>> GetUpcomingChequesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<RentPaymentReceiptDto?> GetReceiptByRentPaymentIdAsync(Guid rentPaymentId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentReceiptDto>> GetReceiptsAsync(RentPaymentReceiptFilterOptions filter, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private static RentPayment CreatePayment(
        Guid companyId,
        decimal amountDue = 1000m,
        decimal amountPaid = 1000m,
        DueDateStatus status = DueDateStatus.Paid,
        PaymentPurpose purpose = PaymentPurpose.ScheduledInstallment)
    {
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.DateTime);
        var payment = RentPayment.Create(
            companyId: companyId,
            leaseContractId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            purpose: purpose,
            amountDue: amountDue,
            currency: "JOD",
            billingPeriodStart: today,
            billingPeriodEnd: today.AddMonths(1),
            dueDate: today.AddDays(5),
            createdAt: now,
            createdBy: Guid.NewGuid()
        );

        if (amountPaid > 0 || status != DueDateStatus.Pending)
        {
            payment.UpdateAllocationSync(amountPaid, status, now, Guid.NewGuid());
        }

        return payment;
    }

    [Fact]
    public async Task ValidFullyPaidPayment_GeneratesPdf()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var payment = CreatePayment(companyId, 1000m, 1000m, DueDateStatus.Paid);

        var paymentRepo = new FakeRentPaymentRepository();
        await paymentRepo.AddAsync(payment);

        var sequenceRepo = Substitute.For<ICompanyReceiptSequenceRepository>();
        sequenceRepo.ReserveAndFormatNextReceiptNumberAsync(companyId, Arg.Any<CancellationToken>())
            .Returns("REC-00042");

        var samplePdfBytes = Encoding.UTF8.GetBytes("%PDF-1.4 sample receipt pdf bytes");
        var pdfGenerator = Substitute.For<IReceiptPdfGenerator>();
        pdfGenerator.GenerateReceiptPdfAsync(Arg.Any<RentPayment>(), Arg.Any<ReceiptPdfModel>(), Arg.Any<CancellationToken>())
            .Returns(samplePdfBytes);

        var fileStorageRepo = Substitute.For<IFileStorageRepository>();
        var storageProvider = Substitute.For<IStorageProvider>();

        var clock = Substitute.For<IBusinessClock>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CompanyId.Returns(companyId);

        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new IssueRentPaymentReceiptCommandHandler(
            paymentRepo, sequenceRepo, pdfGenerator, fileStorageRepo, storageProvider,
            dbContext, clock, tenantContext, currentUser);

        var command = new IssueRentPaymentReceiptCommand(payment.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be("REC-00042");
        await pdfGenerator.Received(1).GenerateReceiptPdfAsync(
            Arg.Is<RentPayment>(p => p.Id == payment.Id),
            Arg.Is<ReceiptPdfModel>(m => m.ReceiptNumber == "REC-00042" && m.AmountPaid == 1000m),
            Arg.Any<CancellationToken>());
        await storageProvider.Received(1).SaveAsync(
            Arg.Is<string>(k => k.StartsWith($"receipts/{companyId}/") && k.EndsWith(".pdf")),
            Arg.Any<Stream>(),
            "application/pdf",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidFullyPaidPayment_CreatesFileStorageRecord()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var payment = CreatePayment(companyId, 1000m, 1000m, DueDateStatus.Paid);

        var paymentRepo = new FakeRentPaymentRepository();
        await paymentRepo.AddAsync(payment);

        var sequenceRepo = Substitute.For<ICompanyReceiptSequenceRepository>();
        sequenceRepo.ReserveAndFormatNextReceiptNumberAsync(companyId, Arg.Any<CancellationToken>())
            .Returns("REC-00099");

        var samplePdfBytes = new byte[] { 1, 2, 3, 4, 5 };
        var pdfGenerator = Substitute.For<IReceiptPdfGenerator>();
        pdfGenerator.GenerateReceiptPdfAsync(Arg.Any<RentPayment>(), Arg.Any<ReceiptPdfModel>(), Arg.Any<CancellationToken>())
            .Returns(samplePdfBytes);

        var fileStorageRepo = Substitute.For<IFileStorageRepository>();
        var storageProvider = Substitute.For<IStorageProvider>();

        var clock = Substitute.For<IBusinessClock>();
        var now = DateTimeOffset.UtcNow;
        clock.UtcNow.Returns(now);

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CompanyId.Returns(companyId);

        var userId = Guid.NewGuid();
        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.UserId.Returns(userId);

        var handler = new IssueRentPaymentReceiptCommandHandler(
            paymentRepo, sequenceRepo, pdfGenerator, fileStorageRepo, storageProvider,
            dbContext, clock, tenantContext, currentUser);

        var command = new IssueRentPaymentReceiptCommand(payment.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await fileStorageRepo.Received(1).AddAsync(
            Arg.Is<FileStorage>(f =>
                f.CompanyId == companyId &&
                f.MimeType == "application/pdf" &&
                f.SizeBytes == samplePdfBytes.Length &&
                f.OriginalFilename == "Receipt_REC-00099.pdf" &&
                f.StorageKey.StartsWith($"receipts/{companyId}/") &&
                f.StorageKey.EndsWith(".pdf") &&
                f.CreatedBy == userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidFullyPaidPayment_PopulatesReceiptFileId()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var payment = CreatePayment(companyId, 1000m, 1000m, DueDateStatus.Paid);

        var paymentRepo = new FakeRentPaymentRepository();
        await paymentRepo.AddAsync(payment);

        var sequenceRepo = Substitute.For<ICompanyReceiptSequenceRepository>();
        sequenceRepo.ReserveAndFormatNextReceiptNumberAsync(companyId, Arg.Any<CancellationToken>())
            .Returns("REC-00001");

        var pdfGenerator = Substitute.For<IReceiptPdfGenerator>();
        pdfGenerator.GenerateReceiptPdfAsync(Arg.Any<RentPayment>(), Arg.Any<ReceiptPdfModel>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 10, 20 });

        var fileStorageRepo = Substitute.For<IFileStorageRepository>();
        var storageProvider = Substitute.For<IStorageProvider>();

        var clock = Substitute.For<IBusinessClock>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CompanyId.Returns(companyId);

        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new IssueRentPaymentReceiptCommandHandler(
            paymentRepo, sequenceRepo, pdfGenerator, fileStorageRepo, storageProvider,
            dbContext, clock, tenantContext, currentUser);

        var command = new IssueRentPaymentReceiptCommand(payment.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var receipt = paymentRepo.AddedReceipts.Should().ContainSingle().Subject;
        receipt.FileId.Should().NotBeNull();
        receipt.FileId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task ReceiptFileId_MatchesFileStorageId()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var payment = CreatePayment(companyId, 1000m, 1000m, DueDateStatus.Paid);

        var paymentRepo = new FakeRentPaymentRepository();
        await paymentRepo.AddAsync(payment);

        var sequenceRepo = Substitute.For<ICompanyReceiptSequenceRepository>();
        sequenceRepo.ReserveAndFormatNextReceiptNumberAsync(companyId, Arg.Any<CancellationToken>())
            .Returns("REC-00001");

        var pdfGenerator = Substitute.For<IReceiptPdfGenerator>();
        pdfGenerator.GenerateReceiptPdfAsync(Arg.Any<RentPayment>(), Arg.Any<ReceiptPdfModel>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 10, 20 });

        FileStorage? capturedFileStorage = null;
        var fileStorageRepo = Substitute.For<IFileStorageRepository>();
        await fileStorageRepo.AddAsync(Arg.Do<FileStorage>(fs => capturedFileStorage = fs), Arg.Any<CancellationToken>());

        var storageProvider = Substitute.For<IStorageProvider>();

        var clock = Substitute.For<IBusinessClock>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CompanyId.Returns(companyId);

        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new IssueRentPaymentReceiptCommandHandler(
            paymentRepo, sequenceRepo, pdfGenerator, fileStorageRepo, storageProvider,
            dbContext, clock, tenantContext, currentUser);

        var command = new IssueRentPaymentReceiptCommand(payment.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var receipt = paymentRepo.AddedReceipts.Single();
        capturedFileStorage.Should().NotBeNull();
        receipt.FileId.Should().Be(capturedFileStorage!.Id);
    }

    [Fact]
    public async Task UnpaidPayment_DoesNotGeneratePdf()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var payment = CreatePayment(companyId, 1000m, 0m, DueDateStatus.Pending);

        var paymentRepo = new FakeRentPaymentRepository();
        await paymentRepo.AddAsync(payment);

        var sequenceRepo = Substitute.For<ICompanyReceiptSequenceRepository>();
        var pdfGenerator = Substitute.For<IReceiptPdfGenerator>();
        var fileStorageRepo = Substitute.For<IFileStorageRepository>();
        var storageProvider = Substitute.For<IStorageProvider>();

        var clock = Substitute.For<IBusinessClock>();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CompanyId.Returns(companyId);

        var currentUser = Substitute.For<ICurrentUserContext>();

        var handler = new IssueRentPaymentReceiptCommandHandler(
            paymentRepo, sequenceRepo, pdfGenerator, fileStorageRepo, storageProvider,
            dbContext, clock, tenantContext, currentUser);

        var command = new IssueRentPaymentReceiptCommand(payment.Id);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*installment that is not fully paid*");
        await pdfGenerator.DidNotReceive().GenerateReceiptPdfAsync(Arg.Any<RentPayment>(), Arg.Any<ReceiptPdfModel>(), Arg.Any<CancellationToken>());
        await storageProvider.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PartiallyPaidPayment_DoesNotGeneratePdf()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var payment = CreatePayment(companyId, 1000m, 200m, DueDateStatus.PartiallyPaid);

        var paymentRepo = new FakeRentPaymentRepository();
        await paymentRepo.AddAsync(payment);

        var sequenceRepo = Substitute.For<ICompanyReceiptSequenceRepository>();
        var pdfGenerator = Substitute.For<IReceiptPdfGenerator>();
        var fileStorageRepo = Substitute.For<IFileStorageRepository>();
        var storageProvider = Substitute.For<IStorageProvider>();

        var clock = Substitute.For<IBusinessClock>();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CompanyId.Returns(companyId);

        var currentUser = Substitute.For<ICurrentUserContext>();

        var handler = new IssueRentPaymentReceiptCommandHandler(
            paymentRepo, sequenceRepo, pdfGenerator, fileStorageRepo, storageProvider,
            dbContext, clock, tenantContext, currentUser);

        var command = new IssueRentPaymentReceiptCommand(payment.Id);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*installment that is not fully paid*");
        await pdfGenerator.DidNotReceive().GenerateReceiptPdfAsync(Arg.Any<RentPayment>(), Arg.Any<ReceiptPdfModel>(), Arg.Any<CancellationToken>());
        await storageProvider.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelledPayment_DoesNotGeneratePdf()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var payment = CreatePayment(companyId, 1000m, 0m, DueDateStatus.Cancelled);

        var paymentRepo = new FakeRentPaymentRepository();
        await paymentRepo.AddAsync(payment);

        var sequenceRepo = Substitute.For<ICompanyReceiptSequenceRepository>();
        var pdfGenerator = Substitute.For<IReceiptPdfGenerator>();
        var fileStorageRepo = Substitute.For<IFileStorageRepository>();
        var storageProvider = Substitute.For<IStorageProvider>();

        var clock = Substitute.For<IBusinessClock>();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CompanyId.Returns(companyId);

        var currentUser = Substitute.For<ICurrentUserContext>();

        var handler = new IssueRentPaymentReceiptCommandHandler(
            paymentRepo, sequenceRepo, pdfGenerator, fileStorageRepo, storageProvider,
            dbContext, clock, tenantContext, currentUser);

        var command = new IssueRentPaymentReceiptCommand(payment.Id);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*cancelled payment*");
        await pdfGenerator.DidNotReceive().GenerateReceiptPdfAsync(Arg.Any<RentPayment>(), Arg.Any<ReceiptPdfModel>(), Arg.Any<CancellationToken>());
        await storageProvider.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DuplicateReceipt_DoesNotGeneratePdf()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var payment = CreatePayment(companyId, 1000m, 1000m, DueDateStatus.Paid);

        var paymentRepo = new FakeRentPaymentRepository();
        await paymentRepo.AddAsync(payment);

        var sequenceRepo = Substitute.For<ICompanyReceiptSequenceRepository>();
        sequenceRepo.ReserveAndFormatNextReceiptNumberAsync(companyId, Arg.Any<CancellationToken>())
            .Returns("REC-00001");

        var pdfGenerator = Substitute.For<IReceiptPdfGenerator>();
        pdfGenerator.GenerateReceiptPdfAsync(Arg.Any<RentPayment>(), Arg.Any<ReceiptPdfModel>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 1 });

        var fileStorageRepo = Substitute.For<IFileStorageRepository>();
        var storageProvider = Substitute.For<IStorageProvider>();

        var clock = Substitute.For<IBusinessClock>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CompanyId.Returns(companyId);

        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new IssueRentPaymentReceiptCommandHandler(
            paymentRepo, sequenceRepo, pdfGenerator, fileStorageRepo, storageProvider,
            dbContext, clock, tenantContext, currentUser);

        var command = new IssueRentPaymentReceiptCommand(payment.Id);

        // First issue succeeds
        await handler.Handle(command, CancellationToken.None);

        // Clear received calls to inspect second attempt
        pdfGenerator.ClearReceivedCalls();
        storageProvider.ClearReceivedCalls();

        // Act - second attempt
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*receipt has already been issued*");
        await pdfGenerator.DidNotReceive().GenerateReceiptPdfAsync(Arg.Any<RentPayment>(), Arg.Any<ReceiptPdfModel>(), Arg.Any<CancellationToken>());
        await storageProvider.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PdfGenerationFailure_DoesNotPersistIncompleteReceipt()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var payment = CreatePayment(companyId, 1000m, 1000m, DueDateStatus.Paid);

        var paymentRepo = new FakeRentPaymentRepository();
        await paymentRepo.AddAsync(payment);

        var sequenceRepo = Substitute.For<ICompanyReceiptSequenceRepository>();
        sequenceRepo.ReserveAndFormatNextReceiptNumberAsync(companyId, Arg.Any<CancellationToken>())
            .Returns("REC-00001");

        var pdfGenerator = Substitute.For<IReceiptPdfGenerator>();
        pdfGenerator.GenerateReceiptPdfAsync(Arg.Any<RentPayment>(), Arg.Any<ReceiptPdfModel>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("PDF generation crashed"));

        var fileStorageRepo = Substitute.For<IFileStorageRepository>();
        var storageProvider = Substitute.For<IStorageProvider>();

        var clock = Substitute.For<IBusinessClock>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CompanyId.Returns(companyId);

        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new IssueRentPaymentReceiptCommandHandler(
            paymentRepo, sequenceRepo, pdfGenerator, fileStorageRepo, storageProvider,
            dbContext, clock, tenantContext, currentUser);

        var command = new IssueRentPaymentReceiptCommand(payment.Id);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("PDF generation crashed");
        paymentRepo.AddedReceipts.Should().BeEmpty();
        await fileStorageRepo.DidNotReceive().AddAsync(Arg.Any<FileStorage>(), Arg.Any<CancellationToken>());
        await storageProvider.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StorageFailure_DoesNotPersistIncompleteReceipt()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var payment = CreatePayment(companyId, 1000m, 1000m, DueDateStatus.Paid);

        var paymentRepo = new FakeRentPaymentRepository();
        await paymentRepo.AddAsync(payment);

        var sequenceRepo = Substitute.For<ICompanyReceiptSequenceRepository>();
        sequenceRepo.ReserveAndFormatNextReceiptNumberAsync(companyId, Arg.Any<CancellationToken>())
            .Returns("REC-00001");

        var pdfGenerator = Substitute.For<IReceiptPdfGenerator>();
        pdfGenerator.GenerateReceiptPdfAsync(Arg.Any<RentPayment>(), Arg.Any<ReceiptPdfModel>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 1, 2, 3 });

        var storageProvider = Substitute.For<IStorageProvider>();
        storageProvider.SaveAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("S3 connection timed out"));

        var fileStorageRepo = Substitute.For<IFileStorageRepository>();

        var clock = Substitute.For<IBusinessClock>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CompanyId.Returns(companyId);

        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new IssueRentPaymentReceiptCommandHandler(
            paymentRepo, sequenceRepo, pdfGenerator, fileStorageRepo, storageProvider,
            dbContext, clock, tenantContext, currentUser);

        var command = new IssueRentPaymentReceiptCommand(payment.Id);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<IOException>().WithMessage("S3 connection timed out");
        paymentRepo.AddedReceipts.Should().BeEmpty();
        await fileStorageRepo.DidNotReceive().AddAsync(Arg.Any<FileStorage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GeneratedStorageKey_IsCompanyScoped()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var payment = CreatePayment(companyId, 1000m, 1000m, DueDateStatus.Paid);

        var paymentRepo = new FakeRentPaymentRepository();
        await paymentRepo.AddAsync(payment);

        var sequenceRepo = Substitute.For<ICompanyReceiptSequenceRepository>();
        sequenceRepo.ReserveAndFormatNextReceiptNumberAsync(companyId, Arg.Any<CancellationToken>())
            .Returns("REC-00001");

        var pdfGenerator = Substitute.For<IReceiptPdfGenerator>();
        pdfGenerator.GenerateReceiptPdfAsync(Arg.Any<RentPayment>(), Arg.Any<ReceiptPdfModel>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 100 });

        string? savedStorageKey = null;
        var storageProvider = Substitute.For<IStorageProvider>();
        await storageProvider.SaveAsync(Arg.Do<string>(k => savedStorageKey = k), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        var fileStorageRepo = Substitute.For<IFileStorageRepository>();
        var clock = Substitute.For<IBusinessClock>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CompanyId.Returns(companyId);

        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new IssueRentPaymentReceiptCommandHandler(
            paymentRepo, sequenceRepo, pdfGenerator, fileStorageRepo, storageProvider,
            dbContext, clock, tenantContext, currentUser);

        var command = new IssueRentPaymentReceiptCommand(payment.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        savedStorageKey.Should().NotBeNull();
        savedStorageKey.Should().StartWith($"receipts/{companyId}/");
        savedStorageKey.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task ReceiptAmount_RemainsEqualToAmountDueForFullyPaidInstallment()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var payment = CreatePayment(companyId, 1250.500m, 1250.500m, DueDateStatus.Paid);

        var paymentRepo = new FakeRentPaymentRepository();
        await paymentRepo.AddAsync(payment);

        var sequenceRepo = Substitute.For<ICompanyReceiptSequenceRepository>();
        sequenceRepo.ReserveAndFormatNextReceiptNumberAsync(companyId, Arg.Any<CancellationToken>())
            .Returns("REC-00001");

        var pdfGenerator = Substitute.For<IReceiptPdfGenerator>();
        pdfGenerator.GenerateReceiptPdfAsync(Arg.Any<RentPayment>(), Arg.Any<ReceiptPdfModel>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 1 });

        var fileStorageRepo = Substitute.For<IFileStorageRepository>();
        var storageProvider = Substitute.For<IStorageProvider>();

        var clock = Substitute.For<IBusinessClock>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CompanyId.Returns(companyId);

        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.UserId.Returns(Guid.NewGuid());

        var handler = new IssueRentPaymentReceiptCommandHandler(
            paymentRepo, sequenceRepo, pdfGenerator, fileStorageRepo, storageProvider,
            dbContext, clock, tenantContext, currentUser);

        var command = new IssueRentPaymentReceiptCommand(payment.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var receipt = paymentRepo.AddedReceipts.Single();
        receipt.Amount.Should().Be(1250.500m);
        receipt.Currency.Should().Be("JOD");
    }

    [Fact]
    public async Task ExistingTenantIsolationBehavior_RemainsUnchanged()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var callerCompanyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        var payment = CreatePayment(otherCompanyId, 1000m, 1000m, DueDateStatus.Paid);

        var paymentRepo = new FakeRentPaymentRepository();
        await paymentRepo.AddAsync(payment);

        var sequenceRepo = Substitute.For<ICompanyReceiptSequenceRepository>();
        var pdfGenerator = Substitute.For<IReceiptPdfGenerator>();
        var fileStorageRepo = Substitute.For<IFileStorageRepository>();
        var storageProvider = Substitute.For<IStorageProvider>();
        var clock = Substitute.For<IBusinessClock>();

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CompanyId.Returns(callerCompanyId);

        var currentUser = Substitute.For<ICurrentUserContext>();

        var handler = new IssueRentPaymentReceiptCommandHandler(
            paymentRepo, sequenceRepo, pdfGenerator, fileStorageRepo, storageProvider,
            dbContext, clock, tenantContext, currentUser);

        var command = new IssueRentPaymentReceiptCommand(payment.Id);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert - masked as not found
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"RentPayment with ID {payment.Id} was not found.");
    }
}
