using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetTenantById;
using PropertyOS.Domain.Leasing;

namespace PropertyOS.Tests.Unit.Application.Leasing.Tenants;

/// <summary>
/// Self-contained fake for ITenantRepository shared by the tenant handler tests.
/// </summary>
internal class FakeTenantRepository : ITenantRepository
{
    public List<Tenant> Store { get; } = new();
    public List<Tenant> AddedTenants { get; } = new();

    public bool NationalIdExists { get; set; }
    public bool HasNonTerminalLease { get; set; }

    public Guid? LastExistsCompanyId { get; private set; }
    public string? LastExistsNationalId { get; private set; }
    public Guid? LastExistsExcludeTenantId { get; private set; }
    public Guid? LastNonTerminalLeaseTenantId { get; private set; }
    public Guid? LastSearchCompanyId { get; private set; }
    public string? LastSearchTerm { get; private set; }

    public List<TenantDto> SearchResult { get; set; } = new();
    public TenantDetailDto? DetailResult { get; set; }

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.FirstOrDefault(t => t.Id == id && t.DeletedAt == null));

    public Task<Tenant?> GetByUserIdAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Store.FirstOrDefault(t => t.CompanyId == companyId && t.UserId == userId && t.DeletedAt == null));

    public Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        AddedTenants.Add(tenant);
        Store.Add(tenant);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByNationalIdAsync(Guid companyId, string nationalId, Guid? excludeTenantId = null, CancellationToken cancellationToken = default)
    {
        LastExistsCompanyId = companyId;
        LastExistsNationalId = nationalId;
        LastExistsExcludeTenantId = excludeTenantId;
        return Task.FromResult(NationalIdExists);
    }

    public Task<bool> HasNonTerminalLeaseContractAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        LastNonTerminalLeaseTenantId = tenantId;
        return Task.FromResult(HasNonTerminalLease);
    }

    public Task<TenantDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
        => Task.FromResult(DetailResult);

    public Task<List<TenantDto>> SearchAsync(Guid companyId, string searchTerm, CancellationToken cancellationToken = default)
    {
        LastSearchCompanyId = companyId;
        LastSearchTerm = searchTerm;
        return Task.FromResult(SearchResult);
    }

    public List<TenantFamilyMember> FamilyMemberStore { get; } = new();
    public List<TenantFamilyMember> AddedFamilyMembers { get; } = new();

    public Task<TenantFamilyMember?> GetFamilyMemberByIdAsync(Guid tenantId, Guid familyMemberId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(FamilyMemberStore.FirstOrDefault(fm => fm.Id == familyMemberId && fm.TenantId == tenantId && fm.CompanyId == companyId && fm.DeletedAt == null));
    }

    public Task<TenantFamilyMemberDto?> GetFamilyMemberDtoByIdAsync(Guid tenantId, Guid familyMemberId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var fm = FamilyMemberStore.FirstOrDefault(m => m.Id == familyMemberId && m.TenantId == tenantId && m.CompanyId == companyId && m.DeletedAt == null);
        if (fm == null) return Task.FromResult<TenantFamilyMemberDto?>(null);

        return Task.FromResult<TenantFamilyMemberDto?>(new TenantFamilyMemberDto
        {
            Id = fm.Id,
            Name = fm.Name,
            RelationshipType = fm.RelationshipType,
            AgeBracket = fm.AgeBracket,
            CreatedAt = fm.CreatedAt
        });
    }

    public Task<List<TenantFamilyMemberDto>> GetFamilyMembersForTenantAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var list = FamilyMemberStore
            .Where(fm => fm.TenantId == tenantId && fm.CompanyId == companyId && fm.DeletedAt == null)
            .OrderBy(fm => fm.CreatedAt)
            .Select(fm => new TenantFamilyMemberDto
            {
                Id = fm.Id,
                Name = fm.Name,
                RelationshipType = fm.RelationshipType,
                AgeBracket = fm.AgeBracket,
                CreatedAt = fm.CreatedAt
            })
            .ToList();

        return Task.FromResult(list);
    }

    public Task AddFamilyMemberAsync(TenantFamilyMember familyMember, CancellationToken cancellationToken = default)
    {
        AddedFamilyMembers.Add(familyMember);
        FamilyMemberStore.Add(familyMember);
        return Task.CompletedTask;
    }

    public List<TenantEmergencyContact> EmergencyContactStore { get; } = new();
    public List<TenantEmergencyContact> AddedEmergencyContacts { get; } = new();

    public Task<TenantEmergencyContact?> GetEmergencyContactByIdAsync(Guid tenantId, Guid contactId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(EmergencyContactStore.FirstOrDefault(ec => ec.Id == contactId && ec.TenantId == tenantId && ec.CompanyId == companyId && ec.DeletedAt == null));
    }

    public Task<TenantEmergencyContactDto?> GetEmergencyContactDtoByIdAsync(Guid tenantId, Guid contactId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var ec = EmergencyContactStore.FirstOrDefault(c => c.Id == contactId && c.TenantId == tenantId && c.CompanyId == companyId && c.DeletedAt == null);
        if (ec == null) return Task.FromResult<TenantEmergencyContactDto?>(null);

        return Task.FromResult<TenantEmergencyContactDto?>(new TenantEmergencyContactDto
        {
            Id = ec.Id,
            Name = ec.Name,
            RelationshipType = ec.RelationshipType,
            Phone = ec.Phone,
            CreatedAt = ec.CreatedAt
        });
    }

    public Task<List<TenantEmergencyContactDto>> GetEmergencyContactsForTenantAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var list = EmergencyContactStore
            .Where(ec => ec.TenantId == tenantId && ec.CompanyId == companyId && ec.DeletedAt == null)
            .OrderBy(ec => ec.CreatedAt)
            .Select(ec => new TenantEmergencyContactDto
            {
                Id = ec.Id,
                Name = ec.Name,
                RelationshipType = ec.RelationshipType,
                Phone = ec.Phone,
                CreatedAt = ec.CreatedAt
            })
            .ToList();

        return Task.FromResult(list);
    }

    public Task AddEmergencyContactAsync(TenantEmergencyContact contact, CancellationToken cancellationToken = default)
    {
        AddedEmergencyContacts.Add(contact);
        EmergencyContactStore.Add(contact);
        return Task.CompletedTask;
    }

    public List<TenantVehicle> VehicleStore { get; } = new();
    public List<TenantVehicle> AddedVehicles { get; } = new();

    public Task<TenantVehicle?> GetVehicleByIdAsync(Guid tenantId, Guid vehicleId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(VehicleStore.FirstOrDefault(v => v.Id == vehicleId && v.TenantId == tenantId && v.CompanyId == companyId && v.DeletedAt == null));
    }

    public Task<TenantVehicleDto?> GetVehicleDtoByIdAsync(Guid tenantId, Guid vehicleId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var v = VehicleStore.FirstOrDefault(x => x.Id == vehicleId && x.TenantId == tenantId && x.CompanyId == companyId && x.DeletedAt == null);
        if (v == null) return Task.FromResult<TenantVehicleDto?>(null);

        return Task.FromResult<TenantVehicleDto?>(new TenantVehicleDto
        {
            Id = v.Id,
            PlateNumber = v.PlateNumber,
            MakeModel = v.MakeModel,
            Color = v.Color,
            CreatedAt = v.CreatedAt
        });
    }

    public Task<List<TenantVehicleDto>> GetVehiclesForTenantAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var list = VehicleStore
            .Where(v => v.TenantId == tenantId && v.CompanyId == companyId && v.DeletedAt == null)
            .OrderBy(v => v.CreatedAt)
            .Select(v => new TenantVehicleDto
            {
                Id = v.Id,
                PlateNumber = v.PlateNumber,
                MakeModel = v.MakeModel,
                Color = v.Color,
                CreatedAt = v.CreatedAt
            })
            .ToList();

        return Task.FromResult(list);
    }

    public Task AddVehicleAsync(TenantVehicle vehicle, CancellationToken cancellationToken = default)
    {
        AddedVehicles.Add(vehicle);
        VehicleStore.Add(vehicle);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByPlateNumberAsync(Guid companyId, string plateNumber, Guid? excludeVehicleId = null, CancellationToken cancellationToken = default)
    {
        var trimmed = plateNumber.Trim();
        var exists = VehicleStore.Any(v =>
            v.CompanyId == companyId &&
            v.DeletedAt == null &&
            v.PlateNumber.Equals(trimmed, StringComparison.OrdinalIgnoreCase) &&
            (!excludeVehicleId.HasValue || v.Id != excludeVehicleId.Value));

        return Task.FromResult(exists);
    }
}

internal class FakeTenantContext : PropertyOS.Application.Common.Interfaces.ITenantContext
{
    public Guid? CompanyId { get; set; } = Guid.NewGuid();
    public bool IsPlatformAdmin { get; set; }
}

internal class FakeCurrentUserContext : PropertyOS.Application.Common.Interfaces.ICurrentUserContext
{
    public Guid? UserId { get; set; } = Guid.NewGuid();
}
