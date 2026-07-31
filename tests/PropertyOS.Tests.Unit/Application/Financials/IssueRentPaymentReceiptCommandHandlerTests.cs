using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Commands.IssueRentPaymentReceipt;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials;

public class IssueRentPaymentReceiptCommandHandlerTests
{
    private class FakeRentPaymentRepository : IRentPaymentRepository
    {
        public List<RentPayment> Payments { get; } = new();
        public List<PaymentAllocation> Allocations { get; } = new();
        public List<RentPaymentReceipt> AddedReceipts { get; } = new();

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

        public Task AddReceiptAsync(RentPaymentReceipt receipt, CancellationToken cancellationToken = default)
        {
            AddedReceipts.Add(receipt);
            return Task.CompletedTask;
        }

        public Task AddAllocationAsync(PaymentAllocation allocation, CancellationToken cancellationToken = default)
        {
            Allocations.Add(allocation);
            return Task.CompletedTask;
        }

        public Task<List<PaymentAllocation>> GetAllocationsByObligationIdAsync(Guid obligationId, CancellationToken cancellationToken = default)
            => Task.FromResult(Allocations.Where(a => a.ObligationPaymentId == obligationId).ToList());

        public Task<List<PaymentAllocation>> GetAllocationsByReceivingIdAsync(Guid receivingId, CancellationToken cancellationToken = default)
            => Task.FromResult(Allocations.Where(a => a.ReceivingPaymentId == receivingId).ToList());

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
        public Task<List<ChequeDetailDto>> GetChequesAsync(ChequeStatus? status, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<ChequeDetailDto>> GetUpcomingChequesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<RentPaymentReceiptDto?> GetReceiptByRentPaymentIdAsync(Guid rentPaymentId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentReceiptDto>> GetReceiptsAsync(RentPaymentReceiptFilterOptions filter, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeCompanyReceiptSequenceRepository : ICompanyReceiptSequenceRepository
    {
        public long CurrentNumber { get; set; }

        public Task<CompanyReceiptSequence?> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAsync(CompanyReceiptSequence sequence, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> ReserveAndFormatNextReceiptNumberAsync(Guid companyId, CancellationToken cancellationToken = default)
        {
            CurrentNumber++;
            return Task.FromResult($"REC-{CurrentNumber:D5}");
        }

        public Task<long> ReserveNextReceiptNumberAsync(Guid companyId, CancellationToken cancellationToken = default)
        {
            CurrentNumber++;
            return Task.FromResult(CurrentNumber);
        }
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

    private static RentPayment CreateInstallment(Guid companyId, decimal amount)
    {
        return RentPayment.Create(
            companyId: companyId,
            leaseContractId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            purpose: PaymentPurpose.ScheduledInstallment,
            amountDue: amount,
            currency: "JOD",
            billingPeriodStart: new DateOnly(2026, 1, 1),
            billingPeriodEnd: new DateOnly(2026, 2, 1),
            dueDate: new DateOnly(2026, 1, 5),
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()
        );
    }

    private static (IssueRentPaymentReceiptCommandHandler Handler, FakeRentPaymentRepository PaymentRepo, FakeCompanyReceiptSequenceRepository SequenceRepo) CreateSut(Guid? companyId)
    {
        var paymentRepo = new FakeRentPaymentRepository();
        var sequenceRepo = new FakeCompanyReceiptSequenceRepository();
        var handler = new IssueRentPaymentReceiptCommandHandler(
            paymentRepo,
            sequenceRepo,
            new FakeTenantContext { CompanyId = companyId },
            new FakeCurrentUserContext());
        return (handler, paymentRepo, sequenceRepo);
    }

    [Fact]
    public async Task Handle_FullyPaidPayment_IssuesReceipt_AndReturnsFormattedNumber()
    {
        var companyId = Guid.NewGuid();
        var (handler, paymentRepo, _) = CreateSut(companyId);

        var payment = CreateInstallment(companyId, 1000m);
        payment.UpdateAllocationSync(1000m, DueDateStatus.Paid, DateTimeOffset.UtcNow, Guid.NewGuid());
        await paymentRepo.AddAsync(payment);

        var command = new IssueRentPaymentReceiptCommand(payment.Id);

        var receiptNumber = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("REC-00001", receiptNumber);
        Assert.Equal("REC-00001", payment.ReceiptNumber);

        // The receipt MUST be explicitly added (client-generated ID protocol).
        var receipt = Assert.Single(paymentRepo.AddedReceipts);
        Assert.Equal(payment.Id, receipt.RentPaymentId);
        Assert.Equal("REC-00001", receipt.ReceiptNumber);
    }

    [Fact]
    public async Task Handle_MissingPayment_ThrowsNotFoundException()
    {
        var (handler, _, _) = CreateSut(Guid.NewGuid());

        var command = new IssueRentPaymentReceiptCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CrossTenantPayment_ThrowsNotFoundException_WithSameMessageAsMissing()
    {
        var (handler, paymentRepo, _) = CreateSut(Guid.NewGuid());

        // Payment belongs to a different company than the tenant context.
        var payment = CreateInstallment(Guid.NewGuid(), 1000m);
        payment.UpdateAllocationSync(1000m, DueDateStatus.Paid, DateTimeOffset.UtcNow, Guid.NewGuid());
        await paymentRepo.AddAsync(payment);

        var command = new IssueRentPaymentReceiptCommand(payment.Id);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal($"RentPayment with ID {payment.Id} was not found.", ex.Message);
    }

    [Fact]
    public async Task Handle_MissingTenantContext_ThrowsInvalidOperationException()
    {
        var (handler, _, _) = CreateSut(null);

        var command = new IssueRentPaymentReceiptCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotFullyPaidPayment_ThrowsBusinessRuleException()
    {
        var companyId = Guid.NewGuid();
        var (handler, paymentRepo, _) = CreateSut(companyId);

        var payment = CreateInstallment(companyId, 1000m);
        payment.UpdateAllocationSync(400m, DueDateStatus.PartiallyPaid, DateTimeOffset.UtcNow, Guid.NewGuid());
        await paymentRepo.AddAsync(payment);

        var command = new IssueRentPaymentReceiptCommand(payment.Id);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("RECEIPT_ISSUE_INVALID_STATE", ex.Code);
        Assert.Empty(paymentRepo.AddedReceipts);
    }

    [Fact]
    public async Task Handle_ReceiptAlreadyIssued_ThrowsBusinessRuleException()
    {
        var companyId = Guid.NewGuid();
        var (handler, paymentRepo, _) = CreateSut(companyId);

        var payment = CreateInstallment(companyId, 1000m);
        payment.UpdateAllocationSync(1000m, DueDateStatus.Paid, DateTimeOffset.UtcNow, Guid.NewGuid());
        await paymentRepo.AddAsync(payment);

        var command = new IssueRentPaymentReceiptCommand(payment.Id);

        // First issuance succeeds; the second must be rejected by the domain invariant.
        await handler.Handle(command, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("RECEIPT_ISSUE_INVALID_STATE", ex.Code);
        Assert.Single(paymentRepo.AddedReceipts);
    }
}
