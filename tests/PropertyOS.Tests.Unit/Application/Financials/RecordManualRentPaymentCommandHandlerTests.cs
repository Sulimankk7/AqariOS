using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Commands.RecordManualRentPayment;
using PropertyOS.Application.Financials.Commands.RecordPaymentAllocation;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetLeaseContractById;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials;

public class RecordManualRentPaymentCommandHandlerTests
{
    private class FakeLeaseContractRepository : ILeaseContractRepository
    {
        public Dictionary<Guid, LeaseContract> Contracts { get; } = new();

        public Task<LeaseContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Contracts.TryGetValue(id, out var c);
            return Task.FromResult(c);
        }

        public Task AddAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default)
        {
            Contracts[leaseContract.Id] = leaseContract;
            return Task.CompletedTask;
        }

        public Task<LeaseContract?> GetWithHistoryByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, Guid excludeContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, Guid excludeContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasSignedContractDocumentAsync(Guid leaseContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasSuccessorContractAsync(Guid priorContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddStatusHistoryAsync(ContractStatusHistory statusHistory, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddTerminationAsync(ContractTermination termination, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Guid>> GetActiveContractIdsExpiringOnOrBeforeAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Guid>> GetActiveContractIdsAsync(int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LeaseContractDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByApartmentIdAsync(Guid apartmentId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByTenantIdAsync(Guid tenantId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> SearchContractsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetExpiringLeasesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeRentPaymentRepository : IRentPaymentRepository
    {
        public List<RentPayment> Payments { get; } = new();
        public List<ChequeDetails> Cheques { get; } = new();
        public List<PaymentAllocation> Allocations { get; } = new();

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

        public Task AddChequeAsync(ChequeDetails cheque, CancellationToken cancellationToken = default)
        {
            Cheques.Add(cheque);
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

        public Task AddReceiptAsync(RentPaymentReceipt receipt, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AddRangeAsync(IEnumerable<RentPayment> rentPayments, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<List<BillingPeriod>> GetScheduledInstallmentPeriodsAsync(Guid leaseContractId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<BillingPeriod>());

        public Task<bool> HasScheduledInstallmentAsync(Guid leaseContractId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Guid>> GetOverdueCandidateIdsAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ChequeDetails?> LoadChequeAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PaymentAllocation?> LoadAllocationAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<RentPaymentDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> GetPaymentsForLeaseAsync(Guid leaseContractId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> GetPaymentsForTenantAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> SearchPaymentsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentDto>> GetOutstandingPaymentsAsync(Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<ChequeDetailDto>> GetChequesByStatusAsync(ChequeStatus status, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<ChequeDetailDto>> GetUpcomingChequesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<RentPaymentReceiptDto?> GetReceiptByRentPaymentIdAsync(Guid rentPaymentId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
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

    private class FakeSender : ISender
    {
        public List<object> SentRequests { get; } = new();

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            return Task.FromResult((TResponse)(object)MediatR.Unit.Value);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            SentRequests.Add(request);
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            return Task.FromResult<object?>(null);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private static LeaseContract CreateContract(Guid companyId)
    {
        return LeaseContract.Create(
            companyId: companyId,
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            contractNumber: "LC-2026-TEST",
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2027, 1, 1),
            monthlyRentAmount: 500m,
            paymentFrequency: PaymentFrequency.Monthly,
            paymentDueDay: 5,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid(),
            status: ContractStatus.Active
        );
    }

    private static (RecordManualRentPaymentCommandHandler Handler, FakeLeaseContractRepository ContractRepo, FakeRentPaymentRepository PaymentRepo, FakeTenantContext TenantContext, FakeSender Sender) CreateSut(Guid? companyId)
    {
        var contractRepo = new FakeLeaseContractRepository();
        var paymentRepo = new FakeRentPaymentRepository();
        var tenantContext = new FakeTenantContext { CompanyId = companyId };
        var sender = new FakeSender();
        var handler = new RecordManualRentPaymentCommandHandler(
            contractRepo, paymentRepo, tenantContext, new FakeCurrentUserContext(), sender);
        return (handler, contractRepo, paymentRepo, tenantContext, sender);
    }

    [Fact]
    public async Task Handle_CashPayment_CreatesUnallocatedReceipt_AndReturnsItsId()
    {
        var companyId = Guid.NewGuid();
        var (handler, contractRepo, paymentRepo, _, sender) = CreateSut(companyId);

        var contract = CreateContract(companyId);
        await contractRepo.AddAsync(contract);

        var command = new RecordManualRentPaymentCommand(
            LeaseContractId: contract.Id,
            Amount: 350m,
            Method: PaymentMethod.Cash,
            PaymentReferenceNumber: "CASH-001",
            Notes: "Walk-in payment"
        );

        var paymentId = await handler.Handle(command, CancellationToken.None);

        var payment = Assert.Single(paymentRepo.Payments);
        Assert.Equal(paymentId, payment.Id);
        Assert.Equal(PaymentPurpose.UnallocatedReceipt, payment.PaymentPurpose);
        Assert.Equal(350m, payment.AmountDue);
        Assert.Equal(contract.Currency, payment.Currency);
        Assert.Equal(companyId, payment.CompanyId);
        Assert.Equal(contract.Id, payment.LeaseContractId);
        Assert.Equal(contract.TenantId, payment.TenantId);
        Assert.Equal(contract.BuildingId, payment.BuildingId);
        Assert.Equal(contract.ApartmentId, payment.ApartmentId);
        Assert.Equal(PaymentMethod.Cash, payment.PaymentMethod);
        Assert.Equal("CASH-001", payment.PaymentReferenceNumber);
        Assert.Equal("Walk-in payment", payment.Notes);
        Assert.Null(payment.BillingPeriodStart);
        Assert.Null(payment.BillingPeriodEnd);
        Assert.Null(payment.DueDate);

        Assert.Empty(paymentRepo.Cheques);
        Assert.Empty(sender.SentRequests);
    }

    [Fact]
    public async Task Handle_MissingContract_ThrowsNotFoundException()
    {
        var (handler, _, _, _, _) = CreateSut(Guid.NewGuid());

        var command = new RecordManualRentPaymentCommand(
            LeaseContractId: Guid.NewGuid(),
            Amount: 100m,
            Method: PaymentMethod.Cash
        );

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CrossTenantContract_ThrowsNotFoundException_WithSameMessageAsMissing()
    {
        var (handler, contractRepo, _, _, _) = CreateSut(Guid.NewGuid());

        // Contract belongs to a different company than the tenant context.
        var contract = CreateContract(Guid.NewGuid());
        await contractRepo.AddAsync(contract);

        var command = new RecordManualRentPaymentCommand(
            LeaseContractId: contract.Id,
            Amount: 100m,
            Method: PaymentMethod.Cash
        );

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal($"LeaseContract with ID {contract.Id} was not found.", ex.Message);
    }

    [Fact]
    public async Task Handle_MissingTenantContext_ThrowsInvalidOperationException()
    {
        var (handler, _, _, _, _) = CreateSut(null);

        var command = new RecordManualRentPaymentCommand(
            LeaseContractId: Guid.NewGuid(),
            Amount: 100m,
            Method: PaymentMethod.Cash
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EfawateercomMethod_ThrowsBusinessRuleException()
    {
        var companyId = Guid.NewGuid();
        var (handler, contractRepo, _, _, _) = CreateSut(companyId);

        var contract = CreateContract(companyId);
        await contractRepo.AddAsync(contract);

        var command = new RecordManualRentPaymentCommand(
            LeaseContractId: contract.Id,
            Amount: 100m,
            Method: PaymentMethod.Efawateercom
        );

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("MANUAL_PAYMENT_METHOD_INVALID", ex.Code);
    }

    [Fact]
    public async Task Handle_ChequePayment_AddsChequeDetails()
    {
        var companyId = Guid.NewGuid();
        var (handler, contractRepo, paymentRepo, _, _) = CreateSut(companyId);

        var contract = CreateContract(companyId);
        await contractRepo.AddAsync(contract);

        var command = new RecordManualRentPaymentCommand(
            LeaseContractId: contract.Id,
            Amount: 1500m,
            Method: PaymentMethod.Cheque,
            Cheque: new ManualChequeDetails(
                ChequeNumber: "CHQ-778899",
                BankName: "Bank of Jordan",
                BankBranch: "Abdali",
                IssueDate: new DateOnly(2026, 7, 1),
                DueDate: new DateOnly(2026, 8, 1),
                ReceivedDate: new DateOnly(2026, 7, 2)
            )
        );

        var paymentId = await handler.Handle(command, CancellationToken.None);

        var cheque = Assert.Single(paymentRepo.Cheques);
        Assert.Equal(paymentId, cheque.RentPaymentId);
        Assert.Equal(companyId, cheque.CompanyId);
        Assert.Equal(contract.Id, cheque.LeaseContractId);
        Assert.Equal(contract.TenantId, cheque.TenantId);
        Assert.Equal("CHQ-778899", cheque.ChequeNumber);
        Assert.Equal("Bank of Jordan", cheque.BankName);
        Assert.Equal("Abdali", cheque.BankBranch);
        Assert.Equal(1500m, cheque.Amount);
        Assert.Equal(contract.Currency, cheque.Currency);
        Assert.Equal(ChequeStatus.Received, cheque.Status);

        var payment = Assert.Single(paymentRepo.Payments);
        Assert.Equal(PaymentMethod.Cheque, payment.PaymentMethod);
    }

    [Fact]
    public async Task Handle_ChequeMethodWithoutChequeBlock_ThrowsBusinessRuleException()
    {
        var companyId = Guid.NewGuid();
        var (handler, contractRepo, _, _, _) = CreateSut(companyId);

        var contract = CreateContract(companyId);
        await contractRepo.AddAsync(contract);

        var command = new RecordManualRentPaymentCommand(
            LeaseContractId: contract.Id,
            Amount: 100m,
            Method: PaymentMethod.Cheque,
            Cheque: null
        );

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("MANUAL_PAYMENT_CHEQUE_DETAILS_REQUIRED", ex.Code);
    }

    [Fact]
    public async Task Handle_WithAllocations_DispatchesNestedAllocationCommand_WithReceivingPaymentId()
    {
        var companyId = Guid.NewGuid();
        var (handler, contractRepo, _, _, sender) = CreateSut(companyId);

        var contract = CreateContract(companyId);
        await contractRepo.AddAsync(contract);

        var obligationId = Guid.NewGuid();
        var allocations = new List<AllocationDetail> { new(obligationId, 250m) };

        var command = new RecordManualRentPaymentCommand(
            LeaseContractId: contract.Id,
            Amount: 250m,
            Method: PaymentMethod.BankTransfer,
            PaymentReferenceNumber: "TRF-42",
            Allocations: allocations
        );

        var paymentId = await handler.Handle(command, CancellationToken.None);

        var dispatched = Assert.Single(sender.SentRequests);
        var allocationCommand = Assert.IsType<RecordPaymentAllocationCommand>(dispatched);
        Assert.Equal(paymentId, allocationCommand.ReceivingPaymentId);
        var detail = Assert.Single(allocationCommand.Allocations);
        Assert.Equal(obligationId, detail.ObligationPaymentId);
        Assert.Equal(250m, detail.Amount);
    }

    [Fact]
    public async Task Handle_EmptyAllocationsList_DoesNotDispatchNestedCommand()
    {
        var companyId = Guid.NewGuid();
        var (handler, contractRepo, _, _, sender) = CreateSut(companyId);

        var contract = CreateContract(companyId);
        await contractRepo.AddAsync(contract);

        var command = new RecordManualRentPaymentCommand(
            LeaseContractId: contract.Id,
            Amount: 100m,
            Method: PaymentMethod.BankTransfer,
            Allocations: new List<AllocationDetail>()
        );

        await handler.Handle(command, CancellationToken.None);

        Assert.Empty(sender.SentRequests);
    }

    [Fact]
    public void Validator_ChequeMethodWithoutChequeBlock_Fails()
    {
        var validator = new RecordManualRentPaymentCommandValidator();

        var command = new RecordManualRentPaymentCommand(
            LeaseContractId: Guid.NewGuid(),
            Amount: 100m,
            Method: PaymentMethod.Cheque
        );

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RecordManualRentPaymentCommand.Cheque));
    }

    [Fact]
    public void Validator_NonPositiveAmount_Fails()
    {
        var validator = new RecordManualRentPaymentCommandValidator();

        var command = new RecordManualRentPaymentCommand(
            LeaseContractId: Guid.NewGuid(),
            Amount: 0m,
            Method: PaymentMethod.Cash
        );

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RecordManualRentPaymentCommand.Amount));
    }

    [Fact]
    public void Validator_NonPositiveAllocationAmount_Fails()
    {
        var validator = new RecordManualRentPaymentCommandValidator();

        var command = new RecordManualRentPaymentCommand(
            LeaseContractId: Guid.NewGuid(),
            Amount: 100m,
            Method: PaymentMethod.Cash,
            Allocations: new List<AllocationDetail> { new(Guid.NewGuid(), 0m) }
        );

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
