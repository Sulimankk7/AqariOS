using System;
using System.Threading;
using System.Threading.Tasks;

namespace PropertyOS.Application.Common.Interfaces;

/// <summary>
/// Service boundary interface to validate files within the file storage subsystem.
/// Used to decouple the Marketplace from the physical file storage implementation (Module 10).
/// </summary>
public interface IFileStorageValidator
{
    /// <summary>
    /// Validates that an image file exists, is not soft-deleted, is designated as an image,
    /// and belongs to the owning company.
    /// </summary>
    Task<bool> ValidateImageFileAsync(Guid fileId, Guid companyId, CancellationToken cancellationToken = default);
}
