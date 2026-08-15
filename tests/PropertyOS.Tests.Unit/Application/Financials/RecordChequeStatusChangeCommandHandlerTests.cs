using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials.Commands.RecordChequeStatusChange;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials;

public class RecordChequeStatusChangeCommandHandlerTests
{
    private class FakeRentPaymentRepository : IRentPaymentRepository
    {
        public Dictionary<Guid, RentPayment> Payments { get; } = new();
        public Dictionary<Guid, ChequeDetails> Cheques { get; } = new();
        public List<PaymentAllocation> Allocations { get; } = new();

        public Task<RentPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Payments.TryGetValue(id, out var p);
            return Task.FromResult(p);
        }

        public Task<ChequeDetails?> LoadChequeAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Cheques.TryGetValue(id, out var c);
            return Task.FromResult(c);
        }

        public Task<List<PaymentAllocation>> GetAllocationsByReceivingIdAsync(Guid receivingId, CancellationToken cancellationToken = default)
        {
            var res = Allocations.Where(a => a.ReceivingPaymentId == receivingId).ToList();
            return Task.FromResult(res);
        }

        public Task<List<PaymentAllocation>> GetAllocationsByObligationIdAsync(Guid obligationId, CancellationToken cancellationToken = default)
        {
            var res = Allocations.Where(a => a.ObligationPaymentId == obligationId).ToList();
            return Task.FromResult(res);
        }

        public Task AddAsync(RentPayment rentPayment, CancellationToken cancellationToken = default) => throw new NotImplementedException();
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
        public Task AddChequeAsync(ChequeDetails cheque, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PaymentAllocation?> LoadAllocationAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAllocationAsync(PaymentAllocation allocation, CancellationToken cancellationToken = default) => throw new NotImplementedException();

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

    private ChequeDetails CreateCheque(Guid id, Guid rentPaymentId, ChequeStatus status = ChequeStatus.Issued)
    {
        var cheque = ChequeDetails.Create(
            companyId: Guid.NewGuid(),
            rentPaymentId: rentPaymentId,
            leaseContractId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            chequeNumber: "CHQ-123",
            bankName: "Arab Bank",
            bankBranch: null,
            amount: 500m,
            currency: "JOD",
            dueDate: new DateOnly(2026, 1, 10),
            issueDate: new DateOnly(2026, 1, 1),
            receivedDate: status != ChequeStatus.Issued ? new DateOnly(2026, 1, 2) : null,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()
        );

        // Reflection to set ID and Status
        var idProp = typeof(ChequeDetails).GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        idProp?.SetValue(cheque, id);

        var statusProp = typeof(ChequeDetails).GetProperty("Status", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        statusProp?.SetValue(cheque, status);

        return cheque;
    }

    private RentPayment CreateRentPayment(Guid id, PaymentPurpose purpose, decimal amountDue, decimal amountPaid, DueDateStatus dueStatus)
    {
        var payment = RentPayment.Create(
            companyId: Guid.NewGuid(),
            leaseContractId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            purpose: purpose,
            amountDue: amountDue,
            currency: "JOD",
            billingPeriodStart: purpose == PaymentPurpose.ScheduledInstallment ? new DateOnly(2026, 1, 1) : null,
            billingPeriodEnd: purpose == PaymentPurpose.ScheduledInstallment ? new DateOnly(2026, 2, 1) : null,
            dueDate: purpose == PaymentPurpose.ScheduledInstallment ? new DateOnly(2026, 1, 5) : null,
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

    private PaymentAllocation CreateAllocation(Guid id, Guid receivingId, Guid obligationId, decimal amount)
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

        // Reflection to set ID
        var idProp = typeof(PaymentAllocation).GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        idProp?.SetValue(allocation, id);

        return allocation;
    }

    [Fact]
    public async Task Handle_ReceiveDepositClearLifecycle_Succeeds()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new RecordChequeStatusChangeCommandHandler(repo, new FakeCurrentUserContext());

        var chequeId = Guid.NewGuid();
        var cheque = CreateCheque(chequeId, Guid.NewGuid(), ChequeStatus.Issued);
        repo.Cheques[chequeId] = cheque;

        // 1. Receive
        var cmdReceive = new RecordChequeStatusChangeCommand(chequeId, ChequeStatus.Received, new DateOnly(2026, 1, 2));
        await handler.Handle(cmdReceive, CancellationToken.None);
        Assert.Equal(ChequeStatus.Received, cheque.Status);
        Assert.Equal(new DateOnly(2026, 1, 2), cheque.ReceivedDate);

        // 2. Deposit
        var cmdDeposit = new RecordChequeStatusChangeCommand(chequeId, ChequeStatus.Deposited, new DateOnly(2026, 1, 5));
        await handler.Handle(cmdDeposit, CancellationToken.None);
        Assert.Equal(ChequeStatus.Deposited, cheque.Status);
        Assert.Equal(new DateOnly(2026, 1, 5), cheque.DepositDate);

        // 3. Clear
        var cmdClear = new RecordChequeStatusChangeCommand(chequeId, ChequeStatus.Cleared, new DateOnly(2026, 1, 10));
        await handler.Handle(cmdClear, CancellationToken.None);
        Assert.Equal(ChequeStatus.Cleared, cheque.Status);
        Assert.Equal(new DateOnly(2026, 1, 10), cheque.ClearanceDate);
    }

    [Fact]
    public async Task Handle_Bounce_ReversesAllocations_And_UpdatesObligationSync()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new RecordChequeStatusChangeCommandHandler(repo, new FakeCurrentUserContext());

        var receivingId = Guid.NewGuid();
        var receiving = CreateRentPayment(receivingId, PaymentPurpose.UnallocatedReceipt, 500m, 0m, DueDateStatus.Pending);
        repo.Payments[receivingId] = receiving;

        var chequeId = Guid.NewGuid();
        var cheque = CreateCheque(chequeId, receivingId, ChequeStatus.Received);
        cheque.Deposit(new DateOnly(2026, 1, 5), DateTimeOffset.UtcNow, Guid.NewGuid());
        repo.Cheques[chequeId] = cheque;

        var obligationId = Guid.NewGuid();
        var obligation = CreateRentPayment(obligationId, PaymentPurpose.ScheduledInstallment, 1000m, 500m, DueDateStatus.PartiallyPaid);
        repo.Payments[obligationId] = obligation;

        // Create active allocation linking the cheque's payment to the obligation
        var allocId = Guid.NewGuid();
        var allocation = CreateAllocation(allocId, receivingId, obligationId, 500m);
        repo.Allocations.Add(allocation);

        var cmdBounce = new RecordChequeStatusChangeCommand(
            ChequeId: chequeId,
            NewStatus: ChequeStatus.Bounced,
            ActionDate: new DateOnly(2026, 1, 12),
            BounceReason: "Insufficient Funds",
            BounceFeeCharged: 15m
        );

        await handler.Handle(cmdBounce, CancellationToken.None);

        Assert.Equal(ChequeStatus.Bounced, cheque.Status);
        Assert.Equal("Insufficient Funds", cheque.BounceReason);
        Assert.Equal(15m, cheque.BounceFeeCharged);
        Assert.Equal(new DateOnly(2026, 1, 12), cheque.BounceDate);

        // Allocation must be reversed
        Assert.Equal(AllocationStatus.Reversed, allocation.AllocationStatus);
        Assert.StartsWith("Cheque Bounced: Cheque CHQ-123 bounced on", allocation.ReversalReason);

        // Target obligation should be synchronized back to Late/0 paid
        Assert.Equal(0m, obligation.AmountPaid);
        Assert.Equal(DueDateStatus.OverdueUnpaid, obligation.DueDateStatus);
    }

    [Fact]
    public async Task Handle_InvalidStateTransition_ThrowsBusinessRuleException()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new RecordChequeStatusChangeCommandHandler(repo, new FakeCurrentUserContext());

        var chequeId = Guid.NewGuid();
        var cheque = CreateCheque(chequeId, Guid.NewGuid(), ChequeStatus.Issued);
        repo.Cheques[chequeId] = cheque;

        // Try to clear without depositing first
        var cmdClear = new RecordChequeStatusChangeCommand(chequeId, ChequeStatus.Cleared, new DateOnly(2026, 1, 10));

        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(cmdClear, CancellationToken.None));
    }
}
