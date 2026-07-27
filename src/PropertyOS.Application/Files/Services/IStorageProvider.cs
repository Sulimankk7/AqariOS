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
    Task<string> GeneratePreSignedDownloadUrlAsync(string storageKey, string filename, int expirationMinutes, CancellationToken cancellationToken = default);
    Task<string> GeneratePreSignedUploadUrlAsync(string storageKey, int expirationMinutes, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>Actual stored object size in bytes — used to verify client-declared sizes.</summary>
    Task<long> GetSizeAsync(string storageKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
