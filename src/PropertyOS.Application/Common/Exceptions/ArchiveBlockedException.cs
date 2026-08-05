using System;
using System.Collections.Generic;
using PropertyOS.Application.Common.Models;

namespace PropertyOS.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when an archive operation is blocked because active dependent entities still exist.
/// Maps to HTTP 422 Unprocessable Entity with a structured <c>dependencies</c> payload, giving the
/// frontend enough information to render a rich confirmation dialog that lists exactly what must be
/// resolved before the archive can proceed.
/// </summary>
public sealed class ArchiveBlockedException : Exception
{
    /// <summary>The entity type being archived (e.g. "Building", "Floor").</summary>
    public string EntityType { get; }

    /// <summary>The human-readable name of the entity being archived (e.g. "Al-Noor Tower").</summary>
    public string EntityName { get; }

    /// <summary>
    /// Ordered list of active dependency categories that are blocking the archive.
    /// Each entry carries a human-readable <see cref="ArchiveDependency.Type"/> and a
    /// non-zero <see cref="ArchiveDependency.Count"/>. The list is guaranteed to be
    /// non-empty when this exception is thrown.
    /// </summary>
    public IReadOnlyList<ArchiveDependency> Dependencies { get; }

    /// <param name="entityType">The type of the entity being archived.</param>
    /// <param name="entityName">The display name of the entity being archived.</param>
    /// <param name="dependencies">Non-empty list of blocking dependency groups.</param>
    public ArchiveBlockedException(
        string entityType,
        string entityName,
        IReadOnlyList<ArchiveDependency> dependencies)
        : base($"{entityType} \"{entityName}\" cannot be archived.")
    {
        EntityType = entityType;
        EntityName = entityName;
        Dependencies = dependencies;
    }
}
