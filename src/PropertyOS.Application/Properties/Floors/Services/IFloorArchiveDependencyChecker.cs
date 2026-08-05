using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Models;

namespace PropertyOS.Application.Properties.Floors.Services;

/// <summary>
/// Queries all active dependency counts for a <c>Floor</c> before an archive operation.
/// Implementations run COUNT queries against live data; no entities are materialised.
/// </summary>
public interface IFloorArchiveDependencyChecker
{
    /// <summary>
    /// Returns a list of <see cref="ArchiveDependency"/> entries whose <c>Count</c> is greater
    /// than zero. An empty list means the floor has no active dependencies and may be archived.
    /// </summary>
    /// <param name="floorId">The floor being evaluated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ArchiveDependency>> GetActiveDependenciesAsync(
        Guid floorId,
        CancellationToken cancellationToken = default);
}
