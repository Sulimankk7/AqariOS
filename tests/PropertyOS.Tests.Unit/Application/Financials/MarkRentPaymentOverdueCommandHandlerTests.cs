using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Commands.MarkRentPaymentOverdue;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials;

public class MarkRentPaymentOverdueCommandHandlerTests
{
    private class FakeRentPaymentRepository : IRentPaymentRepository
    {
        public List<RentPayment> Payments { get; } = new();

        public Task<RentPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Payments.FirstOrDefault(p => p.Id == id));

        public Task AddAsync(RentPayment rentPayment, CancellationToken cancellationToken = default)
        {
            Payments.Add(rentPayment);
            return Task.CompletedTask;
        }

        public int RentGracePeriodDays { get; set; } = 0;
        public Task<int> GetRentGracePeriodDaysAsync(Guid companyId, CancellationToken cancellationToken = default)
            => Task.FromResult(RentGracePeriodDays);

        public Task<List<RentPayment>> GetByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
            => Task.FromResult(Payments.Where(p => ids.Contains(p.Id)).OrderBy(p => p.Id).ToList());

        public Task AddRangeAsync(IEnumerable<RentPayment> rentPayments, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<List<BillingPeriod>> GetScheduledInstallmentPeriodsAsync(Guid leaseContractId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<BillingPeriod>());

        public Task<bool> HasScheduledInstallmentAsync(Guid leaseContractId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Guid>> GetOverdueCandidateIdsAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ChequeDetails?> LoadChequeAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddChequeAsync(ChequeDetails cheque, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PaymentAllocation?> LoadAllocationAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAllocationAsync(PaymentAllocation allocation, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddReceiptAsync(RentPaymentReceipt receipt, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<PaymentAllocation>> GetAllocationsByObligationIdAsync(Guid obligationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<PaymentAllocation>> GetAllocationsByReceivingIdAsync(Guid receivingId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
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

    private class FakeBusinessClock : IBusinessClock
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
        public DateOnly GetJordanBusinessDate(DateTimeOffset? utcInstant = null)
            => DateOnly.FromDateTime((utcInstant ?? UtcNow).UtcDateTime);
    }

    private class FakeCurrentUserContext : ICurrentUserContext
    {
        public Guid? UserId { get; } = Guid.NewGuid();
    }

    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly DueDate = new(2026, 1, 5);

    private static RentPayment CreateScheduledInstallment(decimal amountDue = 500m)
    {
        return RentPayment.Create(
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
            dueDate: DueDate,
            createdAt: CreatedAt,
            createdBy: Guid.NewGuid());
    }

    private static RentPayment CreateUnallocatedReceipt(decimal amountDue = 200m)
    {
        return RentPayment.Create(
            companyId: Guid.NewGuid(),
            leaseContractId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            purpose: PaymentPurpose.UnallocatedReceipt,
            amountDue: amountDue,
            currency: "JOD",
            billingPeriodStart: null,
            billingPeriodEnd: null,
            dueDate: null,
            createdAt: CreatedAt,
            createdBy: Guid.NewGuid());
    }

    private static (MarkRentPaymentOverdueCommandHandler Handler,
        FakeRentPaymentRepository PaymentRepo,
        FakeBusinessClock Clock,
        FakeCurrentUserContext UserContext) CreateSut()
    {
        var paymentRepo = new FakeRentPaymentRepository();
        var clock = new FakeBusinessClock();
        var userContext = new FakeCurrentUserContext();
        var handler = new MarkRentPaymentOverdueCommandHandler(paymentRepo, clock, userContext);
        return (handler, paymentRepo, clock, userContext);
    }

    [Fact]
    public async Task Handle_PendingInstallmentPastDue_DerivesOverdueUnpaidStatus()
    {
        var (handler, paymentRepo, _, userContext) = CreateSut();
        var payment = CreateScheduledInstallment();
        await paymentRepo.AddAsync(payment);

        // AsOf well past the due date (2026-01-05).
        var command = new MarkRentPaymentOverdueCommand(
            payment.Id, new DateTimeOffset(2026, 2, 10, 0, 0, 0, TimeSpan.Zero));

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(DueDateStatus.OverdueUnpaid, payment.DueDateStatus);
        Assert.Equal(0m, payment.AmountPaid);
        Assert.Equal(userContext.UserId, payment.UpdatedBy);
    }

    [Fact]
    public async Task Handle_MissingPayment_ThrowsNotFoundException()
    {
        var (handler, _, _, _) = CreateSut();
        var missingId = Guid.NewGuid();

        var command = new MarkRentPaymentOverdueCommand(missingId, DateTimeOffset.UtcNow);

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Equal($"RentPayment with ID {missingId} was not found.", ex.Message);
    }

    [Fact]
    public async Task Handle_NonInstallmentPurpose_IsSilentNoOp()
    {
        var (handler, paymentRepo, _, _) = CreateSut();
        var receipt = CreateUnallocatedReceipt();
        await paymentRepo.AddAsync(receipt);

        var command = new MarkRentPaymentOverdueCommand(
            receipt.Id, new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(DueDateStatus.Pending, receipt.DueDateStatus);
        Assert.Equal(CreatedAt, receipt.UpdatedAt);
    }

    [Fact]
    public async Task Handle_AlreadyPaidInstallment_IsSilentNoOp()
    {
        var (handler, paymentRepo, _, _) = CreateSut();
        var payment = CreateScheduledInstallment(amountDue: 500m);
        var settledAt = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        payment.UpdateAllocationSync(500m, DueDateStatus.Paid, settledAt, Guid.NewGuid());
        await paymentRepo.AddAsync(payment);

        var command = new MarkRentPaymentOverdueCommand(
            payment.Id, new DateTimeOffset(2026, 2, 10, 0, 0, 0, TimeSpan.Zero));

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(DueDateStatus.Paid, payment.DueDateStatus);
        Assert.Equal(settledAt, payment.UpdatedAt);
    }

    [Fact]
    public async Task Handle_CancelledInstallment_IsSilentNoOp_CancelledIsSticky()
    {
        var (handler, paymentRepo, _, _) = CreateSut();
        var payment = CreateScheduledInstallment();
        var cancelledAt = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        payment.Cancel(cancelledAt, Guid.NewGuid());
        await paymentRepo.AddAsync(payment);

        var command = new MarkRentPaymentOverdueCommand(
            payment.Id, new DateTimeOffset(2026, 2, 10, 0, 0, 0, TimeSpan.Zero));

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(DueDateStatus.Cancelled, payment.DueDateStatus);
        Assert.Equal(cancelledAt, payment.UpdatedAt);
    }

    [Fact]
    public async Task Handle_DueDateNotYetPassed_LeavesPendingUntouched()
    {
        var (handler, paymentRepo, _, _) = CreateSut();
        var payment = CreateScheduledInstallment();
        await paymentRepo.AddAsync(payment);

        // Jordan business date == due date: DeriveStatus requires today > dueDate,
        // so the payment is not yet late and no mutation must occur.
        var command = new MarkRentPaymentOverdueCommand(
            payment.Id, new DateTimeOffset(2026, 1, 5, 12, 0, 0, TimeSpan.Zero));

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(DueDateStatus.Pending, payment.DueDateStatus);
        Assert.Equal(CreatedAt, payment.UpdatedAt);
        Assert.Equal(payment.CreatedBy, payment.UpdatedBy);
    }

    [Fact]
    public async Task Handle_NullAsOf_UsesBusinessClockUtcNow()
    {
        var (handler, paymentRepo, clock, _) = CreateSut();
        clock.UtcNow = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var payment = CreateScheduledInstallment();
        await paymentRepo.AddAsync(payment);

        var command = new MarkRentPaymentOverdueCommand(payment.Id, AsOf: null);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(DueDateStatus.OverdueUnpaid, payment.DueDateStatus);
    }
}
