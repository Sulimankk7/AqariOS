using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PropertyOS.Application.Files.Services;

/// <summary>
/// Low-level object storage provider contract (S3, Azure Blob, MinIO, or Local Disk).
/// Focused purely on binary I/O and pre-signed URL generation.
/// </summary>
public interface IStorageProvider
{
    Task<string> SaveAsync(string storageKey, Stream content, string mimeType, CancellationToken cancellationToken = default);
    Task<Stream> GetReadStreamAsync(string storageKey, CancellationToken cancellationToken = default);
    Task<string> GeneratePreSignedDownloadUrlAsync(string storageKey, string filename, int expirationMinutes, bool inline = false, CancellationToken cancellationToken = default);

    Task<string> GeneratePreSignedUploadUrlAsync(string storageKey, int expirationMinutes, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>Actual stored object size in bytes — used to verify client-declared sizes.</summary>
    Task<long> GetSizeAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Physically deletes a stored object from the active storage provider.
    /// Note: Under the AqariOS Deferred Blob Deletion Policy, physical deletion must NOT be invoked
    /// synchronously on user-facing document soft-deletes. It is reserved for execution by the background
    /// Storage Retention Job after the configured retention period (FileStorageOptions.BlobRetentionDays) expires.
    /// TODO [Production Component]: Storage Retention Job (scheduled background service) will invoke this method.
    /// </summary>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}

