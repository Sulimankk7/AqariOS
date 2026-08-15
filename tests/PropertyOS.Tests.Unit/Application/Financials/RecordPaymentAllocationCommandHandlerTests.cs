using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials.Commands.RecordPaymentAllocation;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials;

public class RecordPaymentAllocationCommandHandlerTests
{
    private class FakeRentPaymentRepository : IRentPaymentRepository
    {
        public Dictionary<Guid, RentPayment> Payments { get; } = new();
        public List<PaymentAllocation> Allocations { get; } = new();

        public Task<RentPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Payments.TryGetValue(id, out var p);
            return Task.FromResult(p);
        }

        public Task AddAsync(RentPayment rentPayment, CancellationToken cancellationToken = default)
        {
            Payments[rentPayment.Id] = rentPayment;
            return Task.CompletedTask;
        }

        public int RentGracePeriodDays { get; set; } = 0;
        public Task<int> GetRentGracePeriodDaysAsync(Guid companyId, CancellationToken cancellationToken = default)
            => Task.FromResult(RentGracePeriodDays);

        public Task<List<RentPayment>> GetByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
            => Task.FromResult(Payments.Values.Where(p => ids.Contains(p.Id)).OrderBy(p => p.Id).ToList());

        public Task AddReceiptAsync(RentPaymentReceipt receipt, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AddRangeAsync(IEnumerable<RentPayment> rentPayments, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<List<BillingPeriod>> GetScheduledInstallmentPeriodsAsync(Guid leaseContractId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<BillingPeriod>());

        public Task<bool> HasScheduledInstallmentAsync(Guid leaseContractId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Guid>> GetOverdueCandidateIdsAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ChequeDetails?> LoadChequeAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddChequeAsync(ChequeDetails cheque, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PaymentAllocation?> LoadAllocationAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task AddAllocationAsync(PaymentAllocation allocation, CancellationToken cancellationToken = default)
        {
            Allocations.Add(allocation);
            return Task.CompletedTask;
        }

        public Task<List<PaymentAllocation>> GetAllocationsByObligationIdAsync(Guid obligationId, CancellationToken cancellationToken = default)
        {
            var res = Allocations.Where(a => a.ObligationPaymentId == obligationId).ToList();
            return Task.FromResult(res);
        }

        public Task<List<PaymentAllocation>> GetAllocationsByReceivingIdAsync(Guid receivingId, CancellationToken cancellationToken = default)
        {
            var res = Allocations.Where(a => a.ReceivingPaymentId == receivingId).ToList();
            return Task.FromResult(res);
        }

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

    private class FakeCurrentUserContext : ICurrentUserContext
    {
        public Guid? UserId { get; } = Guid.NewGuid();
    }

    private RentPayment CreateRentPayment(Guid id, PaymentPurpose purpose, decimal amount, DueDateStatus dueStatus = DueDateStatus.Pending)
    {
        var payment = RentPayment.Create(
            companyId: Guid.NewGuid(),
            leaseContractId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            purpose: purpose,
            amountDue: amount,
            currency: "JOD",
            billingPeriodStart: purpose == PaymentPurpose.ScheduledInstallment ? new DateOnly(2026, 1, 1) : null,
            billingPeriodEnd: purpose == PaymentPurpose.ScheduledInstallment ? new DateOnly(2026, 2, 1) : null,
            dueDate: purpose == PaymentPurpose.ScheduledInstallment ? new DateOnly(2026, 1, 5) : null,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()
        );

        // Reflection to set ID and DueDateStatus
        var idProp = typeof(RentPayment).GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        idProp?.SetValue(payment, id);

        var statusProp = typeof(RentPayment).GetProperty("DueDateStatus", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        statusProp?.SetValue(payment, dueStatus);

        return payment;
    }

    [Fact]
    public async Task Handle_HappyPath_CreatesAllocation_And_UpdatesObligationSync()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new RecordPaymentAllocationCommandHandler(repo, new FakeCurrentUserContext());

        var receivingId = Guid.NewGuid();
        var receiving = CreateRentPayment(receivingId, PaymentPurpose.UnallocatedReceipt, 500m);
        await repo.AddAsync(receiving);

        var obligationId = Guid.NewGuid();
        var obligation = CreateRentPayment(obligationId, PaymentPurpose.ScheduledInstallment, 1000m);
        await repo.AddAsync(obligation);

        var command = new RecordPaymentAllocationCommand(
            ReceivingPaymentId: receivingId,
            Allocations: new List<AllocationDetail> { new(obligationId, 400m) },
            AllocationDate: new DateOnly(2026, 1, 10)
        );

        await handler.Handle(command, CancellationToken.None);

        Assert.Single(repo.Allocations);
        var alloc = repo.Allocations.First();
        Assert.Equal(receivingId, alloc.ReceivingPaymentId);
        Assert.Equal(obligationId, alloc.ObligationPaymentId);
        Assert.Equal(400m, alloc.AllocatedAmount);
        Assert.Equal(AllocationStatus.Active, alloc.AllocationStatus);

        // Target obligation should be synchronized in-memory. Doc §6.1 matrix: a partial
        // payment past the grace-adjusted due date derives Late (fake grace = 0 days).
        Assert.Equal(400m, obligation.AmountPaid);
        Assert.Equal(DueDateStatus.Late, obligation.DueDateStatus);
    }

    [Fact]
    public async Task Handle_FullyPaidObligation_TransitionsToPaid()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new RecordPaymentAllocationCommandHandler(repo, new FakeCurrentUserContext());

        var receivingId = Guid.NewGuid();
        var receiving = CreateRentPayment(receivingId, PaymentPurpose.UnallocatedReceipt, 1000m);
        await repo.AddAsync(receiving);

        var obligationId = Guid.NewGuid();
        var obligation = CreateRentPayment(obligationId, PaymentPurpose.ScheduledInstallment, 1000m);
        await repo.AddAsync(obligation);

        var command = new RecordPaymentAllocationCommand(
            ReceivingPaymentId: receivingId,
            Allocations: new List<AllocationDetail> { new(obligationId, 1000m) },
            AllocationDate: new DateOnly(2026, 1, 10)
        );

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(1000m, obligation.AmountPaid);
        Assert.Equal(DueDateStatus.Paid, obligation.DueDateStatus);
    }

    [Fact]
    public async Task Handle_SelfAllocation_ThrowsBusinessRuleException()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new RecordPaymentAllocationCommandHandler(repo, new FakeCurrentUserContext());

        var receivingId = Guid.NewGuid();
        var receiving = CreateRentPayment(receivingId, PaymentPurpose.UnallocatedReceipt, 500m);
        await repo.AddAsync(receiving);

        var command = new RecordPaymentAllocationCommand(
            ReceivingPaymentId: receivingId,
            Allocations: new List<AllocationDetail> { new(receivingId, 200m) },
            AllocationDate: new DateOnly(2026, 1, 10)
        );

        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SourceIsScheduledInstallment_ThrowsBusinessRuleException()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new RecordPaymentAllocationCommandHandler(repo, new FakeCurrentUserContext());

        var receivingId = Guid.NewGuid();
        var receiving = CreateRentPayment(receivingId, PaymentPurpose.ScheduledInstallment, 500m);
        await repo.AddAsync(receiving);

        var obligationId = Guid.NewGuid();
        var obligation = CreateRentPayment(obligationId, PaymentPurpose.ScheduledInstallment, 1000m);
        await repo.AddAsync(obligation);

        var command = new RecordPaymentAllocationCommand(
            ReceivingPaymentId: receivingId,
            Allocations: new List<AllocationDetail> { new(obligationId, 200m) },
            AllocationDate: new DateOnly(2026, 1, 10)
        );

        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DestinationIsCancelled_ThrowsBusinessRuleException()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new RecordPaymentAllocationCommandHandler(repo, new FakeCurrentUserContext());

        var receivingId = Guid.NewGuid();
        var receiving = CreateRentPayment(receivingId, PaymentPurpose.UnallocatedReceipt, 500m);
        await repo.AddAsync(receiving);

        var obligationId = Guid.NewGuid();
        var obligation = CreateRentPayment(obligationId, PaymentPurpose.ScheduledInstallment, 1000m, DueDateStatus.Cancelled);
        await repo.AddAsync(obligation);

        var command = new RecordPaymentAllocationCommand(
            ReceivingPaymentId: receivingId,
            Allocations: new List<AllocationDetail> { new(obligationId, 200m) },
            AllocationDate: new DateOnly(2026, 1, 10)
        );

        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AllocationExceedsSourceFunds_ThrowsBusinessRuleException()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new RecordPaymentAllocationCommandHandler(repo, new FakeCurrentUserContext());

        var receivingId = Guid.NewGuid();
        var receiving = CreateRentPayment(receivingId, PaymentPurpose.UnallocatedReceipt, 500m);
        await repo.AddAsync(receiving);

        var obligationId = Guid.NewGuid();
        var obligation = CreateRentPayment(obligationId, PaymentPurpose.ScheduledInstallment, 1000m);
        await repo.AddAsync(obligation);

        var command = new RecordPaymentAllocationCommand(
            ReceivingPaymentId: receivingId,
            Allocations: new List<AllocationDetail> { new(obligationId, 600m) },
            AllocationDate: new DateOnly(2026, 1, 10)
        );

        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AllocationExceedsObligationOutstanding_ThrowsBusinessRuleException()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new RecordPaymentAllocationCommandHandler(repo, new FakeCurrentUserContext());

        var receivingId = Guid.NewGuid();
        var receiving = CreateRentPayment(receivingId, PaymentPurpose.UnallocatedReceipt, 1000m);
        await repo.AddAsync(receiving);

        var obligationId = Guid.NewGuid();
        var obligation = CreateRentPayment(obligationId, PaymentPurpose.ScheduledInstallment, 500m);
        await repo.AddAsync(obligation);

        var command = new RecordPaymentAllocationCommand(
            ReceivingPaymentId: receivingId,
            Allocations: new List<AllocationDetail> { new(obligationId, 600m) },
            AllocationDate: new DateOnly(2026, 1, 10)
        );

        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
    }
}
