using System;
using PropertyOS.Domain.Common;

namespace PropertyOS.Domain.Maintenance;

/// <summary>
/// An uploaded file attached to a <see cref="MaintenanceRequest"/> as supporting evidence
/// (photos, videos, vendor documents). Spec §8.2.
///
/// <para>
/// <c>UploadedBy</c> is intentionally separate from <c>CreatedBy</c> (spec §8.2 Business Rules):
/// the uploader and the row-inserting actor can legitimately diverge for tenant-portal uploads
/// and background ingestion jobs.
/// </para>
/// </summary>
public class MaintenanceRequestAttachment : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid MaintenanceRequestId { get; private set; }

    /// <summary>FK to <c>file_storage.id</c>. Nullable because the file subsystem is not yet implemented.</summary>
    public Guid? FileId { get; private set; }

    /// <summary>Optional free-form label, e.g. "Before photo — kitchen sink".</summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Who performed the upload action. Nullable to support system imports and
    /// background jobs that do not have a direct human actor. Deliberately kept
    /// separate from <c>CreatedBy</c> per spec §8.2.
    /// </summary>
    public Guid? UploadedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    private MaintenanceRequestAttachment() { }

    public static MaintenanceRequestAttachment Create(
        Guid companyId,
        Guid maintenanceRequestId,
        Guid? fileId,
        Guid? uploadedBy,
        string? description,
        DateTimeOffset now,
        Guid? createdBy)
    {
        return new MaintenanceRequestAttachment
        {
            // Client-generated UUIDv7 (uniform platform pattern): the ID must exist before
            // TransactionBehavior's SaveChanges so child rows and command return values can use it.
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            MaintenanceRequestId = maintenanceRequestId,
            FileId = fileId,
            UploadedBy = uploadedBy,
            Description = description?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    internal void SoftDelete(DateTimeOffset deletedAt, Guid? deletedBy)
    {
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;
    }
}
