using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials.Queries.GetChequesByStatus;
using PropertyOS.Application.Financials.Queries.GetOutstandingRentPayments;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;
using PropertyOS.Application.Financials.Queries.GetRentPaymentsForLease;
using PropertyOS.Application.Financials.Queries.GetRentPaymentsForTenant;
using PropertyOS.Application.Financials.Queries.GetUpcomingCheques;
using PropertyOS.Application.Financials.Queries.SearchRentPayments;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials;

public class RentPaymentQueriesTests
{
    private class FakeTenantContext : ITenantContext
    {
        public Guid? CompanyId { get; set; }
        public bool IsPlatformAdmin => false;
    }

    private class FakeRentPaymentRepository : IRentPaymentRepository
    {
        public List<RentPayment> Payments { get; } = new();
        public List<ChequeDetails> Cheques { get; } = new();
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

        public Task AddReceiptAsync(RentPaymentReceipt receipt, CancellationToken cancellationToken = default)
        {
            Receipts.Add(receipt);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<RentPayment> rentPayments, CancellationToken cancellationToken = default)
        {
            Payments.AddRange(rentPayments);
            return Task.CompletedTask;
        }

        public Task<List<BillingPeriod>> GetScheduledInstallmentPeriodsAsync(Guid leaseContractId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<BillingPeriod>());

        public Task<bool> HasScheduledInstallmentAsync(Guid leaseContractId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
            => Task.FromResult(Payments.Any(p => p.LeaseContractId == leaseContractId && p.BillingPeriodStart == start && p.BillingPeriodEnd == end));

        public Task<List<Guid>> GetOverdueCandidateIdsAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<ChequeDetails?> LoadChequeAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Cheques.FirstOrDefault(c => c.Id == id));

        public Task AddChequeAsync(ChequeDetails cheque, CancellationToken cancellationToken = default)
        {
            Cheques.Add(cheque);
            return Task.CompletedTask;
        }

        public Task<PaymentAllocation?> LoadAllocationAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Allocations.FirstOrDefault(a => a.Id == id));

        public Task AddAllocationAsync(PaymentAllocation allocation, CancellationToken cancellationToken = default)
        {
            Allocations.Add(allocation);
            return Task.CompletedTask;
        }

        public Task<List<PaymentAllocation>> GetAllocationsByObligationIdAsync(Guid obligationId, CancellationToken cancellationToken = default)
            => Task.FromResult(Allocations.Where(a => a.ObligationPaymentId == obligationId).ToList());

        public Task<List<PaymentAllocation>> GetAllocationsByReceivingIdAsync(Guid receivingId, CancellationToken cancellationToken = default)
            => Task.FromResult(Allocations.Where(a => a.ReceivingPaymentId == receivingId).ToList());

        // READ-SIDE PROJECTIONS — mirror the repository contract: tenant-scoped by companyId.
        public Task<RentPaymentDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
        {
            var p = Payments.FirstOrDefault(p => p.Id == id && p.CompanyId == companyId);
            if (p == null) return Task.FromResult<RentPaymentDetailDto?>(null);

            var cheque = Cheques.FirstOrDefault(c => c.RentPaymentId == id);
            var chequeDto = cheque != null ? new ChequeDetailDto
            {
                Id = cheque.Id,
                RentPaymentId = cheque.RentPaymentId,
                ChequeNumber = cheque.ChequeNumber,
                BankName = cheque.BankName,
                Amount = cheque.Amount,
                Status = cheque.Status
            } : null;

            var incoming = Allocations
                .Where(a => a.ReceivingPaymentId == id)
                .Select(a => new PaymentAllocationDto { Id = a.Id, ReceivingPaymentId = a.ReceivingPaymentId, ObligationPaymentId = a.ObligationPaymentId, AllocatedAmount = a.AllocatedAmount, AllocationStatus = a.AllocationStatus })
                .ToList();

            var outgoing = Allocations
                .Where(a => a.ObligationPaymentId == id)
                .Select(a => new PaymentAllocationDto { Id = a.Id, ReceivingPaymentId = a.ReceivingPaymentId, ObligationPaymentId = a.ObligationPaymentId, AllocatedAmount = a.AllocatedAmount, AllocationStatus = a.AllocationStatus })
                .ToList();

            var detail = new RentPaymentDetailDto
            {
                Id = p.Id,
                AmountDue = p.AmountDue,
                AmountPaid = p.AmountPaid,
                DueDateStatus = p.DueDateStatus,
                DueDate = p.DueDate,
                PaymentPurpose = p.PaymentPurpose,
                ChequeDetails = chequeDto,
                IncomingAllocations = incoming,
                OutgoingAllocations = outgoing
            };

            return Task.FromResult<RentPaymentDetailDto?>(detail);
        }

