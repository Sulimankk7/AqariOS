using System;
using PropertyOS.Domain.Common;

namespace PropertyOS.Domain.Files.Entities;

/// <summary>
/// Central metadata entity for every uploaded file across PropertyOS.
/// Maps to PostgreSQL table: file_storage (Phase 1 §1.32, Backend Architecture §15).
/// </summary>
public class FileStorage : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid? UploadedBy { get; private set; }
    public string OriginalFilename { get; private set; } = null!;
    public string MimeType { get; private set; } = null!;
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    private FileStorage() { }

    public static FileStorage Create(
        Guid companyId,
        Guid? uploadedBy,
        string originalFilename,
        string mimeType,
        long sizeBytes,
        string storageKey,
        DateTimeOffset now,
        Guid? createdBy,
        Guid? id = null)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID must be specified.", nameof(companyId));

        if (string.IsNullOrWhiteSpace(originalFilename))
            throw new ArgumentException("Original filename cannot be blank.", nameof(originalFilename));

        if (string.IsNullOrWhiteSpace(mimeType))
            throw new ArgumentException("MIME type cannot be blank.", nameof(mimeType));

        if (sizeBytes <= 0)
            throw new ArgumentException("File size must be strictly positive.", nameof(sizeBytes));

        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key cannot be blank.", nameof(storageKey));

        return new FileStorage
        {
            // Caller-supplied id keeps the upload-request FileId (embedded in the storage
            // key) identical to the persisted row's Id; generated otherwise.
            Id = id ?? Guid.CreateVersion7(),
            CompanyId = companyId,
            UploadedBy = uploadedBy,
            OriginalFilename = originalFilename.Trim(),
            MimeType = mimeType.Trim().ToLowerInvariant(),
            SizeBytes = sizeBytes,
            StorageKey = storageKey.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void SoftDelete(DateTimeOffset now, Guid? deletedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("File storage record is already deleted.");

        DeletedAt = now;
        DeletedBy = deletedBy;
        UpdatedAt = now;
        UpdatedBy = deletedBy;
    }
}
