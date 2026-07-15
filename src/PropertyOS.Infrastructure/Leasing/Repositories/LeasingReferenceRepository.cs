using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Leasing;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Leasing.Repositories;

public class LeasingReferenceRepository : ILeasingReferenceRepository
{
    private readonly PropertyOsDbContext _dbContext;

    public LeasingReferenceRepository(PropertyOsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid?> GetApartmentBuildingIdAsync(Guid apartmentId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var apt = await _dbContext.Apartments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == apartmentId && a.CompanyId == companyId, cancellationToken);
        return apt?.BuildingId;
    }

    public Task<bool> TenantExistsAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Tenants.AnyAsync(t => t.Id == tenantId && t.CompanyId == companyId, cancellationToken);
    }
}
