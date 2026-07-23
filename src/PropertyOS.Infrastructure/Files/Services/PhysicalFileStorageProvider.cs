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
/// Focused purely on binary I/O and pre-signed URL token creation.
/// </summary>
public class PhysicalFileStorageProvider : IStorageProvider
{
    private readonly FileStorageOptions _options;

    public PhysicalFileStorageProvider(IOptions<FileStorageOptions> options)
    {
        _options = options.Value;
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
            throw new FileNotFoundException($"Storage file '{storageKey}' was not found at path '{fullPath}'.");
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, true);
        return Task.FromResult(stream);
    }

    public Task<string> GeneratePreSignedDownloadUrlAsync(string storageKey, string filename, int expirationMinutes, CancellationToken cancellationToken = default)
    {
        // Generates pre-signed URL format for file download endpoint with short-lived token parameter
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        var encodedKey = Uri.EscapeDataString(storageKey);
        var url = $"/api/v1/files/download?key={encodedKey}&token={token}&expires={expirationMinutes}";
        return Task.FromResult(url);
    }

    public Task<string> GeneratePreSignedUploadUrlAsync(string storageKey, int expirationMinutes, CancellationToken cancellationToken = default)
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        var encodedKey = Uri.EscapeDataString(storageKey);
        var url = $"/api/v1/files/upload?key={encodedKey}&token={token}&expires={expirationMinutes}";
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

    private string GetFullPath(string storageKey)
    {
        var basePath = Path.IsPathRooted(_options.StorageBasePath)
            ? _options.StorageBasePath
            : Path.Combine(Directory.GetCurrentDirectory(), _options.StorageBasePath);

        var normalizedKey = storageKey.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        return Path.Combine(basePath, normalizedKey);
    }
}
