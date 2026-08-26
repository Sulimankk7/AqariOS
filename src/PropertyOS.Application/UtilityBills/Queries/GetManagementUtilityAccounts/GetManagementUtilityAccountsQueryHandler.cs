using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Models;
using PropertyOS.Application.UtilityBills.Options;
using PropertyOS.Application.UtilityBills.Queries.Common;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Application.UtilityBills.Queries.GetManagementUtilityAccounts;

public sealed class GetManagementUtilityAccountsQueryHandler
    : IRequestHandler<GetManagementUtilityAccountsQuery, KeysetPage<ManagementUtilityAccountDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly UtilityBillsOptions _options;

    public GetManagementUtilityAccountsQueryHandler(
        IApplicationDbContext db,
        ITenantContext tenantContext,
        IOptionsSnapshot<UtilityBillsOptions> options)
    {
        _db = db;
        _tenantContext = tenantContext;
        _options = options.Value;
    }

    public async Task<KeysetPage<ManagementUtilityAccountDto>> Handle(
        GetManagementUtilityAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new UnauthorizedAccessException("Tenant context not established.");
        var scraperConfigured = IsScraperConfigured(_options.ScraperService);
        var electricityAvailability = GetAvailability(
            _options.Electricity.Enabled, scraperConfigured);
        var waterAvailability = GetAvailability(
            _options.Water.Enabled, scraperConfigured);

        var accountSource = request.IncludeUnlinked
            ? _db.UtilityAccounts.IgnoreQueryFilters().AsNoTracking()
            : _db.UtilityAccounts.AsNoTracking();

        var query =
            from account in accountSource
            join lease in _db.LeaseContracts.AsNoTracking()
                on new { Id = account.LeaseContractId, account.CompanyId }
                equals new { lease.Id, lease.CompanyId }
            join tenant in _db.Tenants.AsNoTracking()
                on new { Id = account.TenantId, account.CompanyId }
                equals new { tenant.Id, tenant.CompanyId }
            join apartment in _db.Apartments.AsNoTracking()
                on new { Id = account.ApartmentId, account.CompanyId }
                equals new { apartment.Id, apartment.CompanyId }
            join building in _db.Buildings.AsNoTracking()
                on new { Id = apartment.BuildingId, apartment.CompanyId }
                equals new { building.Id, building.CompanyId }
            where account.CompanyId == companyId
            select new { Account = account, Lease = lease, Tenant = tenant, Apartment = apartment, Building = building };

        if (request.UtilityType.HasValue)
            query = query.Where(row => row.Account.UtilityType == request.UtilityType.Value);

        if (request.SyncStatus.HasValue)
            query = query.Where(row => row.Account.SyncStatus == request.SyncStatus.Value);

        if (request.IsActive.HasValue)
            query = query.Where(row => row.Account.IsActive == request.IsActive.Value);

        if (request.LeaseContractId.HasValue)
            query = query.Where(row =>
                row.Account.LeaseContractId == request.LeaseContractId.Value);

        var cursor = KeysetCursor.Decode(request.Cursor);
        if (cursor.HasValue)
        {
            query = query.Where(row =>
                row.Account.CreatedAt < cursor.Value.SubmittedAt
                || (row.Account.CreatedAt == cursor.Value.SubmittedAt
                    && row.Account.Id.CompareTo(cursor.Value.Id) < 0));
        }

        var rows = await query
            .OrderByDescending(row => row.Account.CreatedAt)
            .ThenByDescending(row => row.Account.Id)
            .Take(request.PageSize + 1)
            .Select(row => new ManagementUtilityAccountDto(
                row.Account.Id,
                row.Account.UtilityType,
                row.Account.AccountNumber,
                row.Account.MeterNumber,
                row.Account.IsActive,
                row.Account.SyncStatus,
                row.Account.UtilityType == UtilityType.Electricity
                    ? electricityAvailability
                    : waterAvailability,
                row.Account.LastKnownBillDate,
                row.Account.LastSuccessfulSyncAt,
                row.Account.LastAttemptedSyncAt,
                row.Account.ConsecutiveFailureCount,
                row.Account.HistoricalBootstrapCompleted,
                row.Lease.Id,
                row.Lease.ContractNumber,
                row.Tenant.Id,
                row.Tenant.Name,
                row.Apartment.Id,
                row.Apartment.UnitNumber,
                row.Building.Id,
                row.Building.Name,
                _db.UtilityBills
                    .AsNoTracking()
                    .Where(bill => bill.CompanyId == companyId
                        && bill.UtilityAccountId == row.Account.Id)
                    .OrderByDescending(bill => bill.BillDate)
                    .ThenByDescending(bill => bill.Id)
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
                    .FirstOrDefault(),
                row.Account.CreatedAt,
                row.Account.DeletedAt))
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > request.PageSize;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        var last = rows.LastOrDefault();
        return new KeysetPage<ManagementUtilityAccountDto>(
            rows,
            hasMore && last is not null ? KeysetCursor.Encode(last.LinkedAt, last.Id) : null,
            hasMore);
    }

    private static UtilityProviderAvailability GetAvailability(bool enabled, bool scraperConfigured)
    {
        if (!enabled)
            return UtilityProviderAvailability.Disabled;

        return scraperConfigured
            ? UtilityProviderAvailability.Configured
            : UtilityProviderAvailability.NotConfigured;
    }

    private static bool IsScraperConfigured(UtilityScraperServiceOptions options) =>
        Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
        && uri.Scheme is "http" or "https"
        && System.Text.Encoding.UTF8.GetByteCount(options.SharedSecret ?? string.Empty) >= 32;
}
