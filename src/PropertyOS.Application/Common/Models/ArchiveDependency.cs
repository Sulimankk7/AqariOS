using System.Collections.Generic;

namespace PropertyOS.Application.Common.Models;

/// <summary>
/// Represents a single category of active dependency that is blocking an archive operation.
/// Serialized as part of the <see cref="Exceptions.ArchiveBlockedException"/> payload.
/// </summary>
/// <param name="Code">Stable machine-readable code for the dependency type (e.g. "FLOORS", "LEASE_CONTRACTS").</param>
/// <param name="Label">Human-readable label for display (e.g. "Floors", "Lease Contracts").</param>
/// <param name="Count">Number of active (non-deleted) records blocking the archive.</param>
/// <param name="Examples">Optional guidance list of up to 3-5 human-readable example names/codes.</param>
public sealed record ArchiveDependency(
    string Code,
    string Label,
    int Count,
    IReadOnlyList<string>? Examples = null);
