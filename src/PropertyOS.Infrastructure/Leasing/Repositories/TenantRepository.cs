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
}
