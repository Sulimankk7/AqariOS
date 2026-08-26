using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials.Queries.GetRentPaymentSettlementStatementPdf;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Properties;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials;

public class GetRentPaymentSettlementStatementPdfQueryHandlerTests
{
    private static PropertyOsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PropertyOsDbContext(options);
    }

    [Fact]
    public async Task Handle_FullyPaidInstallment_GeneratesSettlementStatementPdf()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var tenantContext = Substitute.For<ITenantContext>();
        var pdfGenerator = Substitute.For<IReceiptPdfGenerator>();
        var clock = Substitute.For<IBusinessClock>();

        var companyId = Guid.NewGuid();
        tenantContext.CompanyId.Returns(companyId);
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        pdfGenerator.GenerateSettlementStatementPdfAsync(Arg.Any<SettlementStatementPdfModel>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 10, 20, 30 });

        var leaseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();

        // 1. Installment (320 JOD - Fully Paid)
        var installment = RentPayment.Create(
            companyId, leaseId, tenantId, buildingId, apartmentId,
            PaymentPurpose.ScheduledInstallment, 320, "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateTimeOffset.UtcNow, null);

        installment.UpdateAllocationSync(200, DueDateStatus.PartiallyPaid, DateTimeOffset.UtcNow, null);
        installment.UpdateAllocationSync(320, DueDateStatus.Paid, DateTimeOffset.UtcNow, null); // now AmountPaid = 320, DueDateStatus = Paid

        // 2. Receiving Payments
        var rcv1 = RentPayment.Create(
            companyId, leaseId, tenantId, buildingId, apartmentId,
            PaymentPurpose.UnallocatedReceipt, 200, "JOD",
            null, null, null, DateTimeOffset.UtcNow, null);
        rcv1.IssueReceipt("REC-001", DateTimeOffset.UtcNow, Guid.NewGuid(), amount: 200);

        var rcv2 = RentPayment.Create(
            companyId, leaseId, tenantId, buildingId, apartmentId,
            PaymentPurpose.UnallocatedReceipt, 120, "JOD",
            null, null, null, DateTimeOffset.UtcNow, null);
        rcv2.IssueReceipt("REC-002", DateTimeOffset.UtcNow, Guid.NewGuid(), amount: 120);

        // 3. Allocations
        var alloc1 = PaymentAllocation.Create(
            companyId, rcv1.Id, installment.Id, 200,
            DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow, Guid.NewGuid());
        var alloc2 = PaymentAllocation.Create(
            companyId, rcv2.Id, installment.Id, 120,
            DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow, Guid.NewGuid());

        await dbContext.RentPayments.AddRangeAsync(installment, rcv1, rcv2);
        await dbContext.PaymentAllocations.AddRangeAsync(alloc1, alloc2);
        await dbContext.SaveChangesAsync();

        var handler = new GetRentPaymentSettlementStatementPdfQueryHandler(dbContext, tenantContext, pdfGenerator, clock);
        var query = new GetRentPaymentSettlementStatementPdfQuery(installment.Id, tenantId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().BeEquivalentTo(new byte[] { 10, 20, 30 });
        result.MimeType.Should().Be("application/pdf");

        await pdfGenerator.Received(1).GenerateSettlementStatementPdfAsync(
            Arg.Is<SettlementStatementPdfModel>(m =>
                m.TotalAmountDue == 320 &&
                m.TotalAmountPaid == 320 &&
                m.RemainingBalance == 0 &&
                m.Transactions.Count == 2 &&
                m.Transactions[0].Amount == 200 &&
                m.Transactions[0].ReceiptNumber == "REC-001" &&
                m.Transactions[1].Amount == 120 &&
                m.Transactions[1].ReceiptNumber == "REC-002"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PartiallyPaidInstallment_ThrowsBusinessRuleException()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var tenantContext = Substitute.For<ITenantContext>();
        var pdfGenerator = Substitute.For<IReceiptPdfGenerator>();
        var clock = Substitute.For<IBusinessClock>();

        var companyId = Guid.NewGuid();
        tenantContext.CompanyId.Returns(companyId);

        // Installment (320 JOD, paid 200 JOD -> PartiallyPaid)
        var installment = RentPayment.Create(
            companyId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment, 320, "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateTimeOffset.UtcNow, null);

        installment.UpdateAllocationSync(200, DueDateStatus.PartiallyPaid, DateTimeOffset.UtcNow, null);

        await dbContext.RentPayments.AddAsync(installment);
        await dbContext.SaveChangesAsync();

        var handler = new GetRentPaymentSettlementStatementPdfQueryHandler(dbContext, tenantContext, pdfGenerator, clock);
        var query = new GetRentPaymentSettlementStatementPdfQuery(installment.Id);

        // Act & Assert
        var act = () => handler.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Final settlement statement is only available for fully settled installments.*");
    }

    [Fact]
    public async Task Handle_CrossTenantAccess_ThrowsNotFoundException()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var tenantContext = Substitute.For<ITenantContext>();
        var pdfGenerator = Substitute.For<IReceiptPdfGenerator>();
        var clock = Substitute.For<IBusinessClock>();

        var companyId = Guid.NewGuid();
        var trueTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        tenantContext.CompanyId.Returns(companyId);

        var installment = RentPayment.Create(
            companyId, Guid.NewGuid(), trueTenantId, Guid.NewGuid(), Guid.NewGuid(),
            PaymentPurpose.ScheduledInstallment, 320, "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateTimeOffset.UtcNow, null);

        await dbContext.RentPayments.AddAsync(installment);
        await dbContext.SaveChangesAsync();

        var handler = new GetRentPaymentSettlementStatementPdfQueryHandler(dbContext, tenantContext, pdfGenerator, clock);
        var query = new GetRentPaymentSettlementStatementPdfQuery(installment.Id, otherTenantId);

        // Act & Assert
        var act = () => handler.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithRealQuestPdfGenerator_ReturnsValidPdfBytesWithPdfHeader()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var tenantContext = Substitute.For<ITenantContext>();
        var realPdfGenerator = new PropertyOS.Infrastructure.Files.Generators.QuestPdfReceiptGenerator();
        var clock = Substitute.For<IBusinessClock>();

        var companyId = Guid.NewGuid();
        tenantContext.CompanyId.Returns(companyId);
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var leaseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();

        // 1. Installment (320 JOD - Fully Paid)
        var installment = RentPayment.Create(
            companyId, leaseId, tenantId, buildingId, apartmentId,
            PaymentPurpose.ScheduledInstallment, 320, "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateTimeOffset.UtcNow, null);

        installment.UpdateAllocationSync(320, DueDateStatus.Paid, DateTimeOffset.UtcNow, null);

        // 2. Receiving Payments & Allocations
        var rcv1 = RentPayment.Create(
            companyId, leaseId, tenantId, buildingId, apartmentId,
            PaymentPurpose.UnallocatedReceipt, 150, "JOD",
            null, null, null, DateTimeOffset.UtcNow.AddMinutes(-10), null);
        rcv1.IssueReceipt("REC-000018", DateTimeOffset.UtcNow.AddMinutes(-10), Guid.NewGuid(), amount: 150);

        var rcv2 = RentPayment.Create(
            companyId, leaseId, tenantId, buildingId, apartmentId,
            PaymentPurpose.UnallocatedReceipt, 170, "JOD",
            null, null, null, DateTimeOffset.UtcNow, null);
        rcv2.IssueReceipt("REC-000019", DateTimeOffset.UtcNow, Guid.NewGuid(), amount: 170);

        var alloc1 = PaymentAllocation.Create(
            companyId, rcv1.Id, installment.Id, 150,
            DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow.AddMinutes(-10), Guid.NewGuid());
        var alloc2 = PaymentAllocation.Create(
            companyId, rcv2.Id, installment.Id, 170,
            DateOnly.FromDateTime(DateTime.UtcNow), DateTimeOffset.UtcNow, Guid.NewGuid());

        await dbContext.RentPayments.AddRangeAsync(installment, rcv1, rcv2);
        await dbContext.RentPaymentReceipts.AddRangeAsync(rcv1.Receipt!, rcv2.Receipt!);
        await dbContext.PaymentAllocations.AddRangeAsync(alloc1, alloc2);
        await dbContext.SaveChangesAsync();

        var handler = new GetRentPaymentSettlementStatementPdfQueryHandler(dbContext, tenantContext, realPdfGenerator, clock);
        var query = new GetRentPaymentSettlementStatementPdfQuery(installment.Id, tenantId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MimeType.Should().Be("application/pdf");
        result.Filename.Should().StartWith("Settlement_SETTLE-");
        result.Content.Should().NotBeNullOrEmpty();
        result.Content.Length.Should().BeGreaterThan(1000);

        var header = System.Text.Encoding.ASCII.GetString(result.Content, 0, 5);
        header.Should().Be("%PDF-");
    }
}
