using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PropertyOS.Domain.Common;
using PropertyOS.Domain.Maintenance.Enums;
using PropertyOS.Domain.Maintenance.Events;

namespace PropertyOS.Domain.Maintenance;

/// <summary>
/// Aggregate root for a reported maintenance issue. Spec §8.1.
///
/// <para>
/// Owns the full lifecycle of <see cref="MaintenanceRequestAttachment"/>,
/// <see cref="MaintenanceRequestComment"/>, and the production of
/// <see cref="MaintenanceStatusHistory"/> records. All child mutations
/// are routed through this aggregate to preserve invariants.
/// </para>
///
/// <para>
/// <b>Status lifecycle</b>: the transition graph is a static, domain-owned
/// dictionary (see <see cref="AllowedTransitions"/>). The handler never
/// validates transitions — it delegates entirely to <see cref="UpdateStatus"/>.
/// </para>
///
/// <para>
/// <b>Partitioning</b>: deliberately excluded per spec §8.6. The dominant
/// query ("Open Requests") filters on status, not date range, so a date-based
/// RANGE partition would not prune anything for this access pattern.
/// </para>
/// </summary>
public class MaintenanceRequest : ISoftDeletable
{
    // -------------------------------------------------------------------------
    // Domain-owned transition graph (spec §8.1 Business Rules)
    // Terminal states (Closed, Cancelled) have no outbound edges.
    // -------------------------------------------------------------------------
    private static readonly IReadOnlyDictionary<MaintenanceStatus, IReadOnlySet<MaintenanceStatus>> AllowedTransitions =
        new Dictionary<MaintenanceStatus, IReadOnlySet<MaintenanceStatus>>
        {
            [MaintenanceStatus.Open]       = new HashSet<MaintenanceStatus> { MaintenanceStatus.InProgress, MaintenanceStatus.Waiting, MaintenanceStatus.Cancelled },
            [MaintenanceStatus.InProgress] = new HashSet<MaintenanceStatus> { MaintenanceStatus.Waiting, MaintenanceStatus.Resolved, MaintenanceStatus.Cancelled },
            [MaintenanceStatus.Waiting]    = new HashSet<MaintenanceStatus> { MaintenanceStatus.InProgress, MaintenanceStatus.Resolved, MaintenanceStatus.Cancelled },
            [MaintenanceStatus.Resolved]   = new HashSet<MaintenanceStatus> { MaintenanceStatus.Closed, MaintenanceStatus.InProgress },
            [MaintenanceStatus.Closed]     = new HashSet<MaintenanceStatus>(), // terminal
            [MaintenanceStatus.Cancelled]  = new HashSet<MaintenanceStatus>()  // terminal
        };

    private readonly List<MaintenanceRequestAttachment> _attachments = new();
    private readonly List<MaintenanceRequestComment> _comments = new();
    private readonly List<MaintenanceRequestStatusChangedEvent> _domainEvents = new();

