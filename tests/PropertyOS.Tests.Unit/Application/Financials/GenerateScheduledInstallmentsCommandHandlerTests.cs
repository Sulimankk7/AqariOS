using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials.Commands.GenerateScheduledInstallments;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetLeaseContractById;
using PropertyOS.Application.Financials;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials;

public class GenerateScheduledInstallmentsCommandHandlerTests
{
    private class FakeLeaseContractRepository : ILeaseContractRepository
    {
        public Dictionary<Guid, LeaseContract> Contracts { get; } = new();

        public Task<LeaseContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (Contracts.TryGetValue(id, out var c)) return Task.FromResult<LeaseContract?>(c);
            return Task.FromResult<LeaseContract?>(null);
        }

        public Task AddAsync(LeaseContract leaseContract, CancellationToken cancellationToken = default)
        {
            Contracts[leaseContract.Id] = leaseContract;
            return Task.CompletedTask;
        }

        public Task AddStatusHistoryAsync(ContractStatusHistory statusHistory, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveContractForApartmentAsync(Guid apartmentId, Guid excludeContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LeaseContract?> GetWithHistoryByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasOverlappingNonTerminalContractAsync(Guid apartmentId, DateTime startDate, DateTime endDate, Guid excludeContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasSignedContractDocumentAsync(Guid leaseContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasDocumentAsync(Guid leaseContractId, Guid fileId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<ContractDocument?> GetDocumentByIdAsync(Guid leaseContractId, Guid documentId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<ContractDocument?>(null);
        public Task<bool> HasSuccessorContractAsync(Guid priorContractId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddDocumentAsync(ContractDocument document, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddTerminationAsync(ContractTermination termination, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Guid>> GetActiveContractIdsExpiringOnOrBeforeAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Guid>());
        public Task<List<Guid>> GetActiveContractIdsAsync(int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => Task.FromResult(new List<Guid>());
        public Task<LeaseContractDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByApartmentIdAsync(Guid apartmentId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetHistoryByTenantIdAsync(Guid tenantId, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> SearchContractsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<LeaseContractDto>> GetExpiringLeasesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeRentPaymentRepository : IRentPaymentRepository
    {
        public List<RentPayment> Payments { get; } = new();

        public Task<RentPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var p = Payments.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(p);
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

        public Task AddRangeAsync(IEnumerable<RentPayment> rentPayments, CancellationToken cancellationToken = default)
        {
            Payments.AddRange(rentPayments);
            return Task.CompletedTask;
        }

            public Task<List<BillingPeriod>> GetScheduledInstallmentPeriodsAsync(Guid leaseContractId, CancellationToken cancellationToken = default)
            => Task.FromResult(Payments
                .Where(p => p.LeaseContractId == leaseContractId
                            && p.PaymentPurpose == PaymentPurpose.ScheduledInstallment
                            && p.BillingPeriodStart != null && p.BillingPeriodEnd != null
                            && p.DeletedAt == null)
                .Select(p => new BillingPeriod(p.BillingPeriodStart!.Value, p.BillingPeriodEnd!.Value))
                .ToList());

        public Task<bool> HasScheduledInstallmentAsync(Guid leaseContractId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
        {
            var exists = Payments.Any(p =>
                p.LeaseContractId == leaseContractId &&
                p.PaymentPurpose == PaymentPurpose.ScheduledInstallment &&
                p.BillingPeriodStart == start &&
                p.BillingPeriodEnd == end);
            return Task.FromResult(exists);
        }

        public Task<List<Guid>> GetOverdueCandidateIdsAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ChequeDetails?> LoadChequeAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddChequeAsync(ChequeDetails cheque, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PaymentAllocation?> LoadAllocationAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAllocationAsync(PaymentAllocation allocation, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<PaymentAllocation>> GetAllocationsByObligationIdAsync(Guid obligationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<PaymentAllocation>> GetAllocationsByReceivingIdAsync(Guid receivingId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

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

    private class FakeCurrentUserContext : ICurrentUserContext
    {
        public Guid? UserId { get; } = Guid.NewGuid();
    }

    private LeaseContract CreateActiveContract(PaymentFrequency frequency, DateOnly start, DateOnly end, decimal monthlyRent)
    {
        var contractId = Guid.NewGuid();
        var contract = LeaseContract.Create(
            companyId: Guid.NewGuid(),
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            contractNumber: "LC-2026-TEST",
            startDate: start,
            endDate: end,
            securityDepositAmount: 500,
            paymentFrequency: frequency,
            paymentDueDay: 5,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid(),
            priorContractId: null,
            legalRegime: LegalRegime.Standard,
            tenantType: TenantType.Personal,
            monthlyRentAmount: monthlyRent,
            status: ContractStatus.Active
        );

        // Reflection to set ID since it's private set
        var idProp = typeof(LeaseContract).GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        idProp?.SetValue(contract, contractId);

        return contract;
    }

    [Fact]
    public async Task Handle_MissingContract_ThrowsNotFoundException()
    {
        var contractRepo = new FakeLeaseContractRepository();
        var paymentRepo = new FakeRentPaymentRepository();
        var handler = new GenerateScheduledInstallmentsCommandHandler(contractRepo, paymentRepo, new FakeCurrentUserContext());

        var command = new GenerateScheduledInstallmentsCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InactiveContract_ThrowsBusinessRuleException()
    {
        var contractRepo = new FakeLeaseContractRepository();
        var paymentRepo = new FakeRentPaymentRepository();
        var handler = new GenerateScheduledInstallmentsCommandHandler(contractRepo, paymentRepo, new FakeCurrentUserContext());

        var contract = CreateActiveContract(PaymentFrequency.Monthly, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), 400);
        // Change status to Draft
        var statusProp = typeof(LeaseContract).GetProperty("Status", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        statusProp?.SetValue(contract, ContractStatus.Draft);

        await contractRepo.AddAsync(contract);

        var command = new GenerateScheduledInstallmentsCommand(contract.Id);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("INSTALLMENTS_CONTRACT_NOT_ACTIVE", ex.Code);
    }

    [Fact]
    public async Task Handle_GenerateMonthlyInstallments_Creates12Payments()
    {
        var contractRepo = new FakeLeaseContractRepository();
        var paymentRepo = new FakeRentPaymentRepository();
        var handler = new GenerateScheduledInstallmentsCommandHandler(contractRepo, paymentRepo, new FakeCurrentUserContext());

        var contract = CreateActiveContract(PaymentFrequency.Monthly, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), 500m);
        await contractRepo.AddAsync(contract);

        var command = new GenerateScheduledInstallmentsCommand(contract.Id);
        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(12, paymentRepo.Payments.Count);
        Assert.All(paymentRepo.Payments, p => Assert.Equal(500m, p.AmountDue));
        Assert.All(paymentRepo.Payments, p => Assert.Equal(PaymentPurpose.ScheduledInstallment, p.PaymentPurpose));
        Assert.Equal(new DateOnly(2026, 1, 1), paymentRepo.Payments.First().BillingPeriodStart);
        Assert.Equal(new DateOnly(2026, 2, 1), paymentRepo.Payments.First().BillingPeriodEnd);
        Assert.Equal(new DateOnly(2026, 12, 1), paymentRepo.Payments.Last().BillingPeriodStart);
        Assert.Equal(new DateOnly(2027, 1, 1), paymentRepo.Payments.Last().BillingPeriodEnd);
    }

    [Fact]
    public async Task Handle_DuplicateScheduledPeriod_DoesNotCreateDuplicates()
    {
        var contractRepo = new FakeLeaseContractRepository();
        var paymentRepo = new FakeRentPaymentRepository();
        var handler = new GenerateScheduledInstallmentsCommandHandler(contractRepo, paymentRepo, new FakeCurrentUserContext());

        var contract = CreateActiveContract(PaymentFrequency.Monthly, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), 500m);
        await contractRepo.AddAsync(contract);

        // Pre-create the first installment
        var firstPayment = RentPayment.Create(
            companyId: contract.CompanyId,
            leaseContractId: contract.Id,
            tenantId: contract.TenantId,
            buildingId: contract.BuildingId,
            apartmentId: contract.ApartmentId,
            purpose: PaymentPurpose.ScheduledInstallment,
            amountDue: 500m,
            currency: "JOD",
            billingPeriodStart: new DateOnly(2026, 1, 1),
            billingPeriodEnd: new DateOnly(2026, 2, 1),
            dueDate: new DateOnly(2026, 1, 5),
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()
        );
        await paymentRepo.AddAsync(firstPayment);

        var command = new GenerateScheduledInstallmentsCommand(contract.Id);
        await handler.Handle(command, CancellationToken.None);

        // Total payments should be 12 (1 pre-existing, 11 newly generated)
        Assert.Equal(12, paymentRepo.Payments.Count);
    }

    [Fact]
    public async Task Handle_TruncatedPeriod_ProRatesAmountCorrectly()
    {
        var contractRepo = new FakeLeaseContractRepository();
        var paymentRepo = new FakeRentPaymentRepository();
        var handler = new GenerateScheduledInstallmentsCommandHandler(contractRepo, paymentRepo, new FakeCurrentUserContext());

        // Contract is 15 months (Jan 2026 - March 2027), payment frequency is Annual (12 months)
        var contract = CreateActiveContract(PaymentFrequency.Annual, new DateOnly(2026, 1, 1), new DateOnly(2027, 3, 31), 1000m);
        await contractRepo.AddAsync(contract);

        var command = new GenerateScheduledInstallmentsCommand(contract.Id);
        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(2, paymentRepo.Payments.Count);

        // First installment is a full 12 months: 1000 * 12 = 12000
        var first = paymentRepo.Payments[0];
        Assert.Equal(new DateOnly(2026, 1, 1), first.BillingPeriodStart);
        Assert.Equal(new DateOnly(2027, 1, 1), first.BillingPeriodEnd);
        Assert.Equal(12000m, first.AmountDue);

        // Second installment is truncated (3 months: Jan, Feb, Mar 2027)
        // Expected end would be 2028-01-01. Actual end is 2027-04-01.
        var second = paymentRepo.Payments[1];
        Assert.Equal(new DateOnly(2027, 1, 1), second.BillingPeriodStart);
        Assert.Equal(new DateOnly(2027, 4, 1), second.BillingPeriodEnd);
        
        // Exact pro-rate logic check:
        // actualDays = 2027-04-01 - 2027-01-01 = 90 days
        // expectedDays = 2028-01-01 - 2027-01-01 = 365 days
        // expectedAmount = 1000 * 12 * (90.0 / 365.0) = 2958.904... JOD, rounded to numeric(12,3)
        decimal actualDays = new DateOnly(2027, 4, 1).DayNumber - new DateOnly(2027, 1, 1).DayNumber;
        decimal expectedDays = new DateOnly(2028, 1, 1).DayNumber - new DateOnly(2027, 1, 1).DayNumber;
        decimal expectedAmount = Math.Round(1000m * 12 * (actualDays / expectedDays), 3, MidpointRounding.AwayFromZero);

        Assert.Equal(expectedAmount, second.AmountDue);
    }
}
