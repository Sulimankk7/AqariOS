using System.Collections.Generic;

namespace PropertyOS.Application.Files.Options;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Selects the active storage backend. Supported values: "Physical" (default), "AzureBlob".
    /// </summary>
    public string Provider { get; set; } = "Physical";

    // ── Azure Blob Storage settings ──────────────────────────────────────────────
    // Required when Provider = "AzureBlob". Ignored for Physical.

    /// <summary>
    /// Azure Storage connection string. Required for AzureBlob provider.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Azure Blob container name. The container must already exist; it will not be
    /// created automatically. Required for AzureBlob provider.
    /// </summary>
    public string? ContainerName { get; set; }

    // ── Shared settings ──────────────────────────────────────────────────────────

    public long MaxSizeBytes { get; set; } = 52_428_800; // Default 50 MB
    public int PreSignedUrlExpirationMinutes { get; set; } = 5;
    public string StorageBasePath { get; set; } = "storage";

    /// <summary>
    /// Retention period in days for soft-deleted blobs before physical purge by the Storage Retention Job.
    /// Default: 30 days.
    /// TODO [Production Component]: A background Retention Job must be implemented prior to release
    /// to query soft-deleted records older than BlobRetentionDays and invoke IStorageProvider.DeleteAsync.
    /// </summary>
    public int BlobRetentionDays { get; set; } = 30;


    /// <summary>
    /// HMAC key for signed upload/download URLs. No default on purpose: file URL
    /// generation and verification fail closed until a 32+ byte secret is configured.
    /// Used by the Physical provider. Not required for AzureBlob (SAS tokens replace it).
    /// </summary>
    public string? UrlSigningSecret { get; set; }

    public Dictionary<string, string[]> AllowedExtensionsToMimeTypes { get; set; } = new()
    {
        { ".pdf", new[] { "application/pdf" } },
        { ".png", new[] { "image/png" } },
        { ".jpg", new[] { "image/jpeg" } },
        { ".jpeg", new[] { "image/jpeg" } },
        { ".webp", new[] { "image/webp" } },
        { ".docx", new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" } },
        { ".xlsx", new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } }
    };
}
