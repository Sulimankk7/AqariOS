using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.UtilityBills.Queries.Common;

namespace PropertyOS.Application.UtilityBills.Queries.GetMyUtilityAccounts;

public sealed record GetMyUtilityAccountsQuery : IRequest<List<UtilityAccountDto>>;

/// <summary>
/// Returns all utility accounts linked to the requesting tenant's active lease.
///
/// Security: tenantId and companyId are derived from ITenantContext (JWT only).
/// Never accepts IDs from the request body or URL parameters.
///
/// Runs OUTSIDE a DB transaction — no TransactionBehavior, no RLS session.
/// Explicit companyId WHERE clause is the authoritative tenant isolation filter.
/// </summary>
public sealed class GetMyUtilityAccountsQueryHandler
    : IRequestHandler<GetMyUtilityAccountsQuery, List<UtilityAccountDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetMyUtilityAccountsQueryHandler(
        IApplicationDbContext db,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _db                 = db;
        _tenantContext      = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<List<UtilityAccountDto>> Handle(
        GetMyUtilityAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new UnauthorizedAccessException("Tenant context not established.");

        var userId = _currentUserContext.UserId
            ?? throw new UnauthorizedAccessException("User context not established.");

        // Resolve tenantId from userId within the company scope
        var tenantId = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.CompanyId == companyId)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (tenantId is null)
            return new List<UtilityAccountDto>();

        // Fetch account scalars and the latest bill in one SQL query. Order by the
        // mapped source column before materialization; ordering by LinkedAt after
        // constructing UtilityAccountDto makes EF try to translate the whole DTO,
        // including its correlated latest-bill projection.
        var rows = await _db.UtilityAccounts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId.Value
                     && a.CompanyId == companyId)
            .Where(a => _db.LeaseContracts.Any(lease =>
                lease.Id == a.LeaseContractId
                && lease.CompanyId == companyId
                && lease.TenantId == tenantId.Value))
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.UtilityType,
                a.AccountNumber,
                a.MeterNumber,
                a.IsActive,
                a.LastKnownBillDate,
                a.LastSuccessfulSyncAt,
                a.SyncStatus,
                a.AverageBillingIntervalDays,
                a.EstimatedNextBillDate,
                a.HistoricalBootstrapCompleted,
                a.TotalOutstandingBalance,
                LatestBill = _db.UtilityBills
                    .AsNoTracking()
                    .Where(b => b.UtilityAccountId == a.Id)
                    .OrderByDescending(b => b.BillDate)
                    .Select(b => new
                    {
                        b.Id,
                        b.BillDate,
                        b.DueDate,
                        b.Amount,
                        b.Currency,
                        b.IsPaid,
                        b.PaymentStatus,
                        b.DiscoveredAt
                    })
                    .FirstOrDefault(),
                UnlinkedAt = a.DeletedAt,
                LinkedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return rows.Select(row => new UtilityAccountDto(
                row.Id,
                row.UtilityType,
                row.AccountNumber,
                row.MeterNumber,
                row.IsActive,
                row.LastKnownBillDate,
                row.LastSuccessfulSyncAt,
                row.SyncStatus,
                row.AverageBillingIntervalDays,
                row.EstimatedNextBillDate,
                row.HistoricalBootstrapCompleted,
                row.TotalOutstandingBalance,
                row.LatestBill is null
                    ? null
                    : new UtilityBillDto(
                        row.LatestBill.Id,
                        row.LatestBill.BillDate,
                        row.LatestBill.DueDate,
                        row.LatestBill.Amount,
                        row.LatestBill.Currency,
                        row.LatestBill.IsPaid,
                        row.LatestBill.PaymentStatus,
                        row.LatestBill.DiscoveredAt,
                        null, null, null, null, null),
                row.UnlinkedAt,
                row.LinkedAt))
            .ToList();
    }
}
