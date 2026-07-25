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

public class ParkingSpotRepository : IParkingSpotRepository
{
    private readonly PropertyOsDbContext _dbContext;

    public ParkingSpotRepository(PropertyOsDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public Task<ParkingSpot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.ParkingSpots
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task AddAsync(ParkingSpot spot, CancellationToken cancellationToken = default)
    {
        if (spot == null) throw new ArgumentNullException(nameof(spot));
        await _dbContext.ParkingSpots.AddAsync(spot, cancellationToken);
    }

    public Task<bool> ExistsBySpotCodeAsync(Guid buildingId, string spotCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(spotCode)) return Task.FromResult(false);

        var normalizedCode = spotCode.Trim().ToLower();
        return _dbContext.ParkingSpots
            .AnyAsync(p => p.BuildingId == buildingId && p.SpotCode.ToLower() == normalizedCode, cancellationToken);
    }

    public Task<List<ParkingSpot>> ListByBuildingIdAsync(Guid buildingId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ParkingSpots
            .AsNoTracking()
            .Where(p => p.BuildingId == buildingId)
            .OrderBy(p => p.SpotCode)
            .ToListAsync(cancellationToken);
    }
}
