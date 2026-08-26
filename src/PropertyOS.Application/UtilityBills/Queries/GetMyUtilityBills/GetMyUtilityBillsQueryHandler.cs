using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Models;
using PropertyOS.Application.UtilityBills.Queries.Common;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Application.UtilityBills.Queries.GetMyUtilityBills;

public sealed record GetMyUtilityBillsQuery(
    UtilityType? UtilityType = null,
    int PageSize = 20,
    string? Cursor = null)
    : IRequest<KeysetPage<UtilityBillDto>>;

/// <summary>
/// Returns paginated utility bills for the requesting tenant's linked account.
///
/// Security: tenantId derived from JWT. companyId explicit WHERE clause.
/// Runs outside transaction — no RLS session. Explicit tenant filter is the boundary.
/// </summary>
public sealed class GetMyUtilityBillsQueryHandler
    : IRequestHandler<GetMyUtilityBillsQuery, KeysetPage<UtilityBillDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetMyUtilityBillsQueryHandler(
        IApplicationDbContext db,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _db                 = db;
        _tenantContext      = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<KeysetPage<UtilityBillDto>> Handle(
        GetMyUtilityBillsQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new UnauthorizedAccessException("Tenant context not established.");

        var userId = _currentUserContext.UserId
            ?? throw new UnauthorizedAccessException("User context not established.");

        var tenantId = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.CompanyId == companyId)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (tenantId is null)
            return new KeysetPage<UtilityBillDto>([], null, false);

        var accountsQuery = _db.UtilityAccounts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId.Value
                     && a.CompanyId == companyId)
            .Where(a => _db.LeaseContracts.Any(lease =>
                lease.Id == a.LeaseContractId
                && lease.CompanyId == companyId
                && lease.TenantId == tenantId.Value));

        if (request.UtilityType.HasValue)
            accountsQuery = accountsQuery
                .Where(a => a.UtilityType == request.UtilityType.Value);

        var query =
            from bill in _db.UtilityBills.AsNoTracking()
            join account in accountsQuery
                on new { Id = bill.UtilityAccountId, bill.CompanyId }
                equals new { account.Id, account.CompanyId }
            where bill.CompanyId == companyId
            select new { Bill = bill, Account = account };

        var cursor = KeysetCursor.Decode(request.Cursor);
        if (cursor.HasValue)
        {
            var cursorDate = DateOnly.FromDateTime(cursor.Value.SubmittedAt.UtcDateTime);
            query = query.Where(row =>
                row.Bill.BillDate < cursorDate
                || (row.Bill.BillDate == cursorDate
                    && row.Bill.Id.CompareTo(cursor.Value.Id) < 0));
        }

        var rows = await query
            .OrderByDescending(row => row.Bill.BillDate)
            .ThenByDescending(row => row.Bill.Id)
            .Take(request.PageSize + 1)
            .Select(row => new UtilityBillDto(
                row.Bill.Id,
                row.Bill.BillDate,
                row.Bill.DueDate,
                row.Bill.Amount,
                row.Bill.Currency,
                row.Bill.IsPaid,
                row.Bill.PaymentStatus,
                row.Bill.DiscoveredAt,
                row.Account.Id,
                row.Account.AccountNumber,
                row.Account.DeletedAt == null,
                row.Account.UtilityType,
                row.Account.DeletedAt))
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > request.PageSize;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        var last = rows.LastOrDefault();
        var nextCursor = hasMore && last is not null
            ? KeysetCursor.Encode(
                new DateTimeOffset(last.BillDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                last.Id)
            : null;

        return new KeysetPage<UtilityBillDto>(rows, nextCursor, hasMore);
    }
}
