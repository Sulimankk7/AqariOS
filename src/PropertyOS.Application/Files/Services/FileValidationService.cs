using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PropertyOS.Application.Files.Options;

namespace PropertyOS.Application.Files.Services;

public class FileValidationService : IFileValidationService
{
    private readonly FileStorageOptions _options;

    public FileValidationService(FileStorageOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public void ValidateFile(string filename, string mimeType, long sizeBytes, Stream headerStream)
    {
        if (string.IsNullOrWhiteSpace(filename))
            throw new ArgumentException("Filename cannot be empty.", nameof(filename));

        var sanitized = SanitizeFilename(filename);
        var extension = Path.GetExtension(sanitized).ToLowerInvariant();

        if (string.IsNullOrEmpty(extension))
            throw new ArgumentException("File must have a valid extension.", nameof(filename));

        // Prevent double extension attacks (e.g. filename.exe.pdf or filename.pdf.exe)
        var nameWithoutLastExt = Path.GetFileNameWithoutExtension(sanitized);
        var secondExt = Path.GetExtension(nameWithoutLastExt);
        if (!string.IsNullOrEmpty(secondExt) && (IsDangerousExtension(secondExt) || IsDangerousExtension(extension)))
        {
            throw new ArgumentException("Double extension attacks are forbidden.", nameof(filename));
        }

        if (IsDangerousExtension(extension))
        {
            throw new ArgumentException($"Files with extension '{extension}' are forbidden.", nameof(filename));
        }

        if (!_options.AllowedExtensionsToMimeTypes.TryGetValue(extension, out var allowedMimes))
        {
            throw new ArgumentException($"Extension '{extension}' is not allowed.", nameof(filename));
        }

        var normalizedMime = mimeType?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalizedMime) || !allowedMimes.Contains(normalizedMime, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"MIME type '{mimeType}' does not match allowed MIME types for extension '{extension}'.", nameof(mimeType));
        }

        if (sizeBytes <= 0)
        {
            throw new ArgumentException("File size must be greater than zero.", nameof(sizeBytes));
        }

        if (sizeBytes > _options.MaxSizeBytes)
        {
            throw new ArgumentException($"File size ({sizeBytes} bytes) exceeds maximum allowed size ({_options.MaxSizeBytes} bytes).", nameof(sizeBytes));
        }

        // Magic byte verification if header stream is provided
        if (headerStream != null && headerStream.CanRead)
        {
            VerifyMagicBytes(extension, headerStream);
        }
    }

    public string SanitizeFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
            return "file";

        // Remove path information
        var clean = Path.GetFileName(filename);

        // Remove path traversal patterns
        clean = clean.Replace("..", "").Replace("/", "").Replace("\\", "");

        // Remove invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        clean = new string(clean.Where(c => !invalidChars.Contains(c)).ToArray());

        if (string.IsNullOrWhiteSpace(clean))
            return "file";

        return clean;
    }

    public string GenerateStorageKey(Guid companyId, string moduleName, Guid entityId, Guid fileId, string sanitizedFilename)
    {
        var safeModule = Regex.Replace(moduleName ?? "general", @"[^a-zA-Z0-9_\-]", "");
        return $"{companyId}/{safeModule}/{entityId}/{fileId}-{sanitizedFilename}";
    }

    private static bool IsDangerousExtension(string ext)
    {
        var dangerous = new[] { ".exe", ".bat", ".cmd", ".sh", ".php", ".asp", ".aspx", ".js", ".vbs", ".jar", ".py", ".ps1", ".dll", ".so", ".dylib" };
        return dangerous.Contains(ext.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase);
    }

    private static void VerifyMagicBytes(string extension, Stream stream)
    {
        var buffer = new byte[8];
        var originalPosition = stream.CanSeek ? stream.Position : 0;
        var bytesRead = stream.Read(buffer, 0, buffer.Length);

        if (stream.CanSeek)
        {
            stream.Position = originalPosition;
        }

        if (bytesRead < 4)
            return; // Insufficient data to verify header

        switch (extension.ToLowerInvariant())
        {
            case ".pdf":
                // PDF header starts with %PDF (0x25, 0x50, 0x44, 0x46)
                if (buffer[0] != 0x25 || buffer[1] != 0x50 || buffer[2] != 0x44 || buffer[3] != 0x46)
                    throw new ArgumentException("File content does not match PDF signature.", nameof(extension));
                break;
            case ".png":
                // PNG header 0x89 0x50 0x4E 0x47
                if (buffer[0] != 0x89 || buffer[1] != 0x50 || buffer[2] != 0x4E || buffer[3] != 0x47)
                    throw new ArgumentException("File content does not match PNG signature.", nameof(extension));
                break;
            case ".jpg":
            case ".jpeg":
                // JPEG header 0xFF 0xD8 0xFF
                if (buffer[0] != 0xFF || buffer[1] != 0xD8 || buffer[2] != 0xFF)
                    throw new ArgumentException("File content does not match JPEG signature.", nameof(extension));
                break;
        }
    }
}
