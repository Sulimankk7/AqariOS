using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetTenantById;
using PropertyOS.Domain.Leasing;

namespace PropertyOS.Application.Leasing;

/// <summary>
/// Repository for Module 5 tenant-person aggregates (write side) and tenant read projections.
/// Read methods take an explicit companyId predicate because plain queries may execute
/// outside a transaction and therefore without RLS tenant context.
/// </summary>
public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when a non-deleted tenant with the given national ID exists in the company,
    /// optionally excluding one tenant (self-exclusion on update).
    /// Backed by the partial unique index uq_tenants_company_national_id (deleted_at IS NULL).
    /// </summary>
    Task<bool> ExistsByNationalIdAsync(Guid companyId, string nationalId, Guid? excludeTenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when any non-terminal (draft / pending_signature / active) lease contract
    /// references the tenant. Guards tenant soft-deletion.
    /// </summary>
    Task<bool> HasNonTerminalLeaseContractAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Read-side detail projection including child collections. Company-scoped; null when not found or cross-tenant.</summary>
    Task<TenantDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Company-scoped search over name / national_id / phone. Empty search term returns all (newest first).</summary>
    Task<List<TenantDto>> SearchAsync(Guid companyId, string searchTerm, CancellationToken cancellationToken = default);
}
