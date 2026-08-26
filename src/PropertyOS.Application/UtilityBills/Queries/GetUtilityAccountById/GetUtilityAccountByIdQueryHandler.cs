using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.UtilityBills.Options;
using PropertyOS.Application.UtilityBills.Queries.Common;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Application.UtilityBills.Queries.GetUtilityAccountById;

public sealed record GetUtilityAccountByIdQuery(Guid UtilityAccountId)
    : IRequest<ManagementUtilityAccountDto>;

public sealed class GetUtilityAccountByIdQueryHandler
    : IRequestHandler<GetUtilityAccountByIdQuery, ManagementUtilityAccountDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly UtilityBillsOptions _options;

    public GetUtilityAccountByIdQueryHandler(
        IApplicationDbContext db,
        ITenantContext tenantContext,
        IOptionsSnapshot<UtilityBillsOptions> options)
    {
        _db = db;
        _tenantContext = tenantContext;
        _options = options.Value;
    }

    public async Task<ManagementUtilityAccountDto> Handle(
        GetUtilityAccountByIdQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new UnauthorizedAccessException("Tenant context not established.");
        var scraperConfigured = IsScraperConfigured(_options.ScraperService);
        var electricityAvailability = GetAvailability(
            _options.Electricity.Enabled, scraperConfigured);
        var waterAvailability = GetAvailability(
            _options.Water.Enabled, scraperConfigured);

        return await (
            from account in _db.UtilityAccounts.IgnoreQueryFilters().AsNoTracking()
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
            where account.Id == request.UtilityAccountId && account.CompanyId == companyId
            select new ManagementUtilityAccountDto(
                account.Id,
                account.UtilityType,
                account.AccountNumber,
                account.MeterNumber,
                account.IsActive,
                account.SyncStatus,
                account.UtilityType == UtilityType.Electricity
                    ? electricityAvailability
                    : waterAvailability,
                account.LastKnownBillDate,
                account.LastSuccessfulSyncAt,
                account.LastAttemptedSyncAt,
                account.ConsecutiveFailureCount,
                account.HistoricalBootstrapCompleted,
                lease.Id,
                lease.ContractNumber,
                tenant.Id,
                tenant.Name,
                apartment.Id,
                apartment.UnitNumber,
                building.Id,
                building.Name,
                _db.UtilityBills
                    .AsNoTracking()
                    .Where(b => b.UtilityAccountId == account.Id && b.CompanyId == companyId)
                    .OrderByDescending(b => b.BillDate)
                    .ThenByDescending(b => b.Id)
                    .Select(b => new UtilityBillDto(
                        b.Id, b.BillDate, b.DueDate, b.Amount, b.Currency,
                        b.IsPaid, b.PaymentStatus, b.DiscoveredAt,
                        null, null, null, null, null))
                    .FirstOrDefault(),
                account.CreatedAt,
                account.DeletedAt))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(
                $"Utility account with ID '{request.UtilityAccountId}' was not found.");
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
