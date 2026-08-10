using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Leasing.Queries.Common;
using PropertyOS.Application.Leasing.Queries.GetTenantById;
using PropertyOS.Domain.Leasing;

namespace PropertyOS.Infrastructure.Leasing.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly PropertyOS.Infrastructure.Persistence.PropertyOsDbContext _dbContext;

    public TenantRepository(PropertyOS.Infrastructure.Persistence.PropertyOsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // DeletedAt filter is explicit on top of the model-level soft-delete query filter.
        return _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == id && t.DeletedAt == null, cancellationToken);
    }

    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        await _dbContext.Tenants.AddAsync(tenant, cancellationToken);
    }

    public Task<Tenant?> GetByUserIdAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Tenants.FirstOrDefaultAsync(t => t.CompanyId == companyId && t.UserId == userId && t.DeletedAt == null, cancellationToken);
    }

    public Task<bool> ExistsByNationalIdAsync(Guid companyId, string nationalId, Guid? excludeTenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.CompanyId == companyId
                        && t.NationalId == nationalId
                        && t.DeletedAt == null);

        if (excludeTenantId.HasValue)
        {
            query = query.Where(t => t.Id != excludeTenantId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> HasNonTerminalLeaseContractAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return _dbContext.LeaseContracts.AnyAsync(
            c => c.TenantId == tenantId
                 && (c.Status == PropertyOS.Domain.Leasing.Enums.ContractStatus.Draft ||
                     c.Status == PropertyOS.Domain.Leasing.Enums.ContractStatus.PendingSignature ||
                     c.Status == PropertyOS.Domain.Leasing.Enums.ContractStatus.Active),
            cancellationToken);
    }

    public async Task<TenantDetailDto?> GetDetailByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        var tenantDto = await _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.Id == id && t.CompanyId == companyId && t.DeletedAt == null)
            .Select(t => new TenantDetailDto
            {
                Id = t.Id,
                CompanyId = t.CompanyId,
                Name = t.Name,
                NationalId = t.NationalId,
                Phone = t.Phone,
                Occupation = t.Occupation,
                Employer = t.Employer,
                UserId = t.UserId,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                CreatedBy = t.CreatedBy,
                UpdatedBy = t.UpdatedBy
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (tenantDto == null)
            return null;

        tenantDto.FamilyMembers = await _dbContext.TenantFamilyMembers
            .AsNoTracking()
            .Where(m => m.TenantId == id && m.CompanyId == companyId && m.DeletedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new TenantFamilyMemberDto
            {
                Id = m.Id,
                Name = m.Name,
                RelationshipType = m.RelationshipType,
                AgeBracket = m.AgeBracket,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync(cancellationToken);

        tenantDto.EmergencyContacts = await _dbContext.TenantEmergencyContacts
            .AsNoTracking()
            .Where(c => c.TenantId == id && c.CompanyId == companyId && c.DeletedAt == null)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new TenantEmergencyContactDto
            {
                Id = c.Id,
                Name = c.Name,
                RelationshipType = c.RelationshipType,
                Phone = c.Phone,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        tenantDto.Vehicles = await _dbContext.TenantVehicles
            .AsNoTracking()
            .Where(v => v.TenantId == id && v.CompanyId == companyId && v.DeletedAt == null)
            .OrderBy(v => v.CreatedAt)
            .Select(v => new TenantVehicleDto
            {
                Id = v.Id,
                PlateNumber = v.PlateNumber,
                MakeModel = v.MakeModel,
                Color = v.Color,
                CreatedAt = v.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return tenantDto;
    }

    public Task<List<TenantDto>> SearchAsync(Guid companyId, string searchTerm, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.CompanyId == companyId && t.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearch = searchTerm.Trim();
            query = query.Where(t =>
                EF.Functions.ILike(t.Name, $"%{normalizedSearch}%") ||
                EF.Functions.ILike(t.NationalId, $"%{normalizedSearch}%") ||
                EF.Functions.ILike(t.Phone, $"%{normalizedSearch}%"));
        }

        return query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TenantDto
            {
                Id = t.Id,
                CompanyId = t.CompanyId,
                Name = t.Name,
                NationalId = t.NationalId,
                Phone = t.Phone,
                Occupation = t.Occupation,
                Employer = t.Employer,
                UserId = t.UserId,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                CreatedBy = t.CreatedBy,
                UpdatedBy = t.UpdatedBy
            })
            .ToListAsync(cancellationToken);
    }

    public Task<TenantFamilyMember?> GetFamilyMemberByIdAsync(Guid tenantId, Guid familyMemberId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TenantFamilyMembers
            .FirstOrDefaultAsync(fm => fm.Id == familyMemberId && fm.TenantId == tenantId && fm.CompanyId == companyId && fm.DeletedAt == null, cancellationToken);
    }

    public Task<TenantFamilyMemberDto?> GetFamilyMemberDtoByIdAsync(Guid tenantId, Guid familyMemberId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TenantFamilyMembers
            .AsNoTracking()
            .Where(fm => fm.Id == familyMemberId && fm.TenantId == tenantId && fm.CompanyId == companyId && fm.DeletedAt == null)
            .Select(fm => new TenantFamilyMemberDto
            {
                Id = fm.Id,
                Name = fm.Name,
                RelationshipType = fm.RelationshipType,
                AgeBracket = fm.AgeBracket,
                CreatedAt = fm.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<TenantFamilyMemberDto>> GetFamilyMembersForTenantAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TenantFamilyMembers
            .AsNoTracking()
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
            .ToListAsync(cancellationToken);
    }

    public async Task AddFamilyMemberAsync(TenantFamilyMember familyMember, CancellationToken cancellationToken = default)
    {
        await _dbContext.TenantFamilyMembers.AddAsync(familyMember, cancellationToken);
    }

    public Task<TenantEmergencyContact?> GetEmergencyContactByIdAsync(Guid tenantId, Guid contactId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TenantEmergencyContacts
            .FirstOrDefaultAsync(ec => ec.Id == contactId && ec.TenantId == tenantId && ec.CompanyId == companyId && ec.DeletedAt == null, cancellationToken);
    }

    public Task<TenantEmergencyContactDto?> GetEmergencyContactDtoByIdAsync(Guid tenantId, Guid contactId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TenantEmergencyContacts
            .AsNoTracking()
            .Where(ec => ec.Id == contactId && ec.TenantId == tenantId && ec.CompanyId == companyId && ec.DeletedAt == null)
            .Select(ec => new TenantEmergencyContactDto
            {
                Id = ec.Id,
                Name = ec.Name,
                RelationshipType = ec.RelationshipType,
                Phone = ec.Phone,
                CreatedAt = ec.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<TenantEmergencyContactDto>> GetEmergencyContactsForTenantAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TenantEmergencyContacts
            .AsNoTracking()
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
            .ToListAsync(cancellationToken);
    }

    public async Task AddEmergencyContactAsync(TenantEmergencyContact contact, CancellationToken cancellationToken = default)
    {
        await _dbContext.TenantEmergencyContacts.AddAsync(contact, cancellationToken);
    }

    public Task<TenantVehicle?> GetVehicleByIdAsync(Guid tenantId, Guid vehicleId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TenantVehicles
            .FirstOrDefaultAsync(v => v.Id == vehicleId && v.TenantId == tenantId && v.CompanyId == companyId && v.DeletedAt == null, cancellationToken);
    }

    public Task<TenantVehicleDto?> GetVehicleDtoByIdAsync(Guid tenantId, Guid vehicleId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TenantVehicles
            .AsNoTracking()
            .Where(v => v.Id == vehicleId && v.TenantId == tenantId && v.CompanyId == companyId && v.DeletedAt == null)
            .Select(v => new TenantVehicleDto
            {
                Id = v.Id,
                PlateNumber = v.PlateNumber,
                MakeModel = v.MakeModel,
                Color = v.Color,
                CreatedAt = v.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<TenantVehicleDto>> GetVehiclesForTenantAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TenantVehicles
            .AsNoTracking()
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
            .ToListAsync(cancellationToken);
    }

    public async Task AddVehicleAsync(TenantVehicle vehicle, CancellationToken cancellationToken = default)
    {
        await _dbContext.TenantVehicles.AddAsync(vehicle, cancellationToken);
    }

    public Task<bool> ExistsByPlateNumberAsync(Guid companyId, string plateNumber, Guid? excludeVehicleId = null, CancellationToken cancellationToken = default)
    {
        var trimmed = plateNumber.Trim();
        var query = _dbContext.TenantVehicles
            .AsNoTracking()
            .Where(v => v.CompanyId == companyId && v.DeletedAt == null && v.PlateNumber == trimmed);

        if (excludeVehicleId.HasValue)
        {
            query = query.Where(v => v.Id != excludeVehicleId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }
}
