using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mapster;
using PropertyOS.Application.Financials;
using PropertyOS.Application.Financials.Queries.Common;
using PropertyOS.Application.Financials.Queries.GetRentPaymentById;
using PropertyOS.Domain.Financials;
using PropertyOS.Domain.Financials.Enums;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Financials.Repositories;

public class RentPaymentRepository : IRentPaymentRepository
{
    private readonly PropertyOsDbContext _dbContext;

    public RentPaymentRepository(PropertyOsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RentPayment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.RentPayments
            .Include(p => p.Receipt)
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null, cancellationToken);
    }

    public async Task<List<RentPayment>> GetByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return new List<RentPayment>();

        // Deterministic ascending lock order prevents deadlocks between concurrent
        // allocation writers locking overlapping payment sets.
        var idArray = ids.Distinct().OrderBy(id => id).ToArray();

        return await _dbContext.RentPayments
            .FromSqlRaw(
                "SELECT *, xmin FROM rent_payments WHERE id = ANY({0}) AND deleted_at IS NULL ORDER BY id FOR UPDATE",
                idArray)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RentPayment rentPayment, CancellationToken cancellationToken = default)
    {
        await _dbContext.RentPayments.AddAsync(rentPayment, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<RentPayment> rentPayments, CancellationToken cancellationToken = default)
    {
        await _dbContext.RentPayments.AddRangeAsync(rentPayments, cancellationToken);
    }

    public Task<bool> HasScheduledInstallmentAsync(Guid leaseContractId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
    {
        // DeletedAt filter matches the partial unique index uq_rent_payments_contract_period
        // (filtered ON deleted_at IS NULL): a soft-deleted installment must not block regeneration.
        return _dbContext.RentPayments.AnyAsync(
            p => p.LeaseContractId == leaseContractId
                 && p.PaymentPurpose == PaymentPurpose.ScheduledInstallment
                 && p.BillingPeriodStart == start
                 && p.BillingPeriodEnd == end
                 && p.DeletedAt == null,
            cancellationToken);
    }

    public Task<List<Guid>> GetOverdueCandidateIdsAsync(DateOnly asOfDate, int batchSize, Guid? afterId, CancellationToken cancellationToken = default)
    {
        // Pending or partially-paid scheduled installments whose due date is strictly before
        // the Jordan business date. Deliberately over-approximates (the per-company grace
        // period is not applied here): the per-payment command re-derives precisely with
        // grace and no-ops when nothing changes. Soft-deleted rows are excluded.
        // Keyset cursor over Id: the job advances afterId with the last returned Id per
        // batch and skips poison ids client-side.
        return _dbContext.RentPayments
            .AsNoTracking()
            .Where(p => p.PaymentPurpose == PaymentPurpose.ScheduledInstallment
                        && (p.DueDateStatus == DueDateStatus.Pending || p.DueDateStatus == DueDateStatus.PartiallyPaid)
                        && p.DueDate != null
                        && p.DueDate < asOfDate
                        && p.DeletedAt == null
                        && (afterId == null || p.Id.CompareTo(afterId.Value) > 0))
            .OrderBy(p => p.Id)
            .Take(batchSize)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetRentGracePeriodDaysAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var graceDays = await _dbContext.CompanySettings
            .AsNoTracking()
            .Where(s => s.CompanyId == companyId)
            .Select(s => (short?)s.RentGracePeriodDays)
            .FirstOrDefaultAsync(cancellationToken);

        // Settings rows are auto-created at company signup; the platform default (5 days,
        // CompanySettings' own default) covers the defensive missing-row case.
        return graceDays ?? 5;
    }

    public Task<List<BillingPeriod>> GetScheduledInstallmentPeriodsAsync(Guid leaseContractId, CancellationToken cancellationToken = default)
    {
        return _dbContext.RentPayments
            .AsNoTracking()
            .Where(p => p.LeaseContractId == leaseContractId
                        && p.PaymentPurpose == PaymentPurpose.ScheduledInstallment
                        && p.BillingPeriodStart != null
                        && p.BillingPeriodEnd != null
                        && p.DeletedAt == null)
            .Select(p => new BillingPeriod(p.BillingPeriodStart!.Value, p.BillingPeriodEnd!.Value))
            .ToListAsync(cancellationToken);
    }

    public Task<ChequeDetails?> LoadChequeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.ChequeDetails.FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null, cancellationToken);
    }

    public async Task AddChequeAsync(ChequeDetails cheque, CancellationToken cancellationToken = default)
    {
        await _dbContext.ChequeDetails.AddAsync(cheque, cancellationToken);
    }

    public Task<PaymentAllocation?> LoadAllocationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.PaymentAllocations.FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null, cancellationToken);
    }

    public async Task AddAllocationAsync(PaymentAllocation allocation, CancellationToken cancellationToken = default)
    {
        await _dbContext.PaymentAllocations.AddAsync(allocation, cancellationToken);
    }

    public async Task AddReceiptAsync(RentPaymentReceipt receipt, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<RentPaymentReceipt>().AddAsync(receipt, cancellationToken);
    }

    public Task<List<PaymentAllocation>> GetAllocationsByObligationIdAsync(Guid obligationId, CancellationToken cancellationToken = default)
    {
        // Soft-deleted allocations must never count toward settlement totals.
        return _dbContext.PaymentAllocations
            .Where(a => a.ObligationPaymentId == obligationId && a.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public Task<List<PaymentAllocation>> GetAllocationsByReceivingIdAsync(Guid receivingId, CancellationToken cancellationToken = default)
    {
        return _dbContext.PaymentAllocations
            .Where(a => a.ReceivingPaymentId == receivingId && a.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<RentPaymentDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        // Tenant scoping on the aggregate root: cross-tenant ids fall through to null (404),
        // and the child queries below only run for a payment proven to belong to companyId.
        var paymentDto = await _dbContext.RentPayments
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId)
            .ProjectToType<RentPaymentDetailDto>()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (paymentDto == null)
            return null;

        paymentDto.ChequeDetails = await _dbContext.ChequeDetails
            .AsNoTracking()
            .ProjectToType<ChequeDetailDto>()
            .FirstOrDefaultAsync(c => c.RentPaymentId == id, cancellationToken);

        paymentDto.IncomingAllocations = await _dbContext.PaymentAllocations
            .AsNoTracking()
            .Where(a => a.ReceivingPaymentId == id)
            .ProjectToType<PaymentAllocationDto>()
            .ToListAsync(cancellationToken);

        paymentDto.OutgoingAllocations = await _dbContext.PaymentAllocations
            .AsNoTracking()
            .Where(a => a.ObligationPaymentId == id)
            .ProjectToType<PaymentAllocationDto>()
            .ToListAsync(cancellationToken);

        return paymentDto;
    }

    public Task<List<RentPaymentDto>> GetPaymentsForLeaseAsync(Guid leaseContractId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _dbContext.RentPayments
            .AsNoTracking()
            .Where(p => p.LeaseContractId == leaseContractId && p.CompanyId == companyId)
            .OrderBy(p => p.DueDate)
            .ProjectToType<RentPaymentDto>()
            .ToListAsync(cancellationToken);
    }

    public Task<List<RentPaymentDto>> GetPaymentsForTenantAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _dbContext.RentPayments
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.CompanyId == companyId)
            .ProjectToType<RentPaymentDto>()
            .ToListAsync(cancellationToken);
    }

    public Task<List<RentPaymentDto>> SearchPaymentsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default)
    {
        var effectivePageSize = Math.Clamp(pageSize, 1, 200);

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return _dbContext.RentPayments
                .AsNoTracking()
                .Where(p => p.CompanyId == companyId)
                .OrderByDescending(p => p.CreatedAt)
                .ThenBy(p => p.Id)
                .Take(effectivePageSize)
                .ProjectToType<RentPaymentDto>()
                .ToListAsync(cancellationToken);
        }

        var normalizedSearch = searchTerm.Trim().ToLower();

        return (from p in _dbContext.RentPayments
                join c in _dbContext.LeaseContracts on p.LeaseContractId equals c.Id
                join t in _dbContext.Tenants on p.TenantId equals t.Id
                where p.CompanyId == companyId &&
                      ((p.ReceiptNumber != null && EF.Functions.ILike(p.ReceiptNumber, $"%{normalizedSearch}%")) ||
                       EF.Functions.ILike(c.ContractNumber, $"%{normalizedSearch}%") ||
                       EF.Functions.ILike(t.Name, $"%{normalizedSearch}%"))
                select p)
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ThenBy(p => p.Id)
                .Take(effectivePageSize)
                .ProjectToType<RentPaymentDto>()
                .ToListAsync(cancellationToken);
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

        return _dbContext.RentPayments
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId && outstandingStatuses.Contains(p.DueDateStatus))
            .OrderBy(p => p.DueDate)
            .ThenBy(p => p.Id)
            .Take(effectivePageSize)
            .ProjectToType<RentPaymentDto>()
            .ToListAsync(cancellationToken);
    }

    public Task<List<ChequeDetailDto>> GetChequesByStatusAsync(ChequeStatus status, Guid companyId, int pageSize, CancellationToken cancellationToken = default)
    {
        var effectivePageSize = Math.Clamp(pageSize, 1, 200);

        return _dbContext.ChequeDetails
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.Status == status)
            .OrderBy(c => c.DueDate)
            .ThenBy(c => c.Id)
            .Take(effectivePageSize)
            .ProjectToType<ChequeDetailDto>()
            .ToListAsync(cancellationToken);
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

        return _dbContext.ChequeDetails
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId
                        && upcomingStatuses.Contains(c.Status)
                        && c.DueDate >= today
                        && c.DueDate <= limitDate)
            .OrderBy(c => c.DueDate)
            .ProjectToType<ChequeDetailDto>()
            .ToListAsync(cancellationToken);
    }

    // ── Rent Payment Receipt read-side ───────────────────────────────────────

    public Task<RentPaymentReceiptDto?> GetReceiptByRentPaymentIdAsync(
        Guid rentPaymentId,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.RentPaymentReceipts
            .AsNoTracking()
            .Where(r => r.RentPaymentId == rentPaymentId && r.CompanyId == companyId && r.DeletedAt == null)
            .ProjectToType<RentPaymentReceiptDto>()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<RentPaymentReceiptDto>> GetReceiptsAsync(
        RentPaymentReceiptFilterOptions filter,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        // Join receipts → payments to allow filtering by LeaseContractId / TenantId
        var query =
            from r in _dbContext.RentPaymentReceipts
            join p in _dbContext.RentPayments on r.RentPaymentId equals p.Id
            where r.CompanyId == companyId && r.DeletedAt == null
            select new { r, p };

        // ── Filters ─────────────────────────────────────────────────────────
        if (filter.LeaseContractId.HasValue)
            query = query.Where(x => x.p.LeaseContractId == filter.LeaseContractId.Value);

        if (filter.TenantId.HasValue)
            query = query.Where(x => x.p.TenantId == filter.TenantId.Value);

        if (filter.DateFrom.HasValue)
            query = query.Where(x => x.r.IssueDate >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(x => x.r.IssueDate <= filter.DateTo.Value);

        // ── Keyset pagination cursor: (IssueDate DESC, Id ASC) ───────────────
        if (filter.LastSeenId.HasValue && filter.LastSeenIssueDate.HasValue)
        {
            var cursorDate = filter.LastSeenIssueDate.Value;
            var cursorId   = filter.LastSeenId.Value;

            query = query.Where(x =>
                x.r.IssueDate < cursorDate ||
                (x.r.IssueDate == cursorDate && x.r.Id.CompareTo(cursorId) > 0));
        }

        return query
            .OrderByDescending(x => x.r.IssueDate)
            .ThenBy(x => x.r.Id)
            .Take(Math.Clamp(filter.PageSize, 1, 200))
            .Select(x => x.r)
            .ProjectToType<RentPaymentReceiptDto>()
            .ToListAsync(cancellationToken);
    }
}

