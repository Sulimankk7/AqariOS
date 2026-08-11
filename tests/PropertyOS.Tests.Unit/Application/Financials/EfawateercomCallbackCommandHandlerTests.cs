using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials.Commands.ReceiveEfawateercomCallback;
using PropertyOS.Application.Financials.Commands.RecordPaymentAllocation;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials;

public class EfawateercomCallbackCommandHandlerTests
{
    private class FakeEfawateercomTransactionRepository : IEfawateercomTransactionRepository
    {
        public List<EfawateercomTransaction> Transactions { get; } = new();

        public Task<EfawateercomTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Transactions.FirstOrDefault(t => t.Id == id));
        }

        public Task<EfawateercomTransaction?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return GetByIdAsync(id, cancellationToken);
        }

        public Task<EfawateercomTransaction?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Transactions.FirstOrDefault(x => x.ExternalTransactionId == externalId));
        }

        public Task<EfawateercomTransaction?> GetByExternalIdForUpdateAsync(string externalId, CancellationToken cancellationToken = default)
        {
            return GetByExternalIdAsync(externalId, cancellationToken);
        }

        public Task AddAsync(EfawateercomTransaction transaction, CancellationToken cancellationToken = default)
        {
            Transactions.Add(transaction);
            return Task.CompletedTask;
        }

        public Task<List<Guid>> GetStaleNonTerminalTransactionIdsAsync(DateTimeOffset olderThan, int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<EfawateercomTransactionDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<EfawateercomTransactionDto>> GetTransactionsAsync(EfawateercomTransactionFilterOptions filter, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeRentPaymentRepository : IRentPaymentRepository
    {
        public List<RentPayment> Payments { get; } = new();
        public List<PaymentAllocation> Allocations { get; } = new();

        public Task<RentPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Payments.FirstOrDefault(p => p.Id == id));
        }

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
            return Task.FromResult(Allocations.Where(a => a.ObligationPaymentId == obligationId).ToList());
        }

        public Task<List<PaymentAllocation>> GetAllocationsByReceivingIdAsync(Guid receivingId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Allocations.Where(a => a.ReceivingPaymentId == receivingId).ToList());
        }

        public Task<RentPaymentDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> GetPaymentsForLeaseAsync(Guid leaseContractId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> GetPaymentsForTenantAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> SearchPaymentsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> GetOutstandingPaymentsAsync(Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PropertyOS.Application.Common.Models.KeysetPage<PropertyOS.Application.Financials.Queries.GetPendingPaymentVerifications.PaymentVerificationQueueItemDto>> GetPendingVerificationsAsync(Guid companyId, int pageSize, DateTimeOffset? lastSeenSubmittedAt, Guid? lastSeenId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<ChequeDetailDto>> GetChequesAsync(ChequeStatus? status, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<ChequeDetailDto>> GetUpcomingChequesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<RentPaymentReceiptDto?> GetReceiptByRentPaymentIdAsync(Guid rentPaymentId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentReceiptDto>> GetReceiptsAsync(RentPaymentReceiptFilterOptions filter, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeCompanyReceiptSequenceRepository : ICompanyReceiptSequenceRepository
    {
        public long CurrentNumber { get; set; } = 0;

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

    private class FakeCurrentUserContext : ICurrentUserContext
    {
        public Guid? UserId { get; } = Guid.NewGuid();
    }

    private class FakeMediator : IMediator
    {
        public List<object> DispatchedRequests { get; } = new();

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            DispatchedRequests.Add(request);
            return Task.FromResult((TResponse)(object)MediatR.Unit.Value);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            DispatchedRequests.Add(request);
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            DispatchedRequests.Add(request);
            return Task.FromResult<object?>(null);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
    }

    [Fact]
    public async Task Handle_FirstCallback_Succeeds_CreatesPayment_DispatchesAllocation_And_IssuesReceipt()
    {
        // Arrange
        var txRepo = new FakeEfawateercomTransactionRepository();
        var rentRepo = new FakeRentPaymentRepository();
        var seqRepo = new FakeCompanyReceiptSequenceRepository();
        var userCtx = new FakeCurrentUserContext();
        var mediator = new FakeMediator();

        var handler = new ReceiveEfawateercomCallbackCommandHandler(txRepo, rentRepo, seqRepo, userCtx, mediator);

        var companyId = Guid.NewGuid();

        // Create target installment payment
        var installment = RentPayment.Create(
            companyId: companyId,
            leaseContractId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            purpose: PaymentPurpose.ScheduledInstallment,
            amountDue: 500.00m,
            currency: "JOD",
            billingPeriodStart: new DateOnly(2026, 1, 1),
            billingPeriodEnd: new DateOnly(2026, 2, 1),
            dueDate: new DateOnly(2026, 1, 15),
            createdAt: DateTimeOffset.UtcNow,
            createdBy: userCtx.UserId
        );
        var rentPaymentId = Guid.NewGuid();
        typeof(RentPayment).GetProperty("Id")?.SetValue(installment, rentPaymentId);
        await rentRepo.AddAsync(installment);

        // Create transaction record in pending state
        var tx = EfawateercomTransaction.Create(
            companyId: companyId,
            rentPaymentId: installment.Id,
            externalTransactionId: "EXT-TX-9999",
            requestTime: DateTimeOffset.UtcNow.AddMinutes(-5),
            amount: 500.00m,
            currency: "JOD"
        );
        await txRepo.AddAsync(tx);

        var command = new ReceiveEfawateercomCallbackCommand(
            ExternalTransactionId: "EXT-TX-9999",
            Status: EfawateercomStatus.Success,
            ResponseTime: DateTimeOffset.UtcNow,
            ResponseCode: "000",
            ResponseMessage: "SUCCESS",
            RawResponse: "{\"status\":\"OK\"}"
        );

        // Act
        // Make the installment look fully paid to trigger IssueReceipt during callback processing (simulating the allocation success)
        installment.UpdateAllocationSync(500.00m, DueDateStatus.Paid, DateTimeOffset.UtcNow, userCtx.UserId);

        await handler.Handle(command, CancellationToken.None);

        // Assert
        // Verify high-level business outcomes: status transitions, payment record creations, and receipt status updates
        Assert.Equal(EfawateercomStatus.Success, tx.TransactionStatus);
        Assert.Equal("000", tx.ResponseCode);

        // Check created received funds payment (UnallocatedReceipt)
        var unallocatedPayment = rentRepo.Payments.FirstOrDefault(p => p.PaymentPurpose == PaymentPurpose.UnallocatedReceipt);
        Assert.NotNull(unallocatedPayment);
        Assert.Equal(companyId, unallocatedPayment.CompanyId);
        Assert.Equal(500.00m, unallocatedPayment.AmountDue);
        Assert.Equal(PaymentMethod.Efawateercom, unallocatedPayment.PaymentMethod);
        Assert.Equal("EXT-TX-9999", unallocatedPayment.PaymentReferenceNumber);

        // Check allocation command dispatched
        Assert.Single(mediator.DispatchedRequests);
        var allocationCmd = mediator.DispatchedRequests.First() as RecordPaymentAllocationCommand;
        Assert.NotNull(allocationCmd);
        Assert.Equal(unallocatedPayment.Id, allocationCmd.ReceivingPaymentId);
        Assert.Single(allocationCmd.Allocations);
        Assert.Equal(installment.Id, allocationCmd.Allocations[0].ObligationPaymentId);
        Assert.Equal(500.00m, allocationCmd.Allocations[0].Amount);

        // Check receipt was issued
        Assert.NotNull(installment.Receipt);
        Assert.Equal("REC-00001", installment.Receipt.ReceiptNumber);
        Assert.Equal(500.00m, installment.Receipt.Amount);
    }

    [Fact]
    public async Task Handle_DuplicateWebhookDelivery_IsSafeIdempotentNoOp()
    {
        // Arrange
        var txRepo = new FakeEfawateercomTransactionRepository();
        var rentRepo = new FakeRentPaymentRepository();
        var seqRepo = new FakeCompanyReceiptSequenceRepository();
        var userCtx = new FakeCurrentUserContext();
        var mediator = new FakeMediator();

        var handler = new ReceiveEfawateercomCallbackCommandHandler(txRepo, rentRepo, seqRepo, userCtx, mediator);

        var companyId = Guid.NewGuid();

        var installment = RentPayment.Create(
            companyId: companyId,
            leaseContractId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            purpose: PaymentPurpose.ScheduledInstallment,
            amountDue: 500.00m,
            currency: "JOD",
            billingPeriodStart: new DateOnly(2026, 1, 1),
            billingPeriodEnd: new DateOnly(2026, 2, 1),
            dueDate: new DateOnly(2026, 1, 15),
            createdAt: DateTimeOffset.UtcNow,
            createdBy: userCtx.UserId
        );
        var rentPaymentId = Guid.NewGuid();
        typeof(RentPayment).GetProperty("Id")?.SetValue(installment, rentPaymentId);
        await rentRepo.AddAsync(installment);

        // Transaction is already marked Success in the DB (simulating prior webhook completion)
        var tx = EfawateercomTransaction.Create(
            companyId: companyId,
            rentPaymentId: installment.Id,
            externalTransactionId: "EXT-TX-9999",
            requestTime: DateTimeOffset.UtcNow.AddMinutes(-5),
            amount: 500.00m,
            currency: "JOD"
        );
        tx.UpdateStatus(EfawateercomStatus.Success, DateTimeOffset.UtcNow, "000", "SUCCESS", "{}", DateTimeOffset.UtcNow, userCtx.UserId);
        await txRepo.AddAsync(tx);

        // Pre-create the unallocated receipt payment to ensure we can verify it is not duplicated
        var existingUnallocated = RentPayment.Create(
            companyId: companyId,
            leaseContractId: installment.LeaseContractId,
            tenantId: installment.TenantId,
            buildingId: installment.BuildingId,
            apartmentId: installment.ApartmentId,
            purpose: PaymentPurpose.UnallocatedReceipt,
            amountDue: 500.00m,
            currency: "JOD",
            billingPeriodStart: null,
            billingPeriodEnd: null,
            dueDate: null,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: userCtx.UserId
        );
        await rentRepo.AddAsync(existingUnallocated);

        var command = new ReceiveEfawateercomCallbackCommand(
            ExternalTransactionId: "EXT-TX-9999",
            Status: EfawateercomStatus.Success,
            ResponseTime: DateTimeOffset.UtcNow,
            ResponseCode: "000",
            ResponseMessage: "SUCCESS",
            RawResponse: "{\"status\":\"OK\"}"
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        // Confirm no new payments were added beyond the pre-existing ones
        Assert.Equal(2, rentRepo.Payments.Count); // 1 installment + 1 pre-existing unallocated payment

        // Confirm no allocations were dispatched
        Assert.Empty(mediator.DispatchedRequests);

        // Verify sequence generation was not touched
        Assert.Equal(0, seqRepo.CurrentNumber);
    }
}