        public Task<List<RentPaymentDto>> GetPaymentsForLeaseAsync(Guid leaseContractId, Guid companyId, CancellationToken cancellationToken = default)
        {
            var list = Payments
                .Where(p => p.LeaseContractId == leaseContractId && p.CompanyId == companyId)
                .OrderBy(p => p.DueDate)
                .Select(p => new RentPaymentDto { Id = p.Id, LeaseContractId = p.LeaseContractId, AmountDue = p.AmountDue, DueDate = p.DueDate })
                .ToList();
            return Task.FromResult(list);
        }

        public Task<List<RentPaymentDto>> GetPaymentsForTenantAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default)
        {
            var list = Payments
                .Where(p => p.TenantId == tenantId && p.CompanyId == companyId)
                .OrderByDescending(p => p.DueDate)
                .Select(p => new RentPaymentDto { Id = p.Id, TenantId = p.TenantId, AmountDue = p.AmountDue })
                .ToList();
            return Task.FromResult(list);
        }

        public Task<List<RentPaymentDto>> SearchPaymentsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default)
        {
            var effectivePageSize = Math.Clamp(pageSize, 1, 200);
            var list = Payments
                .Where(p => p.CompanyId == companyId)
                .Where(p => string.IsNullOrEmpty(searchTerm) || (p.ReceiptNumber != null && p.ReceiptNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                .Take(effectivePageSize)
                .Select(p => new RentPaymentDto { Id = p.Id, ReceiptNumber = p.ReceiptNumber, AmountDue = p.AmountDue })
                .ToList();
            return Task.FromResult(list);
        }

        public Task<List<RentPaymentDto>> GetOutstandingPaymentsAsync(Guid companyId, int pageSize, CancellationToken cancellationToken = default)
        {
            var effectivePageSize = Math.Clamp(pageSize, 1, 200);

            var outstandingStatuses = new[]
            {
                DueDateStatus.Pending,
                DueDateStatus.PartiallyPaid,
                DueDateStatus.Late,
                DueDateStatus.OverdueUnpaid
            };

            var list = Payments
                .Where(p => p.CompanyId == companyId
                            && p.PaymentPurpose == PaymentPurpose.ScheduledInstallment
                            && p.DeletedAt == null
                            && outstandingStatuses.Contains(p.DueDateStatus))
                .OrderBy(p => p.DueDate)
                .Take(effectivePageSize)
                .Select(p => new RentPaymentDto { Id = p.Id, DueDateStatus = p.DueDateStatus, DueDate = p.DueDate })
                .ToList();
            return Task.FromResult(list);
        }

        public Task<List<ChequeDetailDto>> GetChequesAsync(ChequeStatus? status, Guid companyId, int pageSize, CancellationToken cancellationToken = default)
        {
            var effectivePageSize = Math.Clamp(pageSize, 1, 200);
            var query = Cheques.Where(c => c.CompanyId == companyId && c.DeletedAt == null);

            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }

            var list = query
                .OrderBy(c => c.DueDate)
                .ThenBy(c => c.Id)
                .Take(effectivePageSize)
                .Select(c => new ChequeDetailDto { Id = c.Id, Status = c.Status })
                .ToList();

            return Task.FromResult(list);
        }

        public Task<List<ChequeDetailDto>> GetUpcomingChequesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var limitDate = today.AddDays(daysAhead);

            var upcomingStatuses = new[]
            {
                ChequeStatus.Received,
                ChequeStatus.Deposited
            };

            var list = Cheques
                .Where(c => c.CompanyId == companyId && upcomingStatuses.Contains(c.Status) && c.DueDate >= today && c.DueDate <= limitDate)
                .OrderBy(c => c.DueDate)
                .Select(c => new ChequeDetailDto { Id = c.Id, Status = c.Status, DueDate = c.DueDate })
                .ToList();
            return Task.FromResult(list);
        }

        public Task<RentPaymentReceiptDto?> GetReceiptByRentPaymentIdAsync(Guid rentPaymentId, Guid companyId, CancellationToken cancellationToken = default)
        {
            var receipt = Receipts.FirstOrDefault(r => r.RentPaymentId == rentPaymentId && r.CompanyId == companyId && r.DeletedAt == null);
            if (receipt == null) return Task.FromResult<RentPaymentReceiptDto?>(null);

            var dto = new RentPaymentReceiptDto
            {
                Id = receipt.Id,
                RentPaymentId = receipt.RentPaymentId,
                ReceiptNumber = receipt.ReceiptNumber,
                IssueDate = receipt.IssueDate,
                Amount = receipt.Amount,
                Currency = receipt.Currency
            };
            return Task.FromResult<RentPaymentReceiptDto?>(dto);
        }

        public Task<List<RentPaymentReceiptDto>> GetReceiptsAsync(RentPaymentReceiptFilterOptions filter, Guid companyId, CancellationToken cancellationToken = default)
        {
            var query = Receipts.Where(r => r.CompanyId == companyId && r.DeletedAt == null);

            if (filter.DateFrom.HasValue)
                query = query.Where(r => r.IssueDate >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                query = query.Where(r => r.IssueDate <= filter.DateTo.Value);

            var effectiveSize = Math.Clamp(filter.PageSize, 1, 200);

            var list = query
                .OrderByDescending(r => r.IssueDate)
                .ThenBy(r => r.Id)
                .Take(effectiveSize)
                .Select(r => new RentPaymentReceiptDto
                {
                    Id = r.Id,
                    RentPaymentId = r.RentPaymentId,
                    ReceiptNumber = r.ReceiptNumber,
                    IssueDate = r.IssueDate,
                    Amount = r.Amount,
                    Currency = r.Currency
                })
                .ToList();

            return Task.FromResult(list);
        }
    }

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly FakeTenantContext _tenantContext;

    public RentPaymentQueriesTests()
    {
        _tenantContext = new FakeTenantContext { CompanyId = _companyId };
    }

    private RentPayment CreateRentPayment(Guid id, Guid leaseContractId, Guid tenantId, decimal amount, DueDateStatus status, DateOnly? dueDate = null, Guid? companyId = null)
    {
        var p = RentPayment.Create(
            companyId: companyId ?? _companyId,
            leaseContractId: leaseContractId,
            tenantId: tenantId,
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            purpose: PaymentPurpose.ScheduledInstallment,
            amountDue: amount,
            currency: "JOD",
            billingPeriodStart: new DateOnly(2026, 1, 1),
            billingPeriodEnd: new DateOnly(2026, 2, 1),
            dueDate: dueDate ?? new DateOnly(2026, 1, 5),
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()
        );

        var idProp = typeof(RentPayment).GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        idProp?.SetValue(p, id);

        var statusProp = typeof(RentPayment).GetProperty("DueDateStatus", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        statusProp?.SetValue(p, status);

        return p;
    }

    private ChequeDetails CreateCheque(ChequeStatus status, DateOnly dueDate, Guid? companyId = null)
    {
        var cheque = ChequeDetails.Create(
            companyId: companyId ?? _companyId,
            rentPaymentId: Guid.NewGuid(),
            leaseContractId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            chequeNumber: "CHQ-100",
            bankName: "Arab Bank",
            bankBranch: null,
            issueDate: new DateOnly(2026, 1, 1),
            dueDate: dueDate,
            amount: 1000m,
            currency: "JOD",
            receivedDate: new DateOnly(2026, 1, 2),
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()
        );
        var statusProp = typeof(ChequeDetails).GetProperty("Status", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        statusProp?.SetValue(cheque, status);
        return cheque;
    }

    [Fact]
    public async Task GetRentPaymentByIdQuery_ReturnsDetailDto()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new GetRentPaymentByIdQueryHandler(repo, _tenantContext);

        var id = Guid.NewGuid();
        var payment = CreateRentPayment(id, Guid.NewGuid(), Guid.NewGuid(), 500m, DueDateStatus.Pending);
        await repo.AddAsync(payment);

        var result = await handler.Handle(new GetRentPaymentByIdQuery(id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal(500m, result.AmountDue);
    }

    [Fact]
    public async Task GetRentPaymentByIdQuery_OtherTenantsPayment_ReturnsNull()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new GetRentPaymentByIdQueryHandler(repo, _tenantContext);

        var id = Guid.NewGuid();
        var foreignPayment = CreateRentPayment(id, Guid.NewGuid(), Guid.NewGuid(), 500m, DueDateStatus.Pending, companyId: Guid.NewGuid());
        await repo.AddAsync(foreignPayment);

        var result = await handler.Handle(new GetRentPaymentByIdQuery(id), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRentPaymentByIdQuery_MissingTenantContext_Throws()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new GetRentPaymentByIdQueryHandler(repo, new FakeTenantContext { CompanyId = null });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetRentPaymentByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task GetRentPaymentByIdQueryValidator_EnforcesIdNotEmpty()
    {
        var validator = new GetRentPaymentByIdQueryValidator();
        var result = await validator.ValidateAsync(new GetRentPaymentByIdQuery(Guid.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetRentPaymentsForLeaseQuery_ReturnsSortedPayments()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new GetRentPaymentsForLeaseQueryHandler(repo, _tenantContext);

        var leaseId = Guid.NewGuid();
        var p1 = CreateRentPayment(Guid.NewGuid(), leaseId, Guid.NewGuid(), 500m, DueDateStatus.Pending, new DateOnly(2026, 2, 1));
        var p2 = CreateRentPayment(Guid.NewGuid(), leaseId, Guid.NewGuid(), 500m, DueDateStatus.Pending, new DateOnly(2026, 1, 1));

        await repo.AddAsync(p1);
        await repo.AddAsync(p2);

        var result = await handler.Handle(new GetRentPaymentsForLeaseQuery(leaseId), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(p2.Id, result[0].Id); // sorted by due date ASC
    }

    [Fact]
    public async Task GetRentPaymentsForLeaseQuery_ExcludesOtherTenantsPayments()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new GetRentPaymentsForLeaseQueryHandler(repo, _tenantContext);

        var leaseId = Guid.NewGuid();
        var mine = CreateRentPayment(Guid.NewGuid(), leaseId, Guid.NewGuid(), 500m, DueDateStatus.Pending);
        var foreign = CreateRentPayment(Guid.NewGuid(), leaseId, Guid.NewGuid(), 500m, DueDateStatus.Pending, companyId: Guid.NewGuid());

        await repo.AddAsync(mine);
        await repo.AddAsync(foreign);

        var result = await handler.Handle(new GetRentPaymentsForLeaseQuery(leaseId), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(mine.Id, result[0].Id);
    }

    [Fact]
    public async Task GetRentPaymentsForLeaseQueryValidator_EnforcesIdNotEmpty()
    {
        var validator = new GetRentPaymentsForLeaseQueryValidator();
        var result = await validator.ValidateAsync(new GetRentPaymentsForLeaseQuery(Guid.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetRentPaymentsForTenantQuery_ReturnsTenantPayments()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new GetRentPaymentsForTenantQueryHandler(repo, _tenantContext);

        var tenantId = Guid.NewGuid();
        var p1 = CreateRentPayment(Guid.NewGuid(), Guid.NewGuid(), tenantId, 500m, DueDateStatus.Pending);
        await repo.AddAsync(p1);

        var result = await handler.Handle(new GetRentPaymentsForTenantQuery(tenantId), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(p1.Id, result[0].Id);
    }

    [Fact]
    public async Task GetRentPaymentsForTenantQueryValidator_EnforcesIdNotEmpty()
    {
        var validator = new GetRentPaymentsForTenantQueryValidator();
        var result = await validator.ValidateAsync(new GetRentPaymentsForTenantQuery(Guid.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task SearchRentPaymentsQuery_FiltersReceipts()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new SearchRentPaymentsQueryHandler(repo, _tenantContext);

        var payment = CreateRentPayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 500m, DueDateStatus.Pending);
        var receiptProp = typeof(RentPayment).GetProperty("ReceiptNumber", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        receiptProp?.SetValue(payment, "RCP-999");
        await repo.AddAsync(payment);

        var result = await handler.Handle(new SearchRentPaymentsQuery("RCP-999"), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("RCP-999", result[0].ReceiptNumber);
    }

    [Fact]
    public async Task SearchRentPaymentsQuery_EmptyTerm_RespectsPageSize()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new SearchRentPaymentsQueryHandler(repo, _tenantContext);

        for (var i = 0; i < 5; i++)
        {
            await repo.AddAsync(CreateRentPayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, DueDateStatus.Pending));
        }

        var result = await handler.Handle(new SearchRentPaymentsQuery("", PageSize: 3), CancellationToken.None);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task SearchRentPaymentsQueryValidator_EnforcesSearchTermNotNull()
    {
        var validator = new SearchRentPaymentsQueryValidator();
        var result = await validator.ValidateAsync(new SearchRentPaymentsQuery(null!));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task SearchRentPaymentsQueryValidator_EnforcesPageSizeRange()
    {
        var validator = new SearchRentPaymentsQueryValidator();

        var tooSmall = await validator.ValidateAsync(new SearchRentPaymentsQuery("x", PageSize: 0));
        var tooLarge = await validator.ValidateAsync(new SearchRentPaymentsQuery("x", PageSize: 201));
        var valid = await validator.ValidateAsync(new SearchRentPaymentsQuery("x", PageSize: 200));

        Assert.False(tooSmall.IsValid);
        Assert.False(tooLarge.IsValid);
        Assert.True(valid.IsValid);
    }

    [Fact]
    public async Task GetOutstandingRentPaymentsQuery_ReturnsOnlyOutstanding()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new GetOutstandingRentPaymentsQueryHandler(repo, _tenantContext);

        var p1 = CreateRentPayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 500m, DueDateStatus.Pending, new DateOnly(2026, 1, 1));
        var p2 = CreateRentPayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 500m, DueDateStatus.Paid, new DateOnly(2026, 1, 2));

        await repo.AddAsync(p1);
        await repo.AddAsync(p2);

        var result = await handler.Handle(new GetOutstandingRentPaymentsQuery(), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(p1.Id, result[0].Id);
    }

    [Fact]
    public async Task GetOutstandingRentPaymentsQuery_ExcludesOtherTenantsPayments()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new GetOutstandingRentPaymentsQueryHandler(repo, _tenantContext);

        var mine = CreateRentPayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 500m, DueDateStatus.Pending, new DateOnly(2026, 1, 1));
        var foreign = CreateRentPayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 500m, DueDateStatus.Pending, new DateOnly(2026, 1, 2), companyId: Guid.NewGuid());

        await repo.AddAsync(mine);
        await repo.AddAsync(foreign);

        var result = await handler.Handle(new GetOutstandingRentPaymentsQuery(), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(mine.Id, result[0].Id);
    }

    [Fact]
    public async Task GetOutstandingRentPaymentsQuery_ExcludesUnallocatedReceipts_Adjustments_Paid_And_SoftDeleted()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new GetOutstandingRentPaymentsQueryHandler(repo, _tenantContext);

        var leaseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // 1. ScheduledInstallment - Pending (Outstanding) -> SHOULD be returned
        var scheduledPending = CreateRentPayment(Guid.NewGuid(), leaseId, tenantId, 900m, DueDateStatus.Pending, new DateOnly(2026, 1, 1));

        // 1b. ScheduledInstallment - Late (Outstanding) -> SHOULD be returned
        var scheduledLate = CreateRentPayment(Guid.NewGuid(), leaseId, tenantId, 900m, DueDateStatus.Late, new DateOnly(2026, 1, 2));

        // 2. UnallocatedReceipt - Pending -> Should NOT be returned
        var unallocatedReceipt = RentPayment.Create(
            companyId: _companyId,
            leaseContractId: leaseId,
            tenantId: tenantId,
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            purpose: PaymentPurpose.UnallocatedReceipt,
            amountDue: 500m,
            currency: "JOD",
            billingPeriodStart: null,
            billingPeriodEnd: null,
            dueDate: null,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()
        );

        // 3. Adjustment - Pending -> Should NOT be returned
        var adjustment = RentPayment.Create(
            companyId: _companyId,
            leaseContractId: leaseId,
            tenantId: tenantId,
            buildingId: Guid.NewGuid(),
            apartmentId: Guid.NewGuid(),
            purpose: PaymentPurpose.Adjustment,
            amountDue: 100m,
            currency: "JOD",
            billingPeriodStart: null,
            billingPeriodEnd: null,
            dueDate: null,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()
        );

        // 4. ScheduledInstallment - Fully Paid -> Should NOT be returned
        var scheduledPaid = CreateRentPayment(Guid.NewGuid(), leaseId, tenantId, 900m, DueDateStatus.Paid, new DateOnly(2026, 1, 3));

        // 5. ScheduledInstallment - Soft Deleted -> Should NOT be returned
        var scheduledSoftDeleted = CreateRentPayment(Guid.NewGuid(), leaseId, tenantId, 900m, DueDateStatus.Pending, new DateOnly(2026, 1, 4));
        scheduledSoftDeleted.SoftDelete(DateTimeOffset.UtcNow, Guid.NewGuid());

        // 6. Foreign company payment -> Should NOT be returned
        var foreignCompanyPayment = CreateRentPayment(Guid.NewGuid(), leaseId, tenantId, 900m, DueDateStatus.Pending, new DateOnly(2026, 1, 5), companyId: Guid.NewGuid());

        await repo.AddAsync(scheduledPending);
        await repo.AddAsync(scheduledLate);
        await repo.AddAsync(unallocatedReceipt);
        await repo.AddAsync(adjustment);
        await repo.AddAsync(scheduledPaid);
        await repo.AddAsync(scheduledSoftDeleted);
        await repo.AddAsync(foreignCompanyPayment);

        var result = await handler.Handle(new GetOutstandingRentPaymentsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Id == scheduledPending.Id);
        Assert.Contains(result, r => r.Id == scheduledLate.Id);
        Assert.DoesNotContain(result, r => r.Id == unallocatedReceipt.Id);
        Assert.DoesNotContain(result, r => r.Id == adjustment.Id);
        Assert.DoesNotContain(result, r => r.Id == scheduledPaid.Id);
        Assert.DoesNotContain(result, r => r.Id == scheduledSoftDeleted.Id);
        Assert.DoesNotContain(result, r => r.Id == foreignCompanyPayment.Id);
    }

    [Fact]
    public async Task GetChequesQuery_OmittedStatus_ReturnsAllStatusesForCompany()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new GetChequesQueryHandler(repo, _tenantContext);

        var c1 = CreateCheque(ChequeStatus.Received, new DateOnly(2026, 1, 10));
        var c2 = CreateCheque(ChequeStatus.Deposited, new DateOnly(2026, 1, 11));
        var c3 = CreateCheque(ChequeStatus.Cleared, new DateOnly(2026, 1, 12));

        await repo.AddChequeAsync(c1);
        await repo.AddChequeAsync(c2);
        await repo.AddChequeAsync(c3);

        // Status is null -> should return all 3 cheques
        var result = await handler.Handle(new GetChequesQuery(Status: null), CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, x => x.Id == c1.Id);
        Assert.Contains(result, x => x.Id == c2.Id);
        Assert.Contains(result, x => x.Id == c3.Id);
    }

    [Fact]
    public async Task GetChequesQuery_SpecificStatus_ReturnsOnlyMatchingStatus()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new GetChequesQueryHandler(repo, _tenantContext);

        var c1 = CreateCheque(ChequeStatus.Received, new DateOnly(2026, 1, 10));
        var c2 = CreateCheque(ChequeStatus.Deposited, new DateOnly(2026, 1, 11));

        await repo.AddChequeAsync(c1);
        await repo.AddChequeAsync(c2);

        var resultReceived = await handler.Handle(new GetChequesQuery(Status: ChequeStatus.Received), CancellationToken.None);
        Assert.Single(resultReceived);
        Assert.Equal(c1.Id, resultReceived[0].Id);

        var resultDeposited = await handler.Handle(new GetChequesQuery(Status: ChequeStatus.Deposited), CancellationToken.None);
        Assert.Single(resultDeposited);
        Assert.Equal(c2.Id, resultDeposited[0].Id);
    }

    [Fact]
    public async Task GetChequesQuery_ExcludesOtherTenantsCheques()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new GetChequesQueryHandler(repo, _tenantContext);

        var mine = CreateCheque(ChequeStatus.Received, new DateOnly(2026, 1, 10));
        var foreign = CreateCheque(ChequeStatus.Received, new DateOnly(2026, 1, 10), companyId: Guid.NewGuid());
        await repo.AddChequeAsync(mine);
        await repo.AddChequeAsync(foreign);

        var result = await handler.Handle(new GetChequesQuery(Status: ChequeStatus.Received), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(mine.Id, result[0].Id);
    }

    [Fact]
    public async Task GetChequesQueryValidator_EnforcesValidEnum_WhenStatusProvided()
    {
        var validator = new GetChequesQueryValidator();
        var invalidResult = await validator.ValidateAsync(new GetChequesQuery((ChequeStatus)999));
        Assert.False(invalidResult.IsValid);

        var nullResult = await validator.ValidateAsync(new GetChequesQuery(Status: null));
        Assert.True(nullResult.IsValid);
    }

    [Fact]
    public async Task GetUpcomingChequesQuery_ReturnsSoonDueCheques()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new GetUpcomingChequesQueryHandler(repo, _tenantContext);

        var cheque = CreateCheque(ChequeStatus.Received, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5));
        await repo.AddChequeAsync(cheque);

        var result = await handler.Handle(new GetUpcomingChequesQuery(10), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(cheque.Id, result[0].Id);
    }

    [Fact]
    public async Task GetUpcomingChequesQueryValidator_EnforcesDaysAheadNonNegative()
    {
        var validator = new GetUpcomingChequesQueryValidator();
        var result = await validator.ValidateAsync(new GetUpcomingChequesQuery(-5));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetRentPaymentReceiptByRentPaymentIdQuery_ReturnsReceipt_WhenExists()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new PropertyOS.Application.Financials.Queries.GetRentPaymentReceiptByRentPaymentId.GetRentPaymentReceiptByRentPaymentIdQueryHandler(repo, _tenantContext);

        var paymentId = Guid.NewGuid();
        var payment = CreateRentPayment(paymentId, Guid.NewGuid(), Guid.NewGuid(), 1000m, DueDateStatus.Paid);
        var paidProp = typeof(RentPayment).GetProperty("AmountPaid", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        paidProp?.SetValue(payment, 1000m);

        var receipt = payment.IssueReceipt("REC-00001", DateTimeOffset.UtcNow, Guid.NewGuid());
        await repo.AddAsync(payment);
        await repo.AddReceiptAsync(receipt);

        var result = await handler.Handle(new PropertyOS.Application.Financials.Queries.GetRentPaymentReceiptByRentPaymentId.GetRentPaymentReceiptByRentPaymentIdQuery(paymentId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(paymentId, result.RentPaymentId);
        Assert.Equal("REC-00001", result.ReceiptNumber);
        Assert.Equal(1000m, result.Amount);
    }

    [Fact]
    public async Task GetRentPaymentReceiptByRentPaymentIdQuery_OtherCompany_ReturnsNull()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new PropertyOS.Application.Financials.Queries.GetRentPaymentReceiptByRentPaymentId.GetRentPaymentReceiptByRentPaymentIdQueryHandler(repo, _tenantContext);

        var paymentId = Guid.NewGuid();
        var foreignCompanyId = Guid.NewGuid();
        var payment = CreateRentPayment(paymentId, Guid.NewGuid(), Guid.NewGuid(), 1000m, DueDateStatus.Paid, companyId: foreignCompanyId);
        var paidProp = typeof(RentPayment).GetProperty("AmountPaid", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        paidProp?.SetValue(payment, 1000m);

        var receipt = payment.IssueReceipt("REC-00002", DateTimeOffset.UtcNow, Guid.NewGuid());
        await repo.AddAsync(payment);
        await repo.AddReceiptAsync(receipt);

        var result = await handler.Handle(new PropertyOS.Application.Financials.Queries.GetRentPaymentReceiptByRentPaymentId.GetRentPaymentReceiptByRentPaymentIdQuery(paymentId), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRentPaymentReceiptByRentPaymentIdQueryValidator_EnforcesRentPaymentIdNotEmpty()
    {
        var validator = new PropertyOS.Application.Financials.Queries.GetRentPaymentReceiptByRentPaymentId.GetRentPaymentReceiptByRentPaymentIdQueryValidator();
        var result = await validator.ValidateAsync(new PropertyOS.Application.Financials.Queries.GetRentPaymentReceiptByRentPaymentId.GetRentPaymentReceiptByRentPaymentIdQuery(Guid.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetRentPaymentReceiptsQuery_ReturnsFilteredAndPaginatedReceipts()
    {
        var repo = new FakeRentPaymentRepository();
        var handler = new PropertyOS.Application.Financials.Queries.GetRentPaymentReceipts.GetRentPaymentReceiptsQueryHandler(repo, _tenantContext);

        var p1 = CreateRentPayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 500m, DueDateStatus.Paid);
        var paidProp = typeof(RentPayment).GetProperty("AmountPaid", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        paidProp?.SetValue(p1, 500m);
        var r1 = p1.IssueReceipt("REC-0001", new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero), Guid.NewGuid());

        var p2 = CreateRentPayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 750m, DueDateStatus.Paid);
        paidProp?.SetValue(p2, 750m);
        var r2 = p2.IssueReceipt("REC-0002", new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero), Guid.NewGuid());

        var p3 = CreateRentPayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1000m, DueDateStatus.Paid, companyId: Guid.NewGuid());
        paidProp?.SetValue(p3, 1000m);
        var r3 = p3.IssueReceipt("REC-0003", new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero), Guid.NewGuid());

        await repo.AddReceiptAsync(r1);
        await repo.AddReceiptAsync(r2);
        await repo.AddReceiptAsync(r3);

        var query = new PropertyOS.Application.Financials.Queries.GetRentPaymentReceipts.GetRentPaymentReceiptsQuery(
            DateFrom: new DateOnly(2026, 7, 1),
            DateTo: new DateOnly(2026, 7, 6),
            PageSize: 10
        );

        var results = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.ReceiptNumber == "REC-0001");
        Assert.Contains(results, r => r.ReceiptNumber == "REC-0002");
    }

    [Fact]
    public async Task GetRentPaymentReceiptsQueryValidator_ValidatesPageSizeAndDateBounds()
    {
        var validator = new PropertyOS.Application.Financials.Queries.GetRentPaymentReceipts.GetRentPaymentReceiptsQueryValidator();

        var invalidPageSize = await validator.ValidateAsync(new PropertyOS.Application.Financials.Queries.GetRentPaymentReceipts.GetRentPaymentReceiptsQuery(PageSize: 0));
        Assert.False(invalidPageSize.IsValid);

        var invalidDateRange = await validator.ValidateAsync(new PropertyOS.Application.Financials.Queries.GetRentPaymentReceipts.GetRentPaymentReceiptsQuery(
            DateFrom: new DateOnly(2026, 7, 10),
            DateTo: new DateOnly(2026, 7, 1)
        ));
        Assert.False(invalidDateRange.IsValid);

        var validQuery = await validator.ValidateAsync(new PropertyOS.Application.Financials.Queries.GetRentPaymentReceipts.GetRentPaymentReceiptsQuery(
            DateFrom: new DateOnly(2026, 7, 1),
            DateTo: new DateOnly(2026, 7, 10),
            PageSize: 50
        ));
        Assert.True(validQuery.IsValid);
    }
}