    // -------------------------------------------------------------------------
    // Scalar properties
    // -------------------------------------------------------------------------

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }

    /// <summary>Mandatory — every request is at minimum building-scoped (spec §8.0).</summary>
    public Guid BuildingId { get; private set; }

    /// <summary>NULL for common-area/building-wide issues (spec §8.0).</summary>
    public Guid? ApartmentId { get; private set; }

    /// <summary>NULL for staff-initiated or common-area requests (spec §8.0).</summary>
    public Guid? TenantId { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public MaintenanceCategory Category { get; private set; }
    public MaintenancePriority Priority { get; private set; }
    public MaintenanceStatus Status { get; private set; }

    /// <summary>Historical fact, not a scheduling field. Cannot be future-dated (spec §8.1).</summary>
    public DateOnly RequestDate { get; private set; }

    /// <summary>
    /// Set only when <see cref="Status"/> is <see cref="MaintenanceStatus.Closed"/> or
    /// <see cref="MaintenanceStatus.Cancelled"/>. Cleared on re-open.
    /// </summary>
    public DateOnly? ClosedDate { get; private set; }

    /// <summary>
    /// Single mutable staff scratchpad — distinct from <see cref="Comments"/>
    /// (spec §8.0). Overwritten in place; no per-entry attribution or history.
    /// </summary>
    public string? InternalNotes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Doubles as the "Created By" business field per spec §8.0.</summary>
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// PostgreSQL system column for optimistic concurrency on concurrent updates.
    /// Mapped with <c>.IsRowVersion()</c> in EF Core configuration.
    /// </summary>
    public uint xmin { get; private set; }

    // -------------------------------------------------------------------------
    // Navigation (read-only wrappers — EF populates the backing lists)
    // -------------------------------------------------------------------------

    public IReadOnlyCollection<MaintenanceRequestAttachment> Attachments => _attachments.AsReadOnly();
    public IReadOnlyCollection<MaintenanceRequestComment> Comments => _comments.AsReadOnly();

    /// <summary>Pending domain events. Cleared after dispatch.</summary>
    public IReadOnlyCollection<MaintenanceRequestStatusChangedEvent> DomainEvents => _domainEvents.AsReadOnly();

    private MaintenanceRequest() { }

    // -------------------------------------------------------------------------
    // Factory
    // -------------------------------------------------------------------------

    public static MaintenanceRequest Create(
        Guid companyId,
        Guid buildingId,
        Guid? apartmentId,
        Guid? tenantId,
        string title,
        string description,
        MaintenanceCategory category,
        MaintenancePriority priority,
        DateOnly requestDate,
        DateTimeOffset now,
        Guid? createdBy)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID must be specified.", nameof(companyId));

        if (buildingId == Guid.Empty)
            throw new ArgumentException("Building ID must be specified.", nameof(buildingId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title must not be blank.", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description must not be blank.", nameof(description));

        if (requestDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("Request date cannot be in the future.", nameof(requestDate));

        return new MaintenanceRequest
        {
            // Client-generated UUIDv7 (uniform platform pattern): the ID must exist before
            // TransactionBehavior's SaveChanges so child rows and command return values can use it.
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            BuildingId = buildingId,
            ApartmentId = apartmentId,
            TenantId = tenantId,
            Title = title.Trim(),
            Description = description.Trim(),
            Category = category,
            Priority = priority,
            Status = MaintenanceStatus.Open,
            RequestDate = requestDate,
            ClosedDate = null,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    // -------------------------------------------------------------------------
    // State mutations
    // -------------------------------------------------------------------------

    /// <summary>
    /// Updates the mutable descriptive fields of the request.
    /// <c>RequestDate</c> and <c>Status</c> are NOT modifiable via this path.
    /// <c>ApartmentId</c> and <c>TenantId</c> may be corrected (spec §8.1 Business Rules).
    /// </summary>
    public void UpdateDetails(
        Guid buildingId,
        Guid? apartmentId,
        Guid? tenantId,
        string title,
        string description,
        MaintenanceCategory category,
        MaintenancePriority priority,
        string? internalNotes,
        DateTimeOffset updatedAt,
        Guid? updatedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot modify a deleted maintenance request.");

        if (buildingId == Guid.Empty)
            throw new ArgumentException("Building ID must be specified.", nameof(buildingId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title must not be blank.", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description must not be blank.", nameof(description));

        BuildingId = buildingId;
        ApartmentId = apartmentId;
        TenantId = tenantId;
        Title = title.Trim();
        Description = description.Trim();
        Category = category;
        Priority = priority;
        InternalNotes = internalNotes?.Trim();
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Transitions the request to <paramref name="newStatus"/>, creates the corresponding
    /// <see cref="MaintenanceStatusHistory"/> row, and raises <see cref="MaintenanceRequestStatusChangedEvent"/>.
    ///
    /// <para>
    /// The caller (command handler) must persist the returned history entry in the
    /// same Unit-of-Work as the aggregate root update.
    /// </para>
    /// </summary>
    /// <returns>The history entry that must be persisted by the caller.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the transition is not allowed by the domain transition graph.
    /// </exception>
    public MaintenanceStatusHistory UpdateStatus(
        MaintenanceStatus newStatus,
        DateTimeOffset changedAt,
        Guid? changedBy,
        string? reason = null)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot change the status of a deleted maintenance request.");

        if (Status == newStatus)
            throw new InvalidOperationException($"Request is already in status '{Status}'.");

        if (!AllowedTransitions[Status].Contains(newStatus))
            throw new InvalidOperationException(
                $"Transition from '{Status}' to '{newStatus}' is not permitted.");

        var previousStatus = Status;
        Status = newStatus;
        UpdatedAt = changedAt;
        UpdatedBy = changedBy;

        // Terminal states record the closure date (spec §8.1 check constraints)
        if (newStatus is MaintenanceStatus.Closed or MaintenanceStatus.Cancelled)
            ClosedDate = DateOnly.FromDateTime(changedAt.UtcDateTime);

        // Re-opening from Resolved → InProgress clears ClosedDate
        if (newStatus == MaintenanceStatus.InProgress && previousStatus == MaintenanceStatus.Resolved)
            ClosedDate = null;

        var history = MaintenanceStatusHistory.Create(
            companyId: CompanyId,
            maintenanceRequestId: Id,
            newStatus: newStatus,
            changedAt: changedAt,
            previousStatus: previousStatus,
            changedBy: changedBy,
            reason: reason);

        _domainEvents.Add(new MaintenanceRequestStatusChangedEvent(
            RequestId: Id,
            CompanyId: CompanyId,
            OldStatus: previousStatus,
            NewStatus: newStatus,
            ChangedAt: changedAt));

        return history;
    }

    /// <summary>Clears all pending domain events after they have been dispatched.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    // -------------------------------------------------------------------------
    // Attachment management
    // -------------------------------------------------------------------------

    /// <summary>
    /// Attaches a file to this request. Rejects duplicate active file references
    /// per the unique constraint <c>uq_maintenance_request_attachments_request_file</c> (spec §8.2).
    /// </summary>
    public MaintenanceRequestAttachment AddAttachment(
        Guid? fileId,
        Guid? uploadedBy,
        string? description,
        DateTimeOffset now,
        Guid? createdBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot attach a file to a deleted maintenance request.");

        if (fileId.HasValue && fileId.Value == Guid.Empty)
            throw new ArgumentException("File ID must be specified.", nameof(fileId));

        if (fileId.HasValue && _attachments.Any(a => a.FileId == fileId && a.DeletedAt == null))
            throw new InvalidOperationException("This file is already attached to this maintenance request.");

        var attachment = MaintenanceRequestAttachment.Create(
            companyId: CompanyId,
            maintenanceRequestId: Id,
            fileId: fileId,
            uploadedBy: uploadedBy,
            description: description,
            now: now,
            createdBy: createdBy);

        _attachments.Add(attachment);
        UpdatedAt = now;
        UpdatedBy = createdBy;

        return attachment;
    }

    /// <summary>Soft-deletes an active attachment.</summary>
    public void RemoveAttachment(Guid attachmentId, DateTimeOffset now, Guid? deletedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot remove an attachment from a deleted maintenance request.");

        var attachment = _attachments.FirstOrDefault(a => a.Id == attachmentId && a.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Attachment '{attachmentId}' was not found on this request.");

        attachment.SoftDelete(now, deletedBy);
        UpdatedAt = now;
        UpdatedBy = deletedBy;
    }

    // -------------------------------------------------------------------------
    // Comment management
    // -------------------------------------------------------------------------

    /// <summary>Creates and registers a new comment on this request.</summary>
    public MaintenanceRequestComment AddComment(
        string commentText,
        DateTimeOffset now,
        Guid? createdBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot add a comment to a deleted maintenance request.");

        var comment = MaintenanceRequestComment.Create(
            companyId: CompanyId,
            maintenanceRequestId: Id,
            commentText: commentText,
            now: now,
            createdBy: createdBy);

        _comments.Add(comment);
        UpdatedAt = now;
        UpdatedBy = createdBy;

        return comment;
    }

    /// <summary>Edits the text of an existing active comment.</summary>
    public void EditComment(Guid commentId, string newText, DateTimeOffset updatedAt, Guid? updatedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot edit a comment on a deleted maintenance request.");

        var comment = _comments.FirstOrDefault(c => c.Id == commentId && c.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Comment '{commentId}' was not found on this request.");

        comment.Edit(newText, updatedAt, updatedBy);
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    /// <summary>Soft-deletes an active comment.</summary>
    public void RemoveComment(Guid commentId, DateTimeOffset now, Guid? deletedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot remove a comment from a deleted maintenance request.");

        var comment = _comments.FirstOrDefault(c => c.Id == commentId && c.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Comment '{commentId}' was not found on this request.");

        comment.SoftDelete(now, deletedBy);
        UpdatedAt = now;
        UpdatedBy = deletedBy;
    }

    // -------------------------------------------------------------------------
    // Soft delete
    // -------------------------------------------------------------------------

    /// <summary>
    /// Soft-deletes this request and cascades to all active attachments and comments.
    /// Status history is preserved permanently (append-only, not cascaded).
    /// </summary>
    public void SoftDelete(DateTimeOffset deletedAt, Guid? deletedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Maintenance request is already deleted.");

        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;

        foreach (var attachment in _attachments.Where(a => a.DeletedAt == null))
            attachment.SoftDelete(deletedAt, deletedBy);

        foreach (var comment in _comments.Where(c => c.DeletedAt == null))
            comment.SoftDelete(deletedAt, deletedBy);
    }
}
