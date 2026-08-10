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
    /// Finds a tenant person record linked to the specified UserId within a company scope.
    /// </summary>
    Task<Tenant?> GetByUserIdAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default);

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

    Task<TenantFamilyMember?> GetFamilyMemberByIdAsync(Guid tenantId, Guid familyMemberId, Guid companyId, CancellationToken cancellationToken = default);

    Task<TenantFamilyMemberDto?> GetFamilyMemberDtoByIdAsync(Guid tenantId, Guid familyMemberId, Guid companyId, CancellationToken cancellationToken = default);

    Task<List<TenantFamilyMemberDto>> GetFamilyMembersForTenantAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default);

    Task AddFamilyMemberAsync(TenantFamilyMember familyMember, CancellationToken cancellationToken = default);

    Task<TenantEmergencyContact?> GetEmergencyContactByIdAsync(Guid tenantId, Guid contactId, Guid companyId, CancellationToken cancellationToken = default);

    Task<TenantEmergencyContactDto?> GetEmergencyContactDtoByIdAsync(Guid tenantId, Guid contactId, Guid companyId, CancellationToken cancellationToken = default);

    Task<List<TenantEmergencyContactDto>> GetEmergencyContactsForTenantAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default);

    Task AddEmergencyContactAsync(TenantEmergencyContact contact, CancellationToken cancellationToken = default);

    Task<TenantVehicle?> GetVehicleByIdAsync(Guid tenantId, Guid vehicleId, Guid companyId, CancellationToken cancellationToken = default);

    Task<TenantVehicleDto?> GetVehicleDtoByIdAsync(Guid tenantId, Guid vehicleId, Guid companyId, CancellationToken cancellationToken = default);

    Task<List<TenantVehicleDto>> GetVehiclesForTenantAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default);

    Task AddVehicleAsync(TenantVehicle vehicle, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when a non-deleted vehicle with the given plate number exists in the company,
    /// optionally excluding one vehicle (self-exclusion on update).
    /// Backed by the partial unique index uq_tenant_vehicles_company_plate (deleted_at IS NULL).
    /// </summary>
    Task<bool> ExistsByPlateNumberAsync(Guid companyId, string plateNumber, Guid? excludeVehicleId = null, CancellationToken cancellationToken = default);
}
