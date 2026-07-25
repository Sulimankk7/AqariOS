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

public class FloorRepository : IFloorRepository
{
    private readonly PropertyOsDbContext _dbContext;

    public FloorRepository(PropertyOsDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public Task<Floor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Floors
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task AddAsync(Floor floor, CancellationToken cancellationToken = default)
    {
        if (floor == null) throw new ArgumentNullException(nameof(floor));
        await _dbContext.Floors.AddAsync(floor, cancellationToken);
    }

    public Task<bool> ExistsByFloorNumberAsync(Guid buildingId, short floorNumber, CancellationToken cancellationToken = default)
    {
        return _dbContext.Floors
            .AnyAsync(f => f.BuildingId == buildingId && f.FloorNumber == floorNumber, cancellationToken);
    }

    public Task<List<Floor>> ListByBuildingIdAsync(Guid buildingId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Floors
            .AsNoTracking()
            .Where(f => f.BuildingId == buildingId)
            .OrderBy(f => f.FloorNumber)
            .ToListAsync(cancellationToken);
    }
}
