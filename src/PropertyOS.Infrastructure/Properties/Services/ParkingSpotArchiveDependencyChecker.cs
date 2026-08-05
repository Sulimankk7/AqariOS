using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Models;
using PropertyOS.Application.Properties.ParkingSpots.Services;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Properties.Services;

/// <summary>
/// Counts every category of active dependency that must be resolved before a ParkingSpot can be archived.
/// All queries use the DbContext's global query filters, so only non-deleted (active) records are counted.
/// Returns dependencies in a fixed business order with stable machine-readable codes and guidance examples.
/// </summary>
public sealed class ParkingSpotArchiveDependencyChecker : IParkingSpotArchiveDependencyChecker
{
    private readonly PropertyOsDbContext _db;

    public ParkingSpotArchiveDependencyChecker(PropertyOsDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<IReadOnlyList<ArchiveDependency>> GetActiveDependenciesAsync(
        Guid parkingSpotId,
        CancellationToken cancellationToken = default)
    {
        // 7. Parking Assignments
        int parkingAssignments = await _db.ParkingAssignments
            .Where(pa => pa.ParkingSpotId == parkingSpotId)
            .CountAsync(cancellationToken);

        var dependencies = new List<ArchiveDependency>(1);

        if (parkingAssignments > 0)
            dependencies.Add(new ArchiveDependency("PARKING_ASSIGNMENTS", "Parking Assignments", parkingAssignments));

        return dependencies.AsReadOnly();
    }
}
