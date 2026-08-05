using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Models;

namespace PropertyOS.Application.Properties.Buildings.Services;

/// <summary>
/// Queries all active dependency counts for a <c>Building</c> before an archive operation.
/// Implementations run COUNT queries against live data; no entities are materialised.
/// </summary>
public interface IBuildingArchiveDependencyChecker
{
    /// <summary>
    /// Returns a list of <see cref="ArchiveDependency"/> entries whose <c>Count</c> is greater
    /// than zero. An empty list means the building has no active dependencies and may be archived.
    /// </summary>
    /// <param name="buildingId">The building being evaluated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ArchiveDependency>> GetActiveDependenciesAsync(
        Guid buildingId,
        CancellationToken cancellationToken = default);
}
