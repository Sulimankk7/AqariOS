using System;
using PropertyOS.Domain.Common;

namespace PropertyOS.Domain.Maintenance;

/// <summary>
/// An individually authored, timestamped comment on a <see cref="MaintenanceRequest"/>,
/// forming the request's threaded discussion log. Spec §8.3.
///
/// <para>
/// Distinct from <c>MaintenanceRequest.InternalNotes</c>: comments are independently
/// authored entries with their own identity; <c>InternalNotes</c> is a single mutable
/// staff scratchpad with no independent history.
/// </para>
/// </summary>
public class MaintenanceRequestComment : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid MaintenanceRequestId { get; private set; }
    public string CommentText { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Doubles as the "Author" business field per spec §8.0.</summary>
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// PostgreSQL system column for optimistic concurrency on comment edits.
    /// Mapped with <c>.IsRowVersion()</c> in EF Core configuration.
    /// </summary>
    public uint xmin { get; private set; }

    private MaintenanceRequestComment() { }

    public static MaintenanceRequestComment Create(
        Guid companyId,
        Guid maintenanceRequestId,
        string commentText,
        DateTimeOffset now,
        Guid? createdBy)
    {
        if (string.IsNullOrWhiteSpace(commentText))
            throw new ArgumentException("Comment text must not be blank.", nameof(commentText));

        return new MaintenanceRequestComment
        {
            Id = Guid.Empty,
            CompanyId = companyId,
            MaintenanceRequestId = maintenanceRequestId,
            CommentText = commentText.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    /// <summary>
    /// Corrects the comment text (typo fix). Does not create a new row;
    /// the original text is lost (use status history pattern for immutable
    /// audit trails — comments are explicitly mutable per spec §8.3).
    /// </summary>
    internal void Edit(string newText, DateTimeOffset updatedAt, Guid? updatedBy)
    {
        if (string.IsNullOrWhiteSpace(newText))
            throw new ArgumentException("Comment text must not be blank.", nameof(newText));

        CommentText = newText.Trim();
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    internal void SoftDelete(DateTimeOffset deletedAt, Guid? deletedBy)
    {
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;
    }
}
