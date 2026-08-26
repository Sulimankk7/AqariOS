using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.UtilityBills.Queries.Common;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Application.UtilityBills.Queries.GetUtilityDashboardSummary;

public sealed record GetUtilityDashboardSummaryQuery : IRequest<UtilityDashboardSummaryDto>;

/// <summary>
/// Returns a lightweight utility summary for the tenant dashboard widget.
/// Single round-trip: indicates whether each utility type is linked
/// and shows the latest bill for each type.
///
/// Never triggers provider calls. Reads exclusively from PostgreSQL.
/// </summary>
public sealed class GetUtilityDashboardSummaryQueryHandler
    : IRequestHandler<GetUtilityDashboardSummaryQuery, UtilityDashboardSummaryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetUtilityDashboardSummaryQueryHandler(
        IApplicationDbContext db,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _db                 = db;
        _tenantContext      = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<UtilityDashboardSummaryDto> Handle(
        GetUtilityDashboardSummaryQuery request,
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
            return new UtilityDashboardSummaryDto(false, null, false, null);

        // Fetch both account IDs in one query
        var accounts = await _db.UtilityAccounts
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId.Value
                     && a.CompanyId == companyId
                     && a.DeletedAt == null)
            .Select(a => new
            {
                a.Id,
                a.UtilityType,
                a.AccountNumber,
                a.SyncStatus,
                a.LastSuccessfulSyncAt
            })
            .ToListAsync(cancellationToken);

        var electricityAccount = accounts
            .FirstOrDefault(a => a.UtilityType == UtilityType.Electricity);
        var waterAccount = accounts
            .FirstOrDefault(a => a.UtilityType == UtilityType.Water);

        // Fetch latest bills for each linked account
        async Task<UtilityBillDto?> GetLatestBill(Guid? accountId)
        {
            if (accountId is null) return null;
            return await _db.UtilityBills
                .AsNoTracking()
                .Where(b => b.UtilityAccountId == accountId.Value
                         && b.CompanyId == companyId)
                .OrderByDescending(b => b.BillDate)
                .Select(b => new UtilityBillDto(
                    b.Id, b.BillDate, b.DueDate, b.Amount, b.Currency,
                    b.IsPaid, b.PaymentStatus, b.DiscoveredAt,
                    null, null, null, null, null))
                .FirstOrDefaultAsync(cancellationToken);
        }

        var latestElectricity = await GetLatestBill(electricityAccount?.Id);
        var latestWater       = await GetLatestBill(waterAccount?.Id);

        return new UtilityDashboardSummaryDto(
            ElectricityLinked:      electricityAccount is not null,
            LatestElectricityBill:  latestElectricity,
            WaterLinked:            waterAccount is not null,
            LatestWaterBill:        latestWater,
            ElectricityAccountNumber: electricityAccount?.AccountNumber,
            ElectricitySyncStatus: electricityAccount?.SyncStatus,
            ElectricityLastSuccessfulSyncAt: electricityAccount?.LastSuccessfulSyncAt,
            WaterAccountNumber: waterAccount?.AccountNumber,
            WaterSyncStatus: waterAccount?.SyncStatus,
            WaterLastSuccessfulSyncAt: waterAccount?.LastSuccessfulSyncAt);
    }
}
