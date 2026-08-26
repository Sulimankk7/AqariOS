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

        var dbPayments = await _dbContext.RentPayments
            .FromSqlRaw(
                "SELECT *, xmin FROM rent_payments WHERE id = ANY({0}) AND deleted_at IS NULL ORDER BY id FOR UPDATE",
                idArray)
            .ToListAsync(cancellationToken);

        // Include any newly added entities in the current transaction (in ChangeTracker / Local)
        // that have not yet been flushed to PostgreSQL via SaveChangesAsync.
        var missingIds = ids.Except(dbPayments.Select(p => p.Id)).ToHashSet();
        if (missingIds.Count > 0)
        {
            var localPayments = _dbContext.RentPayments.Local
                .Where(p => missingIds.Contains(p.Id) && p.DeletedAt == null)
                .ToList();

            if (localPayments.Count > 0)
            {
                dbPayments.AddRange(localPayments);
            }
        }

        return dbPayments;
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

    public async Task<List<RentPaymentDto>> GetPaymentsForLeaseAsync(Guid leaseContractId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.RentPayments
            .AsNoTracking()
            .Where(p => p.LeaseContractId == leaseContractId && p.CompanyId == companyId)
            .OrderBy(p => p.DueDate)
            .ProjectToType<RentPaymentDto>()
            .ToListAsync(cancellationToken);

        await PopulateTransactionReceiptsAndSettlementAsync(list, cancellationToken);
        return list;
    }

    public async Task<List<RentPaymentDto>> GetPaymentsForTenantAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var baseQuery = _dbContext.RentPayments
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId 
                     && p.CompanyId == companyId 
                     && p.PaymentPurpose == PaymentPurpose.ScheduledInstallment 
                     && p.DeletedAt == null)
            .OrderByDescending(p => p.DueDate)
            .ThenByDescending(p => p.CreatedAt);

        var list = await (from p in baseQuery
                join lc in _dbContext.LeaseContracts on p.LeaseContractId equals lc.Id into lcs from lc in lcs.DefaultIfEmpty()
                join t  in _dbContext.Tenants         on p.TenantId        equals t.Id  into ts  from t  in ts.DefaultIfEmpty()
                join a  in _dbContext.Apartments      on p.ApartmentId     equals a.Id  into ax  from a  in ax.DefaultIfEmpty()
                join b  in _dbContext.Buildings       on p.BuildingId      equals b.Id  into bx  from b  in bx.DefaultIfEmpty()
                join r  in _dbContext.RentPaymentReceipts on p.Id equals r.RentPaymentId into rs from r in rs.Where(x => x.DeletedAt == null).DefaultIfEmpty()
                select new RentPaymentDto
                {
                    Id                    = p.Id,
                    CompanyId             = p.CompanyId,
                    LeaseContractId       = p.LeaseContractId,
                    TenantId              = p.TenantId,
                    BuildingId            = p.BuildingId,
                    ApartmentId           = p.ApartmentId,
                    PaymentPurpose        = p.PaymentPurpose,
                    AmountDue             = p.AmountDue,
                    AmountPaid            = p.AmountPaid,
                    Currency              = p.Currency,
                    DueDateStatus         = p.DueDateStatus,
                    BillingPeriodStart    = p.BillingPeriodStart,
                    BillingPeriodEnd      = p.BillingPeriodEnd,
                    DueDate               = p.DueDate,
                    ReceiptNumber         = r != null ? r.ReceiptNumber : p.ReceiptNumber,
                    PaymentMethod         = p.PaymentMethod,
                    PaymentReferenceNumber = p.PaymentReferenceNumber,
                    Notes                 = p.Notes,
                    CreatedAt             = p.CreatedAt,
                    UpdatedAt             = p.UpdatedAt,
                    CreatedBy             = p.CreatedBy,
                    UpdatedBy             = p.UpdatedBy,
                    TenantName            = t != null ? t.Name : null,
                    BuildingName          = b != null ? b.Name : null,
                    ApartmentNumber       = a != null ? a.UnitNumber : null,
                    ContractNumber        = lc != null ? lc.ContractNumber : null,
                    LatestSubmissionStatus = p.Submissions.Where(s => s.DeletedAt == null).OrderByDescending(s => s.CreatedAt).Select(s => (SubmissionStatus?)s.Status).FirstOrDefault(),
                    LatestSubmissionRejectionReason = p.Submissions.Where(s => s.DeletedAt == null).OrderByDescending(s => s.CreatedAt).Select(s => s.RejectionReason).FirstOrDefault(),
                    LatestSubmissionAmount = p.Submissions.Where(s => s.DeletedAt == null).OrderByDescending(s => s.CreatedAt).Select(s => s.Amount).FirstOrDefault(),
                    LatestSubmissionDate = p.Submissions.Where(s => s.DeletedAt == null).OrderByDescending(s => s.CreatedAt).Select(s => (DateTimeOffset?)s.CreatedAt).FirstOrDefault(),
                    ReceiptFileId         = r != null ? r.FileId : null,
                })
                .ToListAsync(cancellationToken);

        await PopulateTransactionReceiptsAndSettlementAsync(list, cancellationToken);
        return list;
    }

    public async Task<List<RentPaymentDto>> SearchPaymentsAsync(string searchTerm, Guid companyId, int pageSize, CancellationToken cancellationToken = default)
    {
        var effectivePageSize = Math.Clamp(pageSize, 1, 200);

        IQueryable<PropertyOS.Domain.Financials.RentPayment> baseQuery;

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            baseQuery = _dbContext.RentPayments
                .AsNoTracking()
                .Where(p => p.CompanyId == companyId && p.DeletedAt == null)
                .OrderByDescending(p => p.CreatedAt)
                .ThenBy(p => p.Id)
                .Take(effectivePageSize);
        }
        else
        {
            var normalizedSearch = searchTerm.Trim().ToLower();
            baseQuery = (from p in _dbContext.RentPayments
                    join lc in _dbContext.LeaseContracts on p.LeaseContractId equals lc.Id
                    join t  in _dbContext.Tenants         on p.TenantId        equals t.Id
                    where p.CompanyId == companyId
                          && p.DeletedAt == null
                          && ((p.ReceiptNumber != null && EF.Functions.ILike(p.ReceiptNumber, $"%{normalizedSearch}%")) ||
                               EF.Functions.ILike(lc.ContractNumber, $"%{normalizedSearch}%") ||
                               EF.Functions.ILike(t.Name, $"%{normalizedSearch}%"))
                    select p)
                    .AsNoTracking()
                    .OrderByDescending(p => p.CreatedAt)
                    .ThenBy(p => p.Id)
                    .Take(effectivePageSize);
        }

        var list = await (from p in baseQuery
                join lc in _dbContext.LeaseContracts on p.LeaseContractId equals lc.Id into lcs from lc in lcs.DefaultIfEmpty()
                join t  in _dbContext.Tenants         on p.TenantId        equals t.Id  into ts  from t  in ts.DefaultIfEmpty()
                join a  in _dbContext.Apartments      on p.ApartmentId     equals a.Id  into ax  from a  in ax.DefaultIfEmpty()
                join b  in _dbContext.Buildings       on p.BuildingId      equals b.Id  into bx  from b  in bx.DefaultIfEmpty()
                select new RentPaymentDto
                {
                    Id                    = p.Id,
                    CompanyId             = p.CompanyId,
                    LeaseContractId       = p.LeaseContractId,
                    TenantId              = p.TenantId,
                    BuildingId            = p.BuildingId,
                    ApartmentId           = p.ApartmentId,
                    PaymentPurpose        = p.PaymentPurpose,
                    AmountDue             = p.AmountDue,
                    AmountPaid            = p.AmountPaid,
                    Currency              = p.Currency,
                    DueDateStatus         = p.DueDateStatus,
                    BillingPeriodStart    = p.BillingPeriodStart,
                    BillingPeriodEnd      = p.BillingPeriodEnd,
                    DueDate               = p.DueDate,
                    ReceiptNumber         = p.ReceiptNumber,
                    PaymentMethod         = p.PaymentMethod,
                    PaymentReferenceNumber = p.PaymentReferenceNumber,
                    Notes                 = p.Notes,
                    CreatedAt             = p.CreatedAt,
                    UpdatedAt             = p.UpdatedAt,
                    CreatedBy             = p.CreatedBy,
                    UpdatedBy             = p.UpdatedBy,
                    TenantName            = t != null ? t.Name : null,
                    BuildingName          = b != null ? b.Name : null,
                    ApartmentNumber       = a != null ? a.UnitNumber : null,
                    ContractNumber        = lc != null ? lc.ContractNumber : null,
                })
                .ToListAsync(cancellationToken);

        await PopulateTransactionReceiptsAndSettlementAsync(list, cancellationToken);
        return list;
    }

    public async Task<List<RentPaymentDto>> GetOutstandingPaymentsAsync(Guid companyId, int pageSize, CancellationToken cancellationToken = default)
    {
        var effectivePageSize = Math.Clamp(pageSize, 1, 200);

        var outstandingStatuses = new[]
        {
            DueDateStatus.Pending,
            DueDateStatus.PartiallyPaid,
            DueDateStatus.Late,
            DueDateStatus.OverdueUnpaid
        };

        var baseQuery = _dbContext.RentPayments
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId
                        && p.PaymentPurpose == PaymentPurpose.ScheduledInstallment
                        && p.DeletedAt == null
                        && outstandingStatuses.Contains(p.DueDateStatus))
            .OrderBy(p => p.DueDate)
            .ThenBy(p => p.Id)
            .Take(effectivePageSize);

        var list = await (from p in baseQuery
                join lc in _dbContext.LeaseContracts on p.LeaseContractId equals lc.Id into lcs from lc in lcs.DefaultIfEmpty()
                join t  in _dbContext.Tenants         on p.TenantId        equals t.Id  into ts  from t  in ts.DefaultIfEmpty()
                join a  in _dbContext.Apartments      on p.ApartmentId     equals a.Id  into ax  from a  in ax.DefaultIfEmpty()
                join b  in _dbContext.Buildings       on p.BuildingId      equals b.Id  into bx  from b  in bx.DefaultIfEmpty()
                select new RentPaymentDto
                {
                    Id                    = p.Id,
                    CompanyId             = p.CompanyId,
                    LeaseContractId       = p.LeaseContractId,
                    TenantId              = p.TenantId,
                    BuildingId            = p.BuildingId,
                    ApartmentId           = p.ApartmentId,
                    PaymentPurpose        = p.PaymentPurpose,
                    AmountDue             = p.AmountDue,
                    AmountPaid            = p.AmountPaid,
                    Currency              = p.Currency,
                    DueDateStatus         = p.DueDateStatus,
                    BillingPeriodStart    = p.BillingPeriodStart,
                    BillingPeriodEnd      = p.BillingPeriodEnd,
                    DueDate               = p.DueDate,
                    ReceiptNumber         = p.ReceiptNumber,
                    PaymentMethod         = p.PaymentMethod,
                    PaymentReferenceNumber = p.PaymentReferenceNumber,
                    Notes                 = p.Notes,
                    CreatedAt             = p.CreatedAt,
                    UpdatedAt             = p.UpdatedAt,
                    CreatedBy             = p.CreatedBy,
                    UpdatedBy             = p.UpdatedBy,
                    TenantName            = t != null ? t.Name : null,
                    BuildingName          = b != null ? b.Name : null,
                    ApartmentNumber       = a != null ? a.UnitNumber : null,
                    ContractNumber        = lc != null ? lc.ContractNumber : null,
                })
                .ToListAsync(cancellationToken);

        await PopulateTransactionReceiptsAndSettlementAsync(list, cancellationToken);
        return list;
    }

    public async Task<List<RentPaymentDto>> GetPaymentsAsync(
        RentPaymentFilterOptions filter,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = from p in _dbContext.RentPayments
                        join lc in _dbContext.LeaseContracts on p.LeaseContractId equals lc.Id into lcs from lc in lcs.DefaultIfEmpty()
                        join t  in _dbContext.Tenants         on p.TenantId        equals t.Id  into ts  from t  in ts.DefaultIfEmpty()
                        join a  in _dbContext.Apartments      on p.ApartmentId     equals a.Id  into ax  from a  in ax.DefaultIfEmpty()
                        join b  in _dbContext.Buildings       on p.BuildingId      equals b.Id  into bx  from b  in bx.DefaultIfEmpty()
                        where p.CompanyId == companyId && p.DeletedAt == null
                        select new { p, lc, t, a, b };

        if (filter.BuildingId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.p.BuildingId == filter.BuildingId.Value);
        }

        if (filter.Status.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.p.DueDateStatus == filter.Status.Value);
        }

        if (filter.DateFrom.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.p.DueDate.HasValue && x.p.DueDate.Value >= filter.DateFrom.Value);
        }

        if (filter.DateTo.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.p.DueDate.HasValue && x.p.DueDate.Value <= filter.DateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var normalizedSearch = filter.SearchTerm.Trim();
            baseQuery = baseQuery.Where(x =>
                (x.p.ReceiptNumber != null && EF.Functions.ILike(x.p.ReceiptNumber, $"%{normalizedSearch}%")) ||
                (x.lc != null && EF.Functions.ILike(x.lc.ContractNumber, $"%{normalizedSearch}%")) ||
                (x.t != null && EF.Functions.ILike(x.t.Name, $"%{normalizedSearch}%")));
        }

        if (filter.LastSeenId.HasValue && filter.LastSeenDueDate.HasValue)
        {
            var cursorDate = filter.LastSeenDueDate.Value;
            var cursorId   = filter.LastSeenId.Value;

            baseQuery = baseQuery.Where(x =>
                (x.p.DueDate.HasValue && x.p.DueDate.Value < cursorDate) ||
                (x.p.DueDate.HasValue && x.p.DueDate.Value == cursorDate && x.p.Id.CompareTo(cursorId) > 0));
        }

        var effectivePageSize = Math.Clamp(filter.PageSize, 1, 200);

        var list = await baseQuery
            .AsNoTracking()
            .OrderByDescending(x => x.p.DueDate)
            .ThenBy(x => x.p.Id)
            .Take(effectivePageSize)
            .Select(x => new RentPaymentDto
            {
                Id                     = x.p.Id,
                CompanyId              = x.p.CompanyId,
                LeaseContractId        = x.p.LeaseContractId,
                TenantId               = x.p.TenantId,
                BuildingId             = x.p.BuildingId,
                ApartmentId            = x.p.ApartmentId,
                PaymentPurpose         = x.p.PaymentPurpose,
                AmountDue              = x.p.AmountDue,
                AmountPaid             = x.p.AmountPaid,
                Currency               = x.p.Currency,
                DueDateStatus          = x.p.DueDateStatus,
                BillingPeriodStart     = x.p.BillingPeriodStart,
                BillingPeriodEnd       = x.p.BillingPeriodEnd,
                DueDate                = x.p.DueDate,
                ReceiptNumber          = x.p.ReceiptNumber,
                PaymentMethod          = x.p.PaymentMethod,
                PaymentReferenceNumber = x.p.PaymentReferenceNumber,
                Notes                  = x.p.Notes,
                CreatedAt              = x.p.CreatedAt,
                UpdatedAt              = x.p.UpdatedAt,
                CreatedBy              = x.p.CreatedBy,
                UpdatedBy              = x.p.UpdatedBy,
                TenantName             = x.t != null ? x.t.Name : null,
                BuildingName           = x.b != null ? x.b.Name : null,
                ApartmentNumber        = x.a != null ? x.a.UnitNumber : null,
                ContractNumber         = x.lc != null ? x.lc.ContractNumber : null,
            })
            .ToListAsync(cancellationToken);

        await PopulateTransactionReceiptsAndSettlementAsync(list, cancellationToken);
        return list;
    }

    private async Task PopulateTransactionReceiptsAndSettlementAsync(
        List<RentPaymentDto> dtos,
        CancellationToken cancellationToken)
    {
        if (dtos.Count == 0) return;

        var obligationIds = dtos.Select(d => d.Id).ToList();

        var allocations = await (
            from a in _dbContext.PaymentAllocations.AsNoTracking()
            where obligationIds.Contains(a.ObligationPaymentId) 
               && a.AllocationStatus == AllocationStatus.Active 
               && a.DeletedAt == null
            join rcv in _dbContext.RentPayments.AsNoTracking() on a.ReceivingPaymentId equals rcv.Id
            join r in _dbContext.RentPaymentReceipts.AsNoTracking() on rcv.Id equals r.RentPaymentId into rs 
            from r in rs.Where(x => x.DeletedAt == null).DefaultIfEmpty()
            orderby a.AllocationDate, a.CreatedAt
            select new
            {
                ObligationId = a.ObligationPaymentId,
                ReceivingPaymentId = a.ReceivingPaymentId,
                AllocatedAmount = a.AllocatedAmount,
                AllocationDate = a.AllocationDate,
                CreatedAt = a.CreatedAt,
                PaymentMethod = rcv.PaymentMethod,
                ReferenceNumber = rcv.PaymentReferenceNumber,
                ReceiptId = r != null ? (Guid?)r.Id : null,
                ReceiptNumber = r != null ? r.ReceiptNumber : null,
                FileId = r != null ? r.FileId : null,
                IssueDate = r != null ? (DateTimeOffset?)r.CreatedAt : null,
                ReceiptAmount = (r != null && r.RentPaymentId == a.ReceivingPaymentId) ? (decimal?)r.Amount : null
            }
        ).ToListAsync(cancellationToken);

        var directReceipts = await (
            from r in _dbContext.RentPaymentReceipts.AsNoTracking()
            where obligationIds.Contains(r.RentPaymentId) && r.DeletedAt == null
            select new
            {
                ObligationId = r.RentPaymentId,
                ReceiptId = r.Id,
                ReceiptNumber = r.ReceiptNumber,
                FileId = r.FileId,
                IssueDate = r.CreatedAt,
                ReceiptAmount = r.Amount
            }
        ).ToListAsync(cancellationToken);

        var allocationsByObligation = allocations.GroupBy(x => x.ObligationId).ToDictionary(g => g.Key, g => g.ToList());
        var directReceiptsByObligation = directReceipts
            .GroupBy(r => r.ObligationId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.IssueDate).First());

        foreach (var dto in dtos)
        {
            var txReceipts = new List<TransactionReceiptDto>();
            decimal previouslyPaid = 0m;

            if (allocationsByObligation.TryGetValue(dto.Id, out var allocs) && allocs.Count > 0)
            {
                foreach (var alloc in allocs)
                {
                    // The authoritative transaction amount is the exact allocated money-in for this obligation
                    var amount = alloc.AllocatedAmount > 0
                        ? alloc.AllocatedAmount
                        : (alloc.ReceiptAmount ?? 0m);

                    var remainingAfter = Math.Max(0, dto.AmountDue - (previouslyPaid + amount));

                    txReceipts.Add(new TransactionReceiptDto
                    {
                        ReceiptId = alloc.ReceiptId ?? Guid.Empty,
                        ReceiptNumber = !string.IsNullOrWhiteSpace(alloc.ReceiptNumber)
                            ? alloc.ReceiptNumber
                            : (allocs.Count == 1 ? (dto.ReceiptNumber ?? "REC-PENDING") : "REC-PENDING"),
                        Amount = amount,
                        IssuedAt = alloc.IssueDate ?? alloc.CreatedAt,
                        FileId = alloc.FileId,
                        PaymentMethod = alloc.PaymentMethod ?? dto.PaymentMethod,
                        ReferenceNumber = alloc.ReferenceNumber ?? dto.PaymentReferenceNumber,
                        PreviouslyPaid = previouslyPaid,
                        RemainingAfter = remainingAfter
                    });

                    previouslyPaid += amount;
                }
            }
            else if (directReceiptsByObligation.TryGetValue(dto.Id, out var direct))
            {
                txReceipts.Add(new TransactionReceiptDto
                {
                    ReceiptId = direct.ReceiptId,
                    ReceiptNumber = direct.ReceiptNumber,
                    Amount = direct.ReceiptAmount,
                    IssuedAt = direct.IssueDate,
                    FileId = direct.FileId,
                    PaymentMethod = dto.PaymentMethod,
                    ReferenceNumber = dto.PaymentReferenceNumber,
                    PreviouslyPaid = 0m,
                    RemainingAfter = Math.Max(0, dto.AmountDue - direct.ReceiptAmount)
                });
            }
            else if (!string.IsNullOrWhiteSpace(dto.ReceiptNumber))
            {
                txReceipts.Add(new TransactionReceiptDto
                {
                    ReceiptId = dto.Id,
                    ReceiptNumber = dto.ReceiptNumber,
                    Amount = dto.AmountPaid > 0 ? dto.AmountPaid : dto.AmountDue,
                    IssuedAt = dto.UpdatedAt,
                    FileId = dto.ReceiptFileId,
                    PaymentMethod = dto.PaymentMethod,
                    ReferenceNumber = dto.PaymentReferenceNumber,
                    PreviouslyPaid = 0m,
                    RemainingAfter = Math.Max(0, dto.AmountDue - dto.AmountPaid)
                });
            }

            dto.TransactionReceipts = txReceipts;

            if (dto.ReceiptFileId == null && txReceipts.Count > 0)
            {
                dto.ReceiptFileId = txReceipts.LastOrDefault(t => t.FileId.HasValue)?.FileId;
            }

            bool isSettled = dto.DueDateStatus == DueDateStatus.Paid && dto.AmountPaid == dto.AmountDue;
            dto.SettlementSummary = new SettlementStatementSummaryDto
            {
                IsAvailable = isSettled,
                TotalDue = dto.AmountDue,
                TotalPaid = dto.AmountPaid,
                Remaining = Math.Max(0, dto.AmountDue - dto.AmountPaid),
                TransactionCount = txReceipts.Count,
                SettledAt = isSettled ? dto.UpdatedAt : null
            };
        }
    }

    public async Task<PropertyOS.Application.Common.Models.KeysetPage<PropertyOS.Application.Financials.Queries.GetPendingPaymentVerifications.PaymentVerificationQueueItemDto>> GetPendingVerificationsAsync(Guid companyId, int pageSize, DateTimeOffset? lastSeenSubmittedAt, Guid? lastSeenId, CancellationToken cancellationToken = default)
    {
        var effectivePageSize = Math.Clamp(pageSize, 1, 200);

        var query = from p in _dbContext.RentPayments
                    join s in _dbContext.Set<PropertyOS.Domain.Financials.PaymentSubmission>()
                        on p.Id equals s.RentPaymentId
                    join t in _dbContext.Tenants on p.TenantId equals t.Id into ts
                    from t in ts.DefaultIfEmpty()
                    join b in _dbContext.Buildings on p.BuildingId equals b.Id into bs
                    from b in bs.DefaultIfEmpty()
                    join a in _dbContext.Apartments on p.ApartmentId equals a.Id into ast
                    from a in ast.DefaultIfEmpty()
                    join lc in _dbContext.LeaseContracts on p.LeaseContractId equals lc.Id into lcs
                    from lc in lcs.DefaultIfEmpty()
                    where p.CompanyId == companyId
                          && p.DeletedAt == null
                          && p.DueDateStatus == DueDateStatus.PendingVerification
                          && s.Status == SubmissionStatus.Pending
                    select new PropertyOS.Application.Financials.Queries.GetPendingPaymentVerifications.PaymentVerificationQueueItemDto
                    {
                        RentPaymentId = p.Id,
                        PaymentSubmissionId = s.Id,
                        TenantId = p.TenantId,
                        TenantName = t != null ? t.Name : null,
                        BuildingId = p.BuildingId,
                        BuildingName = b != null ? b.Name : null,
                        ApartmentId = p.ApartmentId,
                        ApartmentNumber = a != null ? a.UnitNumber : null,
                        LeaseContractId = p.LeaseContractId,
                        ContractNumber = lc != null ? lc.ContractNumber : null,
                        AmountDue = p.AmountDue,
                        AmountPaid = p.AmountPaid,
                        SubmittedAmount = s.Amount ?? (p.AmountDue - p.AmountPaid),
                        Currency = p.Currency,
                        PaymentMethod = s.PaymentMethod,
                        ReferenceNumber = s.ReferenceNumber,
                        ProofFileId = s.ProofFileId,
                        ChequeNumber = s.ChequeNumber,
                        BankName = s.BankName,
                        ChequeIssueDate = s.ChequeIssueDate,
                        ChequeDueDate = s.ChequeDueDate,
                        SubmittedAt = s.SubmittedAt,
                        SubmissionStatus = s.Status,
                        DueDateStatus = p.DueDateStatus
                    };

        if (lastSeenSubmittedAt.HasValue && lastSeenId.HasValue)
        {
            query = query.Where(x => x.SubmittedAt < lastSeenSubmittedAt.Value || (x.SubmittedAt == lastSeenSubmittedAt.Value && x.PaymentSubmissionId < lastSeenId.Value));
        }

        var items = await query
            .AsNoTracking()
            .OrderByDescending(x => x.SubmittedAt)
            .ThenByDescending(x => x.PaymentSubmissionId)
            .Take(effectivePageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > effectivePageSize;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        var nextCursor = items.Count > 0 
            ? PropertyOS.Application.Common.Models.KeysetCursor.Encode(items[^1].SubmittedAt, items[^1].PaymentSubmissionId) 
            : null;

        return new PropertyOS.Application.Common.Models.KeysetPage<PropertyOS.Application.Financials.Queries.GetPendingPaymentVerifications.PaymentVerificationQueueItemDto>(items, nextCursor, hasMore);
    }

    public Task<List<ChequeDetailDto>> GetChequesAsync(ChequeStatus? status, Guid companyId, int pageSize, CancellationToken cancellationToken = default)
    {
        var effectivePageSize = Math.Clamp(pageSize, 1, 200);

        var baseQuery = _dbContext.ChequeDetails
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.DeletedAt == null);

        if (status.HasValue)
            baseQuery = baseQuery.Where(c => c.Status == status.Value);

        baseQuery = baseQuery
            .OrderBy(c => c.DueDate)
            .ThenBy(c => c.Id)
            .Take(effectivePageSize);

        return BuildEnrichedChequeQuery(baseQuery).ToListAsync(cancellationToken);
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

        var baseQuery = _dbContext.ChequeDetails
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId
                        && c.DeletedAt == null
                        && upcomingStatuses.Contains(c.Status)
                        && c.DueDate >= today
                        && c.DueDate <= limitDate)
            .OrderBy(c => c.DueDate);

        return BuildEnrichedChequeQuery(baseQuery).ToListAsync(cancellationToken);
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    private IQueryable<ChequeDetailDto> BuildEnrichedChequeQuery(
        IQueryable<PropertyOS.Domain.Financials.ChequeDetails> baseQuery)
    {
        return from c  in baseQuery
               join rp in _dbContext.RentPayments   on c.RentPaymentId   equals rp.Id into rps from rp in rps.DefaultIfEmpty()
               join lc in _dbContext.LeaseContracts on c.LeaseContractId equals lc.Id into lcs from lc in lcs.DefaultIfEmpty()
               join t  in _dbContext.Tenants         on c.TenantId        equals t.Id  into ts  from t  in ts.DefaultIfEmpty()
               join a  in _dbContext.Apartments      on (rp != null ? rp.ApartmentId : Guid.Empty) equals a.Id into ax from a in ax.DefaultIfEmpty()
               join b  in _dbContext.Buildings       on (rp != null ? rp.BuildingId  : Guid.Empty) equals b.Id into bx from b in bx.DefaultIfEmpty()
               select new ChequeDetailDto
               {
                   Id                  = c.Id,
                   CompanyId           = c.CompanyId,
                   RentPaymentId       = c.RentPaymentId,
                   LeaseContractId     = c.LeaseContractId,
                   TenantId            = c.TenantId,
                   ChequeNumber        = c.ChequeNumber,
                   BankName            = c.BankName,
                   BankBranch          = c.BankBranch,
                   IssueDate           = c.IssueDate,
                   DueDate             = c.DueDate,
                   Amount              = c.Amount,
                   Currency            = c.Currency,
                   Status              = c.Status,
                   ReceivedDate        = c.ReceivedDate,
                   DepositDate         = c.DepositDate,
                   ClearanceDate       = c.ClearanceDate,
                   BounceDate          = c.BounceDate,
                   BounceReason        = c.BounceReason,
                   BounceFeeCharged    = c.BounceFeeCharged,
                   CancellationReason  = c.CancellationReason,
                   ReplacementChequeId = c.ReplacementChequeId,
                   Notes               = c.Notes,
                   CreatedAt           = c.CreatedAt,
                   UpdatedAt           = c.UpdatedAt,
                   CreatedBy           = c.CreatedBy,
                   UpdatedBy           = c.UpdatedBy,
                   TenantName          = t != null ? t.Name : null,
                   BuildingName        = b != null ? b.Name : null,
                   ApartmentNumber     = a != null ? a.UnitNumber : null,
                   ContractNumber      = lc != null ? lc.ContractNumber : null,
               };
    }

    // ── Rent Payment Receipt read-side ───────────────────────────────────────

    public async Task<RentPaymentReceiptDto?> GetReceiptByRentPaymentIdAsync(
        Guid rentPaymentId,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return await (from r in _dbContext.RentPaymentReceipts
                      join p in _dbContext.RentPayments   on r.RentPaymentId equals p.Id into ps from p in ps.DefaultIfEmpty()
                      join lc in _dbContext.LeaseContracts on (p != null ? p.LeaseContractId : Guid.Empty) equals lc.Id into lcs from lc in lcs.DefaultIfEmpty()
                      join t  in _dbContext.Tenants         on (p != null ? p.TenantId        : Guid.Empty) equals t.Id  into ts  from t  in ts.DefaultIfEmpty()
                      join a  in _dbContext.Apartments      on (p != null ? p.ApartmentId     : Guid.Empty) equals a.Id  into ax  from a  in ax.DefaultIfEmpty()
                      join b  in _dbContext.Buildings       on (p != null ? p.BuildingId      : Guid.Empty) equals b.Id  into bx  from b  in bx.DefaultIfEmpty()
                      where r.RentPaymentId == rentPaymentId && r.CompanyId == companyId && r.DeletedAt == null
                      select new RentPaymentReceiptDto
                      {
                          Id              = r.Id,
                          CompanyId       = r.CompanyId,
                          RentPaymentId   = r.RentPaymentId,
                          ReceiptNumber   = r.ReceiptNumber,
                          IssueDate       = r.IssueDate,
                          IssuedBy        = r.IssuedBy,
                          Amount          = r.Amount,
                          Currency        = r.Currency,
                          Notes           = r.Notes,
                          FileId          = r.FileId,
                          CreatedAt       = r.CreatedAt,
                          TenantName      = t != null ? t.Name : null,
                          BuildingName    = b != null ? b.Name : null,
                          ApartmentNumber = a != null ? a.UnitNumber : null,
                          ContractNumber  = lc != null ? lc.ContractNumber : null,
                      })
                      .AsNoTracking()
                      .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<RentPaymentReceiptDto>> GetReceiptsAsync(
        RentPaymentReceiptFilterOptions filter,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = from r in _dbContext.RentPaymentReceipts
                        join p in _dbContext.RentPayments on r.RentPaymentId equals p.Id into ps from p in ps.DefaultIfEmpty()
                        join lc in _dbContext.LeaseContracts on (p != null ? p.LeaseContractId : Guid.Empty) equals lc.Id into lcs from lc in lcs.DefaultIfEmpty()
                        join t  in _dbContext.Tenants         on (p != null ? p.TenantId        : Guid.Empty) equals t.Id  into ts  from t  in ts.DefaultIfEmpty()
                        join a  in _dbContext.Apartments      on (p != null ? p.ApartmentId     : Guid.Empty) equals a.Id  into ax  from a  in ax.DefaultIfEmpty()
                        join b  in _dbContext.Buildings       on (p != null ? p.BuildingId      : Guid.Empty) equals b.Id  into bx  from b  in bx.DefaultIfEmpty()
                        where r.CompanyId == companyId && r.DeletedAt == null
                        select new { r, p, lc, t, a, b };

        if (filter.LeaseContractId.HasValue)
            baseQuery = baseQuery.Where(x => x.p != null && x.p.LeaseContractId == filter.LeaseContractId.Value);

        if (filter.TenantId.HasValue)
            baseQuery = baseQuery.Where(x => x.p != null && x.p.TenantId == filter.TenantId.Value);

        if (filter.DateFrom.HasValue)
            baseQuery = baseQuery.Where(x => x.r.IssueDate >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            baseQuery = baseQuery.Where(x => x.r.IssueDate <= filter.DateTo.Value);

        if (filter.LastSeenId.HasValue && filter.LastSeenIssueDate.HasValue)
        {
            var cursorDate = filter.LastSeenIssueDate.Value;
            var cursorId   = filter.LastSeenId.Value;

            baseQuery = baseQuery.Where(x =>
                x.r.IssueDate < cursorDate ||
                (x.r.IssueDate == cursorDate && x.r.Id.CompareTo(cursorId) > 0));
        }

        return baseQuery
            .AsNoTracking()
            .OrderByDescending(x => x.r.IssueDate)
            .ThenBy(x => x.r.Id)
            .Take(Math.Clamp(filter.PageSize, 1, 200))
            .Select(x => new RentPaymentReceiptDto
            {
                Id              = x.r.Id,
                CompanyId       = x.r.CompanyId,
                RentPaymentId   = x.r.RentPaymentId,
                ReceiptNumber   = x.r.ReceiptNumber,
                IssueDate       = x.r.IssueDate,
                IssuedBy        = x.r.IssuedBy,
                Amount          = x.r.Amount,
                Currency        = x.r.Currency,
                Notes           = x.r.Notes,
                FileId          = x.r.FileId,
                CreatedAt       = x.r.CreatedAt,
                TenantName      = x.t != null ? x.t.Name : null,
                BuildingName    = x.b != null ? x.b.Name : null,
                ApartmentNumber = x.a != null ? x.a.UnitNumber : null,
                ContractNumber  = x.lc != null ? x.lc.ContractNumber : null,
            })
            .ToListAsync(cancellationToken);
    }
}

