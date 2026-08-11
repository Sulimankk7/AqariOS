using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Commands.CancelRentPayment;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials;

public class CancelRentPaymentCommandHandlerTests
{
    private class FakeRentPaymentRepository : IRentPaymentRepository
    {
        public List<RentPayment> Payments { get; } = new();
        public List<PaymentAllocation> Allocations { get; } = new();
        public List<RentPaymentReceipt> Receipts { get; } = new();

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

        public Task AddAllocationAsync(PaymentAllocation allocation, CancellationToken cancellationToken = default)
        {
            Allocations.Add(allocation);
            return Task.CompletedTask;
        }

        public Task<List<PaymentAllocation>> GetAllocationsByObligationIdAsync(Guid obligationId, CancellationToken cancellationToken = default)
            => Task.FromResult(Allocations.Where(a => a.ObligationPaymentId == obligationId).ToList());

        public Task<List<PaymentAllocation>> GetAllocationsByReceivingIdAsync(Guid receivingId, CancellationToken cancellationToken = default)
            => Task.FromResult(Allocations.Where(a => a.ReceivingPaymentId == receivingId).ToList());

        public Task AddReceiptAsync(RentPaymentReceipt receipt, CancellationToken cancellationToken = default)
        {
            Receipts.Add(receipt);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<RentPayment> rentPayments, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<BillingPeriod>> GetScheduledInstallmentPeriodsAsync(Guid leaseContractId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<BillingPeriod>());

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
        public Task<PropertyOS.Application.Common.Models.KeysetPage<PropertyOS.Application.Financials.Queries.GetPendingPaymentVerifications.PaymentVerificationQueueItemDto>> GetPendingVerificationsAsync(Guid companyId, int pageSize, DateTimeOffset? lastSeenSubmittedAt, Guid? lastSeenId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<ChequeDetailDto>> GetChequesAsync(ChequeStatus? status, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<ChequeDetailDto>> GetUpcomingChequesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<RentPaymentReceiptDto?> GetReceiptByRentPaymentIdAsync(Guid rentPaymentId, Guid companyId, CancellationToken cancellationToken = default)
        {
            var payment = Payments.FirstOrDefault(p => p.Id == rentPaymentId && p.CompanyId == companyId);
            if (payment?.Receipt != null && payment.Receipt.DeletedAt == null)
            {
                return Task.FromResult<RentPaymentReceiptDto?>(new RentPaymentReceiptDto
                {
                    Id = payment.Receipt.Id,
                    CompanyId = payment.Receipt.CompanyId,
                    RentPaymentId = payment.Receipt.RentPaymentId,
                    ReceiptNumber = payment.Receipt.ReceiptNumber,
                    IssueDate = payment.Receipt.IssueDate,
                    IssuedBy = payment.Receipt.IssuedBy,
                    Amount = payment.Receipt.Amount,
                    Currency = payment.Receipt.Currency,
                    Notes = payment.Receipt.Notes
                });
            }

            var receipt = Receipts.FirstOrDefault(r => r.RentPaymentId == rentPaymentId && r.CompanyId == companyId && r.DeletedAt == null);
            if (receipt != null)
            {
                return Task.FromResult<RentPaymentReceiptDto?>(new RentPaymentReceiptDto
                {
                    Id = receipt.Id,
                    CompanyId = receipt.CompanyId,
                    RentPaymentId = receipt.RentPaymentId,
                    ReceiptNumber = receipt.ReceiptNumber,
                    IssueDate = receipt.IssueDate,
                    IssuedBy = receipt.IssuedBy,
                    Amount = receipt.Amount,
                    Currency = receipt.Currency,
                    Notes = receipt.Notes
                });
            }

            return Task.FromResult<RentPaymentReceiptDto?>(null);
        }

        public Task<List<RentPaymentReceiptDto>> GetReceiptsAsync(RentPaymentReceiptFilterOptions filter, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeTenantContext : ITenantContext
    {
        public Guid? CompanyId { get; set; }
        public bool IsPlatformAdmin => false;
    }

    private class FakeCurrentUserContext : ICurrentUserContext
    {
        public Guid? UserId { get; } = Guid.NewGuid();
    }

    private static RentPayment CreatePayment(Guid companyId, PaymentPurpose purpose, decimal amount)
    {
        return RentPayment.Create(
            companyId: companyId,
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
    }

    private static (CancelRentPaymentCommandHandler Handler, FakeRentPaymentRepository PaymentRepo) CreateSut(Guid? companyId)
    {
        var paymentRepo = new FakeRentPaymentRepository();
        var handler = new CancelRentPaymentCommandHandler(
            paymentRepo,
            new FakeTenantContext { CompanyId = companyId },
            new FakeCurrentUserContext());
        return (handler, paymentRepo);
    }

    [Fact]
    public async Task Handle_PaymentWithoutAllocations_CancelsPayment_AndAppendsReason()
    {
        var companyId = Guid.NewGuid();
        var (handler, paymentRepo) = CreateSut(companyId);

        var payment = CreatePayment(companyId, PaymentPurpose.UnallocatedReceipt, 200m);
        await paymentRepo.AddAsync(payment);

        var command = new CancelRentPaymentCommand(payment.Id, "Recorded by mistake");

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(DueDateStatus.Cancelled, payment.DueDateStatus);
        Assert.Equal("Cancelled: Recorded by mistake", payment.Notes);
    }

    [Fact]
    public async Task Handle_NoReason_CancelsPayment_WithoutTouchingNotes()
    {
        var companyId = Guid.NewGuid();
        var (handler, paymentRepo) = CreateSut(companyId);

        var payment = CreatePayment(companyId, PaymentPurpose.ScheduledInstallment, 500m);
        await paymentRepo.AddAsync(payment);

        var command = new CancelRentPaymentCommand(payment.Id);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(DueDateStatus.Cancelled, payment.DueDateStatus);
        Assert.Null(payment.Notes);
    }

    [Fact]
    public async Task Handle_MissingPayment_ThrowsNotFoundException()
    {
        var (handler, _) = CreateSut(Guid.NewGuid());

        var command = new CancelRentPaymentCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CrossTenantPayment_ThrowsNotFoundException_WithSameMessageAsMissing()
    {
        var (handler, paymentRepo) = CreateSut(Guid.NewGuid());

        // Payment belongs to a different company than the tenant context.
        var payment = CreatePayment(Guid.NewGuid(), PaymentPurpose.UnallocatedReceipt, 200m);
        await paymentRepo.AddAsync(payment);

        var command = new CancelRentPaymentCommand(payment.Id);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal($"RentPayment with ID {payment.Id} was not found.", ex.Message);
        Assert.NotEqual(DueDateStatus.Cancelled, payment.DueDateStatus);
    }

    [Fact]
    public async Task Handle_MissingTenantContext_ThrowsInvalidOperationException()
    {
        var (handler, _) = CreateSut(null);

        var command = new CancelRentPaymentCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ActiveAllocationAsObligation_ThrowsBusinessRuleException()
    {
        var companyId = Guid.NewGuid();
        var (handler, paymentRepo) = CreateSut(companyId);

        var installment = CreatePayment(companyId, PaymentPurpose.ScheduledInstallment, 500m);
        await paymentRepo.AddAsync(installment);

        var allocation = PaymentAllocation.Create(
            companyId: companyId,
            receivingPaymentId: Guid.NewGuid(),
            obligationPaymentId: installment.Id,
            allocatedAmount: 200m,
            allocationDate: new DateOnly(2026, 7, 1),
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid());
        await paymentRepo.AddAllocationAsync(allocation);

        var command = new CancelRentPaymentCommand(installment.Id);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("PAYMENT_CANCEL_HAS_ACTIVE_ALLOCATIONS", ex.Code);
        Assert.NotEqual(DueDateStatus.Cancelled, installment.DueDateStatus);
    }

    [Fact]
    public async Task Handle_ActiveAllocationAsReceiving_ThrowsBusinessRuleException()
    {
        var companyId = Guid.NewGuid();
        var (handler, paymentRepo) = CreateSut(companyId);

        var receipt = CreatePayment(companyId, PaymentPurpose.UnallocatedReceipt, 500m);
        await paymentRepo.AddAsync(receipt);

        var allocation = PaymentAllocation.Create(
            companyId: companyId,
            receivingPaymentId: receipt.Id,
            obligationPaymentId: Guid.NewGuid(),
            allocatedAmount: 200m,
            allocationDate: new DateOnly(2026, 7, 1),
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid());
        await paymentRepo.AddAllocationAsync(allocation);

        var command = new CancelRentPaymentCommand(receipt.Id);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("PAYMENT_CANCEL_HAS_ACTIVE_ALLOCATIONS", ex.Code);
    }

    [Fact]
    public async Task Handle_ReversedAllocationsOnly_CancelsPayment()
    {
        var companyId = Guid.NewGuid();
        var (handler, paymentRepo) = CreateSut(companyId);

        var receipt = CreatePayment(companyId, PaymentPurpose.UnallocatedReceipt, 500m);
        await paymentRepo.AddAsync(receipt);

        var allocation = PaymentAllocation.Create(
            companyId: companyId,
            receivingPaymentId: receipt.Id,
            obligationPaymentId: Guid.NewGuid(),
            allocatedAmount: 200m,
            allocationDate: new DateOnly(2026, 7, 1),
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid());
        allocation.Reverse("Posted in error", DateTimeOffset.UtcNow, Guid.NewGuid());
        await paymentRepo.AddAllocationAsync(allocation);

        var command = new CancelRentPaymentCommand(receipt.Id);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(DueDateStatus.Cancelled, receipt.DueDateStatus);
    }

    [Fact]
    public async Task Handle_AlreadyCancelledPayment_ThrowsBusinessRuleException_AndDoesNotMutateMetadataOrNotes()
    {
        var companyId = Guid.NewGuid();
        var (handler, paymentRepo) = CreateSut(companyId);

        var payment = CreatePayment(companyId, PaymentPurpose.UnallocatedReceipt, 200m);
        await paymentRepo.AddAsync(payment);

        // Cancel the first time
        var initialCommand = new CancelRentPaymentCommand(payment.Id, "Initial cancellation reason");
        await handler.Handle(initialCommand, CancellationToken.None);

        var originalUpdatedAt = payment.UpdatedAt;
        var originalUpdatedBy = payment.UpdatedBy;
        var originalNotes = payment.Notes;

        // Attempt to cancel a second time
        var secondCommand = new CancelRentPaymentCommand(payment.Id, "Second cancellation attempt");
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(secondCommand, CancellationToken.None));

        Assert.Equal("PAYMENT_ALREADY_CANCELLED", ex.Code);
        Assert.Equal(DueDateStatus.Cancelled, payment.DueDateStatus);
        Assert.Equal(originalUpdatedAt, payment.UpdatedAt);
        Assert.Equal(originalUpdatedBy, payment.UpdatedBy);
        Assert.Equal(originalNotes, payment.Notes);
    }

    [Fact]
    public async Task Handle_PaymentWithActiveIssuedReceipt_ThrowsBusinessRuleException_AndPreservesReceipt()
    {
        var companyId = Guid.NewGuid();
        var (handler, paymentRepo) = CreateSut(companyId);

        var payment = CreatePayment(companyId, PaymentPurpose.ScheduledInstallment, 500m);
        payment.UpdateAllocationSync(500m, DueDateStatus.Paid, DateTimeOffset.UtcNow, Guid.NewGuid());
        var issuedAt = DateTimeOffset.UtcNow;
        var issuedBy = Guid.NewGuid();
        payment.IssueReceipt("REC-99999", issuedAt, issuedBy);
        await paymentRepo.AddAsync(payment);

        var command = new CancelRentPaymentCommand(payment.Id);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));

        Assert.Equal("PAYMENT_CANCEL_HAS_RECEIPT", ex.Code);
        Assert.NotEqual(DueDateStatus.Cancelled, payment.DueDateStatus);
        Assert.NotNull(payment.Receipt);
        Assert.Equal("REC-99999", payment.Receipt.ReceiptNumber);
        Assert.Null(payment.Receipt.DeletedAt);
    }
}
