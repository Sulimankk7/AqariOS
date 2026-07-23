using System.Collections.Generic;

namespace PropertyOS.Application.Files.Options;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public long MaxSizeBytes { get; set; } = 52_428_800; // Default 50 MB
    public int PreSignedUrlExpirationMinutes { get; set; } = 5;
    public string StorageBasePath { get; set; } = "storage";

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
