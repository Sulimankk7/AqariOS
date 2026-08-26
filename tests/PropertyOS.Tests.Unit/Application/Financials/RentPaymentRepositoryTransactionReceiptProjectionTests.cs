using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Infrastructure.Financials.Repositories;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials;

public class RentPaymentRepositoryTransactionReceiptProjectionTests
{
    private static PropertyOsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PropertyOsDbContext(options);
    }

    [Fact]
    public async Task GetPaymentsForTenantAsync_MultiplePartialPayments_ProjectsDistinctAmountsAndReceiptNumbers()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // 1. One scheduled installment obligation (320 JOD)
        var installment = RentPayment.Create(
            companyId, leaseId, tenantId, buildingId, apartmentId,
            PaymentPurpose.ScheduledInstallment, 320m, "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            DateTimeOffset.UtcNow,
            userId);

        installment.UpdateAllocationSync(320m, DueDateStatus.Paid, DateTimeOffset.UtcNow, userId);

        // 2. Receiving transaction #1 (150 JOD) + Receipt #1 ("REC-000018", 150 JOD) + Allocation #1 (150 JOD)
        var receivingPayment1 = RentPayment.Create(
            companyId, leaseId, tenantId, buildingId, apartmentId,
            PaymentPurpose.UnallocatedReceipt, 150m, "JOD",
            null, null, null,
            DateTimeOffset.UtcNow.AddMinutes(-10),
            userId);

        var receipt1 = receivingPayment1.IssueReceipt(
            receiptNumber: "REC-000018",
            issuedAt: DateTimeOffset.UtcNow.AddMinutes(-10),
            issuedBy: userId,
            notes: "Payment 1 receipt",
            fileId: Guid.NewGuid(),
            amount: 150m);

        var allocation1 = PaymentAllocation.Create(
            companyId,
            receivingPayment1.Id,
            installment.Id,
            allocatedAmount: 150m,
            allocationDate: DateOnly.FromDateTime(DateTime.UtcNow),
            createdAt: DateTimeOffset.UtcNow.AddMinutes(-10),
            createdBy: userId);

        // 3. Receiving transaction #2 (170 JOD) + Receipt #2 ("REC-000019", 170 JOD) + Allocation #2 (170 JOD)
        var receivingPayment2 = RentPayment.Create(
            companyId, leaseId, tenantId, buildingId, apartmentId,
            PaymentPurpose.UnallocatedReceipt, 170m, "JOD",
            null, null, null,
            DateTimeOffset.UtcNow,
            userId);

        var receipt2 = receivingPayment2.IssueReceipt(
            receiptNumber: "REC-000019",
            issuedAt: DateTimeOffset.UtcNow,
            issuedBy: userId,
            notes: "Payment 2 receipt",
            fileId: Guid.NewGuid(),
            amount: 170m);

        var allocation2 = PaymentAllocation.Create(
            companyId,
            receivingPayment2.Id,
            installment.Id,
            allocatedAmount: 170m,
            allocationDate: DateOnly.FromDateTime(DateTime.UtcNow),
            createdAt: DateTimeOffset.UtcNow,
            createdBy: userId);

        dbContext.RentPayments.AddRange(installment, receivingPayment1, receivingPayment2);
        dbContext.RentPaymentReceipts.AddRange(receipt1, receipt2);
        dbContext.PaymentAllocations.AddRange(allocation1, allocation2);
        await dbContext.SaveChangesAsync();

        var repo = new RentPaymentRepository(dbContext);

        // Act
        var result = await repo.GetPaymentsForTenantAsync(tenantId, companyId, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var paymentDto = result.Single();
        paymentDto.Id.Should().Be(installment.Id);
        paymentDto.AmountDue.Should().Be(320m);
        paymentDto.AmountPaid.Should().Be(320m);

        paymentDto.TransactionReceipts.Should().HaveCount(2);

        // Transaction 1: 150 JOD, REC-000018
        paymentDto.TransactionReceipts[0].Amount.Should().Be(150m);
        paymentDto.TransactionReceipts[0].ReceiptNumber.Should().Be("REC-000018");
        paymentDto.TransactionReceipts[0].ReceiptId.Should().Be(receipt1.Id);
        paymentDto.TransactionReceipts[0].PreviouslyPaid.Should().Be(0m);
        paymentDto.TransactionReceipts[0].RemainingAfter.Should().Be(170m);

        // Transaction 2: 170 JOD, REC-000019
        paymentDto.TransactionReceipts[1].Amount.Should().Be(170m);
        paymentDto.TransactionReceipts[1].ReceiptNumber.Should().Be("REC-000019");
        paymentDto.TransactionReceipts[1].ReceiptId.Should().Be(receipt2.Id);
        paymentDto.TransactionReceipts[1].PreviouslyPaid.Should().Be(150m);
        paymentDto.TransactionReceipts[1].RemainingAfter.Should().Be(0m);
    }

    [Fact]
    public async Task GetPaymentsForTenantAsync_ChangingSecondTransactionAmount_DoesNotAlterFirstTransactionProjectedAmount()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // 1. One scheduled installment obligation (350 JOD)
        var installment = RentPayment.Create(
            companyId, leaseId, tenantId, buildingId, apartmentId,
            PaymentPurpose.ScheduledInstallment, 350m, "JOD",
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            DateTimeOffset.UtcNow,
            userId);

        installment.UpdateAllocationSync(350m, DueDateStatus.Paid, DateTimeOffset.UtcNow, userId);

        // 2. Transaction 1 (150 JOD)
        var receivingPayment1 = RentPayment.Create(
            companyId, leaseId, tenantId, buildingId, apartmentId,
            PaymentPurpose.UnallocatedReceipt, 150m, "JOD",
            null, null, null,
            DateTimeOffset.UtcNow.AddMinutes(-20),
            userId);

        var receipt1 = receivingPayment1.IssueReceipt(
            receiptNumber: "REC-000018",
            issuedAt: DateTimeOffset.UtcNow.AddMinutes(-20),
            issuedBy: userId,
            notes: "Payment 1",
            fileId: Guid.NewGuid(),
            amount: 150m);

        var allocation1 = PaymentAllocation.Create(
            companyId,
            receivingPayment1.Id,
            installment.Id,
            allocatedAmount: 150m,
            allocationDate: DateOnly.FromDateTime(DateTime.UtcNow),
            createdAt: DateTimeOffset.UtcNow.AddMinutes(-20),
            createdBy: userId);

        // 3. Transaction 2 altered to 200 JOD (different from 170 JOD)
        var receivingPayment2 = RentPayment.Create(
            companyId, leaseId, tenantId, buildingId, apartmentId,
            PaymentPurpose.UnallocatedReceipt, 200m, "JOD",
            null, null, null,
            DateTimeOffset.UtcNow,
            userId);

        var receipt2 = receivingPayment2.IssueReceipt(
            receiptNumber: "REC-000099",
            issuedAt: DateTimeOffset.UtcNow,
            issuedBy: userId,
            notes: "Payment 2 altered",
            fileId: Guid.NewGuid(),
            amount: 200m);

        var allocation2 = PaymentAllocation.Create(
            companyId,
            receivingPayment2.Id,
            installment.Id,
            allocatedAmount: 200m,
            allocationDate: DateOnly.FromDateTime(DateTime.UtcNow),
            createdAt: DateTimeOffset.UtcNow,
            createdBy: userId);

        dbContext.RentPayments.AddRange(installment, receivingPayment1, receivingPayment2);
        dbContext.RentPaymentReceipts.AddRange(receipt1, receipt2);
        dbContext.PaymentAllocations.AddRange(allocation1, allocation2);
        await dbContext.SaveChangesAsync();

        var repo = new RentPaymentRepository(dbContext);

        // Act
        var result = await repo.GetPaymentsForTenantAsync(tenantId, companyId, CancellationToken.None);

        // Assert - Transaction 1 amount is strictly isolated to 150 JOD regardless of Transaction 2 amount
        var paymentDto = result.Single();
        paymentDto.TransactionReceipts.Should().HaveCount(2);

        paymentDto.TransactionReceipts[0].Amount.Should().Be(150m);
        paymentDto.TransactionReceipts[0].ReceiptNumber.Should().Be("REC-000018");

        paymentDto.TransactionReceipts[1].Amount.Should().Be(200m);
        paymentDto.TransactionReceipts[1].ReceiptNumber.Should().Be("REC-000099");
    }
}
