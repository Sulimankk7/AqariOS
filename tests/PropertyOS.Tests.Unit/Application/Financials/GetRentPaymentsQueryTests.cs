using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;
using PropertyOS.Application.Financials.Queries.GetRentPayments;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Financials;

public class GetRentPaymentsQueryTests
{
    private class FakeTenantContext : ITenantContext
    {
        public Guid? CompanyId { get; set; }
        public bool IsPlatformAdmin => false;
    }

    private class InMemoryRentPaymentRepository : IRentPaymentRepository
    {
        public List<RentPayment> Payments { get; } = new();
        public Dictionary<Guid, string> TenantNames { get; } = new();
        public Dictionary<Guid, string> BuildingNames { get; } = new();
        public Dictionary<Guid, string> ApartmentNumbers { get; } = new();
        public Dictionary<Guid, string> ContractNumbers { get; } = new();

        public Task<RentPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Payments.FirstOrDefault(p => p.Id == id));

        public Task<List<RentPayment>> GetByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAsync(RentPayment rentPayment, CancellationToken cancellationToken = default)
        {
            Payments.Add(rentPayment);
            return Task.CompletedTask;
        }
        public Task AddRangeAsync(IEnumerable<RentPayment> rentPayments, CancellationToken cancellationToken = default)
        {
            Payments.AddRange(rentPayments);
            return Task.CompletedTask;
        }
        public Task<bool> HasScheduledInstallmentAsync(Guid leaseContractId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<BillingPeriod>> GetScheduledInstallmentPeriodsAsync(Guid leaseContractId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<Guid>> GetOverdueCandidateIdsAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> GetRentGracePeriodDaysAsync(Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult(5);
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
        public Task<PropertyOS.Application.Common.Models.KeysetPage<PropertyOS.Application.Financials.Queries.GetPendingPaymentVerifications.PaymentVerificationQueueItemDto>> GetPendingVerificationsAsync(Guid companyId, int pageSize, DateTimeOffset? lastSeenSubmittedAt, Guid? lastSeenId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<ChequeDetailDto>> GetChequesAsync(ChequeStatus? status, Guid companyId, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<ChequeDetailDto>> GetUpcomingChequesAsync(int daysAhead, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<RentPaymentReceiptDto?> GetReceiptByRentPaymentIdAsync(Guid rentPaymentId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<RentPaymentReceiptDto>> GetReceiptsAsync(RentPaymentReceiptFilterOptions filter, Guid companyId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<List<RentPaymentDto>> GetPaymentsAsync(RentPaymentFilterOptions filter, Guid companyId, CancellationToken cancellationToken = default)
        {
            var query = Payments.Where(p => p.CompanyId == companyId && p.DeletedAt == null);

            if (filter.BuildingId.HasValue)
                query = query.Where(p => p.BuildingId == filter.BuildingId.Value);

            if (filter.Status.HasValue)
                query = query.Where(p => p.DueDateStatus == filter.Status.Value);

            if (filter.DateFrom.HasValue)
                query = query.Where(p => p.DueDate.HasValue && p.DueDate.Value >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                query = query.Where(p => p.DueDate.HasValue && p.DueDate.Value <= filter.DateTo.Value);

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim();
                query = query.Where(p =>
                    (p.ReceiptNumber != null && p.ReceiptNumber.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (ContractNumbers.TryGetValue(p.LeaseContractId, out var cNum) && cNum.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (TenantNames.TryGetValue(p.TenantId, out var tName) && tName.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            if (filter.LastSeenId.HasValue && filter.LastSeenDueDate.HasValue)
            {
                var cursorDate = filter.LastSeenDueDate.Value;
                var cursorId = filter.LastSeenId.Value;
                query = query.Where(p =>
                    (p.DueDate.HasValue && p.DueDate.Value < cursorDate) ||
                    (p.DueDate.HasValue && p.DueDate.Value == cursorDate && p.Id.CompareTo(cursorId) > 0));
            }

            var effectivePageSize = Math.Clamp(filter.PageSize, 1, 200);
            var list = query
                .OrderByDescending(p => p.DueDate)
                .ThenBy(p => p.Id)
                .Take(effectivePageSize)
                .Select(p => new RentPaymentDto
                {
                    Id = p.Id,
                    CompanyId = p.CompanyId,
                    BuildingId = p.BuildingId,
                    ApartmentId = p.ApartmentId,
                    TenantId = p.TenantId,
                    LeaseContractId = p.LeaseContractId,
                    AmountDue = p.AmountDue,
                    AmountPaid = p.AmountPaid,
                    Currency = p.Currency,
                    DueDate = p.DueDate,
                    DueDateStatus = p.DueDateStatus,
                    BillingPeriodStart = p.BillingPeriodStart,
                    BillingPeriodEnd = p.BillingPeriodEnd,
                    ReceiptNumber = p.ReceiptNumber,
                    PaymentPurpose = p.PaymentPurpose,
                    TenantName = TenantNames.TryGetValue(p.TenantId, out var tn) ? tn : null,
                    BuildingName = BuildingNames.TryGetValue(p.BuildingId, out var bn) ? bn : null,
                    ApartmentNumber = ApartmentNumbers.TryGetValue(p.ApartmentId, out var an) ? an : null,
                    ContractNumber = ContractNumbers.TryGetValue(p.LeaseContractId, out var cn) ? cn : null,
                })
                .ToList();

            return Task.FromResult(list);
        }
    }

    private static RentPayment MakePayment(
        Guid companyId,
        Guid? buildingId = null,
        Guid? tenantId = null,
        Guid? apartmentId = null,
        Guid? leaseContractId = null,
        decimal amountDue = 500m,
        DateOnly? dueDate = null,
        DueDateStatus status = DueDateStatus.Pending,
        decimal amountPaid = 0m)
    {
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 1, 31);
        var due = dueDate ?? new DateOnly(2026, 1, 5);

        var payment = RentPayment.Create(
            companyId: companyId,
            leaseContractId: leaseContractId ?? Guid.NewGuid(),
            tenantId: tenantId ?? Guid.NewGuid(),
            buildingId: buildingId ?? Guid.NewGuid(),
            apartmentId: apartmentId ?? Guid.NewGuid(),
            purpose: PaymentPurpose.ScheduledInstallment,
            amountDue: amountDue,
            currency: "JOD",
            billingPeriodStart: start,
            billingPeriodEnd: end,
            dueDate: due,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()
        );

        if (amountPaid > 0 || status != DueDateStatus.Pending)
        {
            payment.UpdateAllocationSync(amountPaid, status, DateTimeOffset.UtcNow, Guid.NewGuid());
        }

        return payment;
    }

    // 1. Returns company-scoped rent payments.
    [Fact]
    public async Task Handle_ReturnsCompanyScopedRentPayments()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var repo = new InMemoryRentPaymentRepository();
        repo.Payments.Add(MakePayment(companyA));
        repo.Payments.Add(MakePayment(companyB));

        var handler = new GetRentPaymentsQueryHandler(repo, new FakeTenantContext { CompanyId = companyA });
        var result = await handler.Handle(new GetRentPaymentsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].CompanyId.Should().Be(companyA);
    }

    // 2. Filters by building.
    [Fact]
    public async Task Handle_FiltersByBuilding()
    {
        var companyId = Guid.NewGuid();
        var buildingA = Guid.NewGuid();
        var buildingB = Guid.NewGuid();
        var repo = new InMemoryRentPaymentRepository();
        repo.Payments.Add(MakePayment(companyId, buildingId: buildingA));
        repo.Payments.Add(MakePayment(companyId, buildingId: buildingB));

        var handler = new GetRentPaymentsQueryHandler(repo, new FakeTenantContext { CompanyId = companyId });
        var result = await handler.Handle(new GetRentPaymentsQuery(BuildingId: buildingA), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].BuildingId.Should().Be(buildingA);
    }

    // 3. Filters by status = Paid.
    [Fact]
    public async Task Handle_FiltersByStatus_Paid()
    {
        var companyId = Guid.NewGuid();
        var repo = new InMemoryRentPaymentRepository();
        repo.Payments.Add(MakePayment(companyId, status: DueDateStatus.Paid, amountPaid: 500m));
        repo.Payments.Add(MakePayment(companyId, status: DueDateStatus.Pending, amountPaid: 0m));

        var handler = new GetRentPaymentsQueryHandler(repo, new FakeTenantContext { CompanyId = companyId });
        var result = await handler.Handle(new GetRentPaymentsQuery(Status: DueDateStatus.Paid), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].DueDateStatus.Should().Be(DueDateStatus.Paid);
    }

    // 4. Filters by status = Pending.
    [Fact]
    public async Task Handle_FiltersByStatus_Pending()
    {
        var companyId = Guid.NewGuid();
        var repo = new InMemoryRentPaymentRepository();
        repo.Payments.Add(MakePayment(companyId, status: DueDateStatus.Pending));
        repo.Payments.Add(MakePayment(companyId, status: DueDateStatus.OverdueUnpaid));

        var handler = new GetRentPaymentsQueryHandler(repo, new FakeTenantContext { CompanyId = companyId });
        var result = await handler.Handle(new GetRentPaymentsQuery(Status: DueDateStatus.Pending), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].DueDateStatus.Should().Be(DueDateStatus.Pending);
    }

    // 5. Filters by status = PartiallyPaid.
    [Fact]
    public async Task Handle_FiltersByStatus_PartiallyPaid()
    {
        var companyId = Guid.NewGuid();
        var repo = new InMemoryRentPaymentRepository();
        repo.Payments.Add(MakePayment(companyId, status: DueDateStatus.PartiallyPaid, amountPaid: 200m));
        repo.Payments.Add(MakePayment(companyId, status: DueDateStatus.Paid, amountPaid: 500m));

        var handler = new GetRentPaymentsQueryHandler(repo, new FakeTenantContext { CompanyId = companyId });
        var result = await handler.Handle(new GetRentPaymentsQuery(Status: DueDateStatus.PartiallyPaid), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].DueDateStatus.Should().Be(DueDateStatus.PartiallyPaid);
    }

    // 6. Filters by status = Late.
    [Fact]
    public async Task Handle_FiltersByStatus_Late()
    {
        var companyId = Guid.NewGuid();
        var repo = new InMemoryRentPaymentRepository();
        repo.Payments.Add(MakePayment(companyId, status: DueDateStatus.Late, amountPaid: 200m));
        repo.Payments.Add(MakePayment(companyId, status: DueDateStatus.OverdueUnpaid));

        var handler = new GetRentPaymentsQueryHandler(repo, new FakeTenantContext { CompanyId = companyId });
        var result = await handler.Handle(new GetRentPaymentsQuery(Status: DueDateStatus.Late), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].DueDateStatus.Should().Be(DueDateStatus.Late);
    }

    // 7. Filters by status = OverdueUnpaid.
    [Fact]
    public async Task Handle_FiltersByStatus_OverdueUnpaid()
    {
        var companyId = Guid.NewGuid();
        var repo = new InMemoryRentPaymentRepository();
        repo.Payments.Add(MakePayment(companyId, status: DueDateStatus.OverdueUnpaid));
        repo.Payments.Add(MakePayment(companyId, status: DueDateStatus.Pending));

        var handler = new GetRentPaymentsQueryHandler(repo, new FakeTenantContext { CompanyId = companyId });
        var result = await handler.Handle(new GetRentPaymentsQuery(Status: DueDateStatus.OverdueUnpaid), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].DueDateStatus.Should().Be(DueDateStatus.OverdueUnpaid);
    }

    // 8. Filters by date range.
    [Fact]
    public async Task Handle_FiltersByDateRange()
    {
        var companyId = Guid.NewGuid();
        var repo = new InMemoryRentPaymentRepository();
        repo.Payments.Add(MakePayment(companyId, dueDate: new DateOnly(2026, 1, 15)));
        repo.Payments.Add(MakePayment(companyId, dueDate: new DateOnly(2026, 2, 15)));
        repo.Payments.Add(MakePayment(companyId, dueDate: new DateOnly(2026, 3, 15)));

        var handler = new GetRentPaymentsQueryHandler(repo, new FakeTenantContext { CompanyId = companyId });
        var result = await handler.Handle(new GetRentPaymentsQuery(
            DateFrom: new DateOnly(2026, 2, 1),
            DateTo: new DateOnly(2026, 2, 28)), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].DueDate.Should().Be(new DateOnly(2026, 2, 15));
    }

    // 9. Filters by searchTerm.
    [Fact]
    public async Task Handle_FiltersBySearchTerm()
    {
        var companyId = Guid.NewGuid();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var repo = new InMemoryRentPaymentRepository();
        repo.TenantNames[tenantA] = "Ahmad Al-Omari";
        repo.TenantNames[tenantB] = "Khaled Mansour";

        repo.Payments.Add(MakePayment(companyId, tenantId: tenantA));
        repo.Payments.Add(MakePayment(companyId, tenantId: tenantB));

        var handler = new GetRentPaymentsQueryHandler(repo, new FakeTenantContext { CompanyId = companyId });
        var result = await handler.Handle(new GetRentPaymentsQuery(SearchTerm: "Al-Omari"), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].TenantName.Should().Be("Ahmad Al-Omari");
    }

    // 10. Combines buildingId + status + dateFrom + dateTo.
    [Fact]
    public async Task Handle_CombinesMultipleFilters()
    {
        var companyId = Guid.NewGuid();
        var targetBuilding = Guid.NewGuid();
        var otherBuilding = Guid.NewGuid();

        var repo = new InMemoryRentPaymentRepository();
        // Matching payment
        repo.Payments.Add(MakePayment(companyId, buildingId: targetBuilding, dueDate: new DateOnly(2026, 2, 10), status: DueDateStatus.OverdueUnpaid));
        // Wrong building
        repo.Payments.Add(MakePayment(companyId, buildingId: otherBuilding, dueDate: new DateOnly(2026, 2, 10), status: DueDateStatus.OverdueUnpaid));
        // Wrong status
        repo.Payments.Add(MakePayment(companyId, buildingId: targetBuilding, dueDate: new DateOnly(2026, 2, 10), status: DueDateStatus.Paid, amountPaid: 500m));
        // Wrong date
        repo.Payments.Add(MakePayment(companyId, buildingId: targetBuilding, dueDate: new DateOnly(2026, 4, 10), status: DueDateStatus.OverdueUnpaid));

        var handler = new GetRentPaymentsQueryHandler(repo, new FakeTenantContext { CompanyId = companyId });
        var result = await handler.Handle(new GetRentPaymentsQuery(
            BuildingId: targetBuilding,
            Status: DueDateStatus.OverdueUnpaid,
            DateFrom: new DateOnly(2026, 2, 1),
            DateTo: new DateOnly(2026, 2, 28)), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].BuildingId.Should().Be(targetBuilding);
        result[0].DueDateStatus.Should().Be(DueDateStatus.OverdueUnpaid);
        result[0].DueDate.Should().Be(new DateOnly(2026, 2, 10));
    }

    // 11-18. Returns correct projection fields.
    [Fact]
    public async Task Handle_ReturnsEnrichedDisplayProjectionFields()
    {
        var companyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var apartmentId = Guid.NewGuid();
        var leaseId = Guid.NewGuid();

        var repo = new InMemoryRentPaymentRepository();
        repo.TenantNames[tenantId] = "Omar Hasan";
        repo.BuildingNames[buildingId] = "Al-Andalus Tower";
        repo.ApartmentNumbers[apartmentId] = "101";
        repo.ContractNumbers[leaseId] = "CNT-2026-0042";

        var payment = MakePayment(
            companyId: companyId,
            buildingId: buildingId,
            tenantId: tenantId,
            apartmentId: apartmentId,
            leaseContractId: leaseId,
            amountDue: 750m,
            dueDate: new DateOnly(2026, 5, 1),
            status: DueDateStatus.PartiallyPaid,
            amountPaid: 300m
        );
        repo.Payments.Add(payment);

        var handler = new GetRentPaymentsQueryHandler(repo, new FakeTenantContext { CompanyId = companyId });
        var result = await handler.Handle(new GetRentPaymentsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        var dto = result[0];
        dto.TenantName.Should().Be("Omar Hasan");
        dto.BuildingName.Should().Be("Al-Andalus Tower");
        dto.ApartmentNumber.Should().Be("101");
        dto.ContractNumber.Should().Be("CNT-2026-0042");
        dto.AmountDue.Should().Be(750m);
        dto.AmountPaid.Should().Be(300m);
        dto.DueDate.Should().Be(new DateOnly(2026, 5, 1));
        dto.DueDateStatus.Should().Be(DueDateStatus.PartiallyPaid);
    }

    // 19. Does not expose another company's payment.
    [Fact]
    public async Task Handle_DoesNotExposeAnotherCompanyPayment()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();

        var repo = new InMemoryRentPaymentRepository();
        repo.Payments.Add(MakePayment(companyB));

        var handler = new GetRentPaymentsQueryHandler(repo, new FakeTenantContext { CompanyId = companyA });
        var result = await handler.Handle(new GetRentPaymentsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // 20. Does not expose soft-deleted payment records.
    [Fact]
    public async Task Handle_DoesNotExposeSoftDeletedPayments()
    {
        var companyId = Guid.NewGuid();
        var payment = MakePayment(companyId);
        payment.SoftDelete(DateTimeOffset.UtcNow, Guid.NewGuid());

        var repo = new InMemoryRentPaymentRepository();
        repo.Payments.Add(payment);

        var handler = new GetRentPaymentsQueryHandler(repo, new FakeTenantContext { CompanyId = companyId });
        var result = await handler.Handle(new GetRentPaymentsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // 21-22. Keyset pagination is deterministic and non-overlapping.
    [Fact]
    public async Task Handle_KeysetPagination_IsDeterministicAndNonOverlapping()
    {
        var companyId = Guid.NewGuid();
        var repo = new InMemoryRentPaymentRepository();

        var p1 = MakePayment(companyId, dueDate: new DateOnly(2026, 3, 1));
        var p2 = MakePayment(companyId, dueDate: new DateOnly(2026, 2, 1));
        var p3 = MakePayment(companyId, dueDate: new DateOnly(2026, 1, 1));
        repo.Payments.AddRange(new[] { p1, p2, p3 });

        var handler = new GetRentPaymentsQueryHandler(repo, new FakeTenantContext { CompanyId = companyId });

        // Page 1: 2 items
        var page1 = await handler.Handle(new GetRentPaymentsQuery(PageSize: 2), CancellationToken.None);
        page1.Should().HaveCount(2);
        page1[0].Id.Should().Be(p1.Id);
        page1[1].Id.Should().Be(p2.Id);

        // Page 2: with cursor from last item of page 1
        var page2 = await handler.Handle(new GetRentPaymentsQuery(
            LastSeenDueDate: page1[1].DueDate,
            LastSeenId: page1[1].Id,
            PageSize: 2), CancellationToken.None);

        page2.Should().HaveCount(1);
        page2[0].Id.Should().Be(p3.Id);

        // Intersect check: no overlap between pages
        var overlap = page1.Select(x => x.Id).Intersect(page2.Select(x => x.Id));
        overlap.Should().BeEmpty();
    }

    // 23. Validator tests.
    [Fact]
    public void Validator_RejectsInvalidPageSize()
    {
        var validator = new GetRentPaymentsQueryValidator();
        var resultZero = validator.TestValidate(new GetRentPaymentsQuery(PageSize: 0));
        resultZero.ShouldHaveValidationErrorFor(x => x.PageSize);

        var resultExcessive = validator.TestValidate(new GetRentPaymentsQuery(PageSize: 250));
        resultExcessive.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Validator_RejectsDateToBeforeDateFrom()
    {
        var validator = new GetRentPaymentsQueryValidator();
        var query = new GetRentPaymentsQuery(
            DateFrom: new DateOnly(2026, 5, 1),
            DateTo: new DateOnly(2026, 4, 1));

        var result = validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor("DateTo");
    }

    [Fact]
    public void Validator_RejectsInvalidStatusEnum()
    {
        var validator = new GetRentPaymentsQueryValidator();
        var query = new GetRentPaymentsQuery(Status: (DueDateStatus)999);

        var result = validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    // 24. Empty result is handled correctly.
    [Fact]
    public async Task Handle_NoPayments_ReturnsEmptyList()
    {
        var companyId = Guid.NewGuid();
        var repo = new InMemoryRentPaymentRepository();
        var handler = new GetRentPaymentsQueryHandler(repo, new FakeTenantContext { CompanyId = companyId });

        var result = await handler.Handle(new GetRentPaymentsQuery(), CancellationToken.None);
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
