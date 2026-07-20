using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials.Commands.ReversePaymentAllocation;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials;

public class ReversePaymentAllocationCommandHandlerTests
{
    private class FakeRentPaymentRepository : IRentPaymentRepository
    {
        public Dictionary<Guid, RentPayment> Payments { get; } = new();
        public Dictionary<Guid, PaymentAllocation> Allocations { get; } = new();

        public Task<RentPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Payments.TryGetValue(id, out var p);
            return Task.FromResult(p);
        }

        public Task<PaymentAllocation?> LoadAllocationAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Allocations.TryGetValue(id, out var a);
            return Task.FromResult(a);
        }

        public Task<List<PaymentAllocation>> GetAllocationsByObligationIdAsync(Guid obligationId, CancellationToken cancellationToken = default)
        {
            var res = Allocations.Values.Where(a => a.ObligationPaymentId == obligationId).ToList();
            return Task.FromResult(res);
        }

        public Task AddAsync(RentPayment rentPayment, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddRangeAsync(IEnumerable<RentPayment> rentPayments, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasScheduledInstallmentAsync(Guid leaseContractId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ChequeDetails?> LoadChequeAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddChequeAsync(ChequeDetails cheque, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAllocationAsync(PaymentAllocation allocation, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<PaymentAllocation>> GetAllocationsByReceivingIdAsync(Guid receivingId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<RentPaymentDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> GetPaymentsForLeaseAsync(Guid leaseContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> GetPaymentsForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> SearchPaymentsAsync(string searchTerm, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> GetOutstandingPaymentsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<ChequeDetailDto>> GetChequesByStatusAsync(ChequeStatus status, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<ChequeDetailDto>> GetUpcomingChequesAsync(int daysAhead, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<RentPaymentReceiptDto?> GetReceiptByRentPaymentIdAsync(Guid rentPaymentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentReceiptDto>> GetReceiptsAsync(RentPaymentReceiptFilterOptions filter, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeCurrentUserContext : ICurrentUserContext
    {
        public Guid? UserId { get; } = Guid.NewGuid();
    }

    private RentPayment CreateRentPayment(Guid id, decimal amountDue, decimal amountPaid, DueDateStatus dueStatus)
    {
        var payment = RentPayment.Create(
            companyId: Guid.NewGuid(),
            leaseContractId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            purpose: PaymentPurpose.ScheduledInstallment,
            amountDue: amountDue,
            currency: "JOD",
            billingPeriodStart: new DateOnly(2026, 1, 1),
            billingPeriodEnd: new DateOnly(2026, 2, 1),
            dueDate: new DateOnly(2026, 1, 5),
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()
        );

        // Reflection to set ID, DueDateStatus, and AmountPaid
        var idProp = typeof(RentPayment).GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        idProp?.SetValue(payment, id);

        var statusProp = typeof(RentPayment).GetProperty("DueDateStatus", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        statusProp?.SetValue(payment, dueStatus);

        var paidProp = typeof(RentPayment).GetProperty("AmountPaid", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        paidProp?.SetValue(payment, amountPaid);

        return payment;
    }

    private PaymentAllocation CreateAllocation(Guid id, Guid receivingId, Guid obligationId, decimal amount, AllocationStatus status = AllocationStatus.Active)
    {
        var allocation = PaymentAllocation.Create(
            companyId: Guid.NewGuid(),
            receivingPaymentId: receivingId,
            obligationPaymentId: obligationId,
            allocatedAmount: amount,
            allocationDate: new DateOnly(2026, 1, 10),
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()
        );

        // Reflection to set ID and AllocationStatus
        var idProp = typeof(PaymentAllocation).GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        idProp?.SetValue(allocation, id);

        var statusProp = typeof(PaymentAllocation).GetProperty("AllocationStatus", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        statusProp?.SetValue(allocation, status);

        return allocation;
    }

    [Fact]
    public async Task Handle_HappyPath_ReversesAllocation_And_UpdatesObligationSync()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new ReversePaymentAllocationCommandHandler(repo, new FakeCurrentUserContext());

        var obligationId = Guid.NewGuid();
        var obligation = CreateRentPayment(obligationId, 1000m, 1000m, DueDateStatus.Paid);
        repo.Payments[obligationId] = obligation;

        var receivingId = Guid.NewGuid();
        var allocId = Guid.NewGuid();
        var allocation = CreateAllocation(allocId, receivingId, obligationId, 1000m);
        repo.Allocations[allocId] = allocation;

        var command = new ReversePaymentAllocationCommand(allocId, "Mistaken allocation");
        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(AllocationStatus.Reversed, allocation.AllocationStatus);
        Assert.Equal("Mistaken allocation", allocation.ReversalReason);
        Assert.NotNull(allocation.ReversedAt);

        // Target obligation should be synchronized back to Late/0 paid
        Assert.Equal(0m, obligation.AmountPaid);
        Assert.Equal(DueDateStatus.Late, obligation.DueDateStatus);
    }

    [Fact]
    public async Task Handle_AlreadyReversed_ThrowsInvalidOperationException()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new ReversePaymentAllocationCommandHandler(repo, new FakeCurrentUserContext());

        var obligationId = Guid.NewGuid();
        var receivingId = Guid.NewGuid();
        var allocId = Guid.NewGuid();
        var allocation = CreateAllocation(allocId, receivingId, obligationId, 1000m, AllocationStatus.Reversed);
        repo.Allocations[allocId] = allocation;

        var command = new ReversePaymentAllocationCommand(allocId, "Attempt double reversal");

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }
}
