using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Leasing.Enums;

namespace PropertyOS.Application.UtilityBills.Security;

internal sealed record TenantUtilityAccessScope(
    Guid CompanyId,
    Guid TenantId,
    Guid LeaseContractId);

internal static class TenantUtilityAccountAccess
{
    public static async Task<TenantUtilityAccessScope> ResolveAsync(
        IApplicationDbContext db,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        CancellationToken cancellationToken)
    {
        var companyId = tenantContext.CompanyId
            ?? throw new UnauthorizedAccessException("Tenant context not established.");
        var userId = currentUserContext.UserId
            ?? throw new UnauthorizedAccessException("User context not established.");

        var tenantIds = await db.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.CompanyId == companyId && tenant.UserId == userId)
            .Select(tenant => tenant.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (tenantIds.Count != 1)
        {
            throw new BusinessRuleException(
                "A unique tenant profile could not be resolved for utility account management.",
                "UTILITY_ACCOUNT_TENANT_CONTEXT_INVALID");
        }

        var tenantId = tenantIds[0];
        var leaseIds = await db.LeaseContracts
            .AsNoTracking()
            .Where(lease => lease.CompanyId == companyId
                         && lease.TenantId == tenantId
                         && lease.Status == ContractStatus.Active)
            .Where(lease => db.Apartments.Any(apartment =>
                apartment.Id == lease.ApartmentId
                && apartment.CompanyId == companyId
                && apartment.BuildingId == lease.BuildingId))
            .Where(lease => db.Buildings.Any(building =>
                building.Id == lease.BuildingId
                && building.CompanyId == companyId))
            .Select(lease => lease.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (leaseIds.Count == 0)
        {
            throw new BusinessRuleException(
                "No eligible active lease is available for utility account management.",
                "UTILITY_ACCOUNT_NO_ELIGIBLE_LEASE");
        }

        if (leaseIds.Count > 1)
        {
            throw new BusinessRuleException(
                "Utility account management cannot continue because the active lease context is ambiguous.",
                "UTILITY_ACCOUNT_ACTIVE_LEASE_AMBIGUOUS");
        }

        return new TenantUtilityAccessScope(companyId, tenantId, leaseIds[0]);
    }

    public static async Task EnsureOwnCurrentAccountAsync(
        IApplicationDbContext db,
        TenantUtilityAccessScope scope,
        Guid utilityAccountId,
        CancellationToken cancellationToken)
    {
        var authorized = await db.UtilityAccounts
            .AsNoTracking()
            .AnyAsync(account => account.Id == utilityAccountId
                              && account.CompanyId == scope.CompanyId
                              && account.TenantId == scope.TenantId
                              && account.LeaseContractId == scope.LeaseContractId,
                cancellationToken);

        if (!authorized)
        {
            throw new NotFoundException(
                $"Utility account with ID '{utilityAccountId}' was not found.");
        }
    }
}
