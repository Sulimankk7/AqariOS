using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PropertyOS.Application.Files.Options;
using PropertyOS.Application.Files.Services;

namespace PropertyOS.Infrastructure.Files.Services;

/// <summary>
/// Physical disk storage provider implementation of IStorageProvider.
/// Focused purely on binary I/O and signed URL creation.
///
/// SECURITY: every storage key is canonicalized and verified to resolve INSIDE the
/// storage root before any filesystem operation (path-traversal defense — storage keys
/// reach this class from client-supplied values via ConfirmFileUpload). Upload/download
/// URLs carry an HMAC token binding purpose + key + absolute expiry (IFileUrlSigner).
/// </summary>
public class PhysicalFileStorageProvider : IStorageProvider
{
    private readonly FileStorageOptions _options;
    private readonly IFileUrlSigner _urlSigner;

    public PhysicalFileStorageProvider(IOptions<FileStorageOptions> options, IFileUrlSigner urlSigner)
    {
        _options = options.Value;
        _urlSigner = urlSigner;
    }

    public async Task<string> SaveAsync(string storageKey, Stream content, string mimeType, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storageKey);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
        await content.CopyToAsync(fileStream, cancellationToken);

        return storageKey;
    }

    public Task<Stream> GetReadStreamAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storageKey);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Storage file '{storageKey}' was not found.");
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, true);
        return Task.FromResult(stream);
    }

    public Task<long> GetSizeAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storageKey);
        var info = new FileInfo(fullPath);
        if (!info.Exists)
        {
            throw new FileNotFoundException($"Storage file '{storageKey}' was not found.");
        }

        return Task.FromResult(info.Length);
    }

    public Task<string> GeneratePreSignedDownloadUrlAsync(string storageKey, string filename, int expirationMinutes, CancellationToken cancellationToken = default)
    {
        GetFullPath(storageKey); // key sanity/containment check before signing anything
        var expires = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes).ToUnixTimeSeconds();
        var token = _urlSigner.CreateToken("download", storageKey, expires);
        var url = $"/api/v1/files/download?key={Uri.EscapeDataString(storageKey)}&expires={expires}&sig={token}";
        return Task.FromResult(url);
    }

    public Task<string> GeneratePreSignedUploadUrlAsync(string storageKey, int expirationMinutes, CancellationToken cancellationToken = default)
    {
        GetFullPath(storageKey);
        var expires = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes).ToUnixTimeSeconds();
        var token = _urlSigner.CreateToken("upload", storageKey, expires);
        var url = $"/api/v1/files/upload?key={Uri.EscapeDataString(storageKey)}&expires={expires}&sig={token}";
        return Task.FromResult(url);
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storageKey);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storageKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Resolves a storage key to an absolute path and REJECTS any key that is rooted or
    /// escapes the storage root after canonicalization (../ traversal, rooted keys like
    /// C:\... or /etc/..., mixed separators). Storage keys are attacker-influenced input.
    /// </summary>
    private string GetFullPath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key cannot be blank.", nameof(storageKey));

        var normalizedKey = storageKey.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

        if (Path.IsPathRooted(normalizedKey))
            throw new ArgumentException("Storage key must be relative to the storage root.", nameof(storageKey));

        var basePath = Path.GetFullPath(Path.IsPathRooted(_options.StorageBasePath)
            ? _options.StorageBasePath
            : Path.Combine(Directory.GetCurrentDirectory(), _options.StorageBasePath));

        var fullPath = Path.GetFullPath(Path.Combine(basePath, normalizedKey));

        // Case sensitivity follows the platform: ignore-case on Windows only.
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var baseWithSeparator = basePath.EndsWith(Path.DirectorySeparatorChar) ? basePath : basePath + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(baseWithSeparator, comparison))
            throw new ArgumentException("Storage key escapes the storage root.", nameof(storageKey));

        return fullPath;
    }
}
