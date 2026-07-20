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
        return _dbContext.RentPayments.Include(p => p.Receipt).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
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
        return _dbContext.RentPayments.AnyAsync(
            p => p.LeaseContractId == leaseContractId
                 && p.PaymentPurpose == PaymentPurpose.ScheduledInstallment
                 && p.BillingPeriodStart == start
                 && p.BillingPeriodEnd == end,
            cancellationToken);
    }

    public Task<ChequeDetails?> LoadChequeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.ChequeDetails.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task AddChequeAsync(ChequeDetails cheque, CancellationToken cancellationToken = default)
    {
        await _dbContext.ChequeDetails.AddAsync(cheque, cancellationToken);
    }

    public Task<PaymentAllocation?> LoadAllocationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.PaymentAllocations.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task AddAllocationAsync(PaymentAllocation allocation, CancellationToken cancellationToken = default)
    {
        await _dbContext.PaymentAllocations.AddAsync(allocation, cancellationToken);
    }

    public Task<List<PaymentAllocation>> GetAllocationsByObligationIdAsync(Guid obligationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.PaymentAllocations
            .Where(a => a.ObligationPaymentId == obligationId)
            .ToListAsync(cancellationToken);
    }

    public Task<List<PaymentAllocation>> GetAllocationsByReceivingIdAsync(Guid receivingId, CancellationToken cancellationToken = default)
    {
        return _dbContext.PaymentAllocations
            .Where(a => a.ReceivingPaymentId == receivingId)
            .ToListAsync(cancellationToken);
    }

    public async Task<RentPaymentDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var paymentDto = await _dbContext.RentPayments
            .AsNoTracking()
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

    public Task<List<RentPaymentDto>> GetPaymentsForLeaseAsync(Guid leaseContractId, CancellationToken cancellationToken = default)
    {
        return _dbContext.RentPayments
            .AsNoTracking()
            .Where(p => p.LeaseContractId == leaseContractId)
            .OrderBy(p => p.DueDate)
            .ProjectToType<RentPaymentDto>()
            .ToListAsync(cancellationToken);
    }

    public Task<List<RentPaymentDto>> GetPaymentsForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return _dbContext.RentPayments
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .ProjectToType<RentPaymentDto>()
            .ToListAsync(cancellationToken);
    }

    public Task<List<RentPaymentDto>> SearchPaymentsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return _dbContext.RentPayments
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ProjectToType<RentPaymentDto>()
                .ToListAsync(cancellationToken);
        }

        var normalizedSearch = searchTerm.Trim().ToLower();

        return (from p in _dbContext.RentPayments
                join c in _dbContext.LeaseContracts on p.LeaseContractId equals c.Id
                join t in _dbContext.Tenants on p.TenantId equals t.Id
                where EF.Functions.ILike(p.ReceiptNumber, $"%{normalizedSearch}%") ||
                      EF.Functions.ILike(c.ContractNumber, $"%{normalizedSearch}%") ||
                      EF.Functions.ILike(t.Name, $"%{normalizedSearch}%")
                select p)
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ProjectToType<RentPaymentDto>()
                .ToListAsync(cancellationToken);
    }

    public Task<List<RentPaymentDto>> GetOutstandingPaymentsAsync(CancellationToken cancellationToken = default)
    {
        var outstandingStatuses = new[]
        {
            DueDateStatus.Pending,
            DueDateStatus.PartiallyPaid,
            DueDateStatus.Late,
            DueDateStatus.OverdueUnpaid
        };

        return _dbContext.RentPayments
            .AsNoTracking()
            .Where(p => outstandingStatuses.Contains(p.DueDateStatus))
            .OrderBy(p => p.DueDate)
            .ProjectToType<RentPaymentDto>()
            .ToListAsync(cancellationToken);
    }

    public Task<List<ChequeDetailDto>> GetChequesByStatusAsync(ChequeStatus status, CancellationToken cancellationToken = default)
    {
        return _dbContext.ChequeDetails
            .AsNoTracking()
            .Where(c => c.Status == status)
            .ProjectToType<ChequeDetailDto>()
            .ToListAsync(cancellationToken);
    }

    public Task<List<ChequeDetailDto>> GetUpcomingChequesAsync(int daysAhead, CancellationToken cancellationToken = default)
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
            .Where(c => upcomingStatuses.Contains(c.Status)
                        && c.DueDate >= today
                        && c.DueDate <= limitDate)
            .OrderBy(c => c.DueDate)
            .ProjectToType<ChequeDetailDto>()
            .ToListAsync(cancellationToken);
    }

    // ── Rent Payment Receipt read-side ───────────────────────────────────────

    public Task<RentPaymentReceiptDto?> GetReceiptByRentPaymentIdAsync(
        Guid rentPaymentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.RentPaymentReceipts
            .AsNoTracking()
            .Where(r => r.RentPaymentId == rentPaymentId && r.DeletedAt == null)
            .ProjectToType<RentPaymentReceiptDto>()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<RentPaymentReceiptDto>> GetReceiptsAsync(
        RentPaymentReceiptFilterOptions filter,
        CancellationToken cancellationToken = default)
    {
        // Join receipts → payments to allow filtering by LeaseContractId / TenantId
        var query =
            from r in _dbContext.RentPaymentReceipts
            join p in _dbContext.RentPayments on r.RentPaymentId equals p.Id
            where r.DeletedAt == null
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
            .Take(filter.PageSize)
            .Select(x => x.r)
            .ProjectToType<RentPaymentReceiptDto>()
            .ToListAsync(cancellationToken);
    }
}

