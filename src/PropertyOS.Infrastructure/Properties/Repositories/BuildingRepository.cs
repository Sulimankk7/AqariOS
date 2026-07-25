using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Properties;
using PropertyOS.Domain.Properties;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Properties.Repositories;

public class BuildingRepository : IBuildingRepository
{
    private readonly PropertyOsDbContext _dbContext;

    public BuildingRepository(PropertyOsDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public Task<Building?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Buildings
            .Include(b => b.Address)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task AddAsync(Building building, CancellationToken cancellationToken = default)
    {
        if (building == null) throw new ArgumentNullException(nameof(building));
        await _dbContext.Buildings.AddAsync(building, cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(Guid companyId, string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return Task.FromResult(false);

        var normalizedName = name.Trim().ToLower();
        return _dbContext.Buildings
            .AnyAsync(b => b.CompanyId == companyId && b.Name.ToLower() == normalizedName, cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(Guid companyId, string internalCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(internalCode)) return Task.FromResult(false);

        var normalizedCode = internalCode.Trim().ToLower();
        return _dbContext.Buildings
            .AnyAsync(b => b.CompanyId == companyId && b.InternalCode != null && b.InternalCode.ToLower() == normalizedCode, cancellationToken);
    }

    public Task<List<Building>> ListByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Buildings
            .AsNoTracking()
            .Include(b => b.Address)
            .Where(b => b.CompanyId == companyId)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetCountByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Buildings
            .CountAsync(b => b.CompanyId == companyId, cancellationToken);
    }
}
