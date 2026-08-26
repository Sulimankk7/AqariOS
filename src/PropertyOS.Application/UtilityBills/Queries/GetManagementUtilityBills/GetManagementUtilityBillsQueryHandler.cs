using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Models;
using PropertyOS.Application.UtilityBills.Queries.Common;

namespace PropertyOS.Application.UtilityBills.Queries.GetManagementUtilityBills;

public sealed class GetManagementUtilityBillsQueryHandler
    : IRequestHandler<GetManagementUtilityBillsQuery, KeysetPage<UtilityBillDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetManagementUtilityBillsQueryHandler(
        IApplicationDbContext db,
        ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<KeysetPage<UtilityBillDto>> Handle(
        GetManagementUtilityBillsQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new UnauthorizedAccessException("Tenant context not established.");

        var accountExists = await _db.UtilityAccounts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(account => account.Id == request.UtilityAccountId
                && account.CompanyId == companyId, cancellationToken);

        if (!accountExists)
            throw new NotFoundException(
                $"Utility account with ID '{request.UtilityAccountId}' was not found.");

        var query =
            from bill in _db.UtilityBills.AsNoTracking()
            join account in _db.UtilityAccounts.IgnoreQueryFilters().AsNoTracking()
                on new { Id = bill.UtilityAccountId, bill.CompanyId }
                equals new { account.Id, account.CompanyId }
            where account.Id == request.UtilityAccountId
                && account.CompanyId == companyId
                && bill.CompanyId == companyId
            select bill;

        if (request.PaymentStatus.HasValue)
            query = query.Where(bill => bill.PaymentStatus == request.PaymentStatus.Value);

        var cursor = KeysetCursor.Decode(request.Cursor);
        if (cursor.HasValue)
        {
            var cursorDate = DateOnly.FromDateTime(cursor.Value.SubmittedAt.UtcDateTime);
            query = query.Where(bill =>
                bill.BillDate < cursorDate
                || (bill.BillDate == cursorDate && bill.Id.CompareTo(cursor.Value.Id) < 0));
        }

        var rows = await query
            .OrderByDescending(bill => bill.BillDate)
            .ThenByDescending(bill => bill.Id)
            .Take(request.PageSize + 1)
            .Select(bill => new UtilityBillDto(
                bill.Id,
                bill.BillDate,
                bill.DueDate,
                bill.Amount,
                bill.Currency,
                bill.IsPaid,
                bill.PaymentStatus,
                bill.DiscoveredAt,
                null,
                null,
                null,
                null,
                null))
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
