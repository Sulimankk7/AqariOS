using System;
using PropertyOS.Domain.Audit.Attributes;
using PropertyOS.Domain.Common;
using PropertyOS.Domain.Common.ValueObjects;
using PropertyOS.Domain.Marketplace.Enums;

namespace PropertyOS.Domain.Marketplace;

/// <summary>
/// Aggregate root representing a request by a prospect to view a property listing.
/// Maps to the PostgreSQL table: viewing_requests (Module 9 §9.3).
/// </summary>
public class ViewingRequest : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid ListingId { get; private set; }

    [Sensitive]
    public string ApplicantName { get; private set; } = string.Empty;

    [Sensitive]
    public PhoneNumber PhoneNumber { get; private set; } = null!;

    [Sensitive]
    public string? Email { get; private set; }

    public DateOnly? PreferredViewingDate { get; private set; }
    public string? Notes { get; private set; } // Notes provided by the prospect
    public string? StaffNotes { get; private set; } // Internal notes by staff
    public ViewingRequestStatus RequestStatus { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// PostgreSQL system column for optimistic concurrency.
    /// </summary>
    public uint xmin { get; private set; }

    private ViewingRequest() { }

    public static ViewingRequest Create(
        Guid companyId,
        Guid listingId,
        string applicantName,
        PhoneNumber phoneNumber,
        string? email,
        DateOnly? preferredViewingDate,
        string? notes,
        DateTimeOffset now,
        Guid? createdBy)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID must be specified.", nameof(companyId));

        if (listingId == Guid.Empty)
            throw new ArgumentException("Listing ID must be specified.", nameof(listingId));

        if (string.IsNullOrWhiteSpace(applicantName))
            throw new ArgumentException("Applicant name cannot be empty.", nameof(applicantName));

        if (phoneNumber == null)
            throw new ArgumentNullException(nameof(phoneNumber));

        if (preferredViewingDate.HasValue && preferredViewingDate.Value < DateOnly.FromDateTime(now.UtcDateTime))
            throw new ArgumentException("Preferred viewing date cannot be in the past.", nameof(preferredViewingDate));

        return new ViewingRequest
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            ListingId = listingId,
            ApplicantName = applicantName.Trim(),
            PhoneNumber = phoneNumber,
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant(),
            PreferredViewingDate = preferredViewingDate,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            StaffNotes = null,
            RequestStatus = ViewingRequestStatus.Pending,
            SubmittedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void UpdateStatus(ViewingRequestStatus newStatus, string? staffNotes, DateTimeOffset now, Guid? updatedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot update status of a deleted request.");

        if (RequestStatus == newStatus)
            throw new InvalidOperationException($"Request is already in status '{RequestStatus}'.");

        // Validate state transitions
        bool validTransition = newStatus switch
        {
            ViewingRequestStatus.Cancelled => true, // Can cancel from any state
            ViewingRequestStatus.Contacted => RequestStatus == ViewingRequestStatus.Pending,
            ViewingRequestStatus.Scheduled => RequestStatus == ViewingRequestStatus.Pending || RequestStatus == ViewingRequestStatus.Contacted,
            ViewingRequestStatus.Completed => RequestStatus == ViewingRequestStatus.Scheduled,
            ViewingRequestStatus.Pending => false, // Cannot go backwards to Pending
            _ => false
        };

        if (!validTransition)
            throw new InvalidOperationException($"Invalid status transition from {RequestStatus} to {newStatus}.");

        // Staff notes are mandatory for certain transitions
        if (newStatus is ViewingRequestStatus.Contacted or ViewingRequestStatus.Scheduled or ViewingRequestStatus.Cancelled)
        {
            if (string.IsNullOrWhiteSpace(staffNotes) && string.IsNullOrWhiteSpace(StaffNotes))
                throw new InvalidOperationException($"Staff notes are mandatory when transitioning to {newStatus}.");
        }

        if (staffNotes != null)
        {
            StaffNotes = string.IsNullOrWhiteSpace(staffNotes) ? null : staffNotes.Trim();
        }

        RequestStatus = newStatus;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void AddStaffNotes(string? staffNotes, DateTimeOffset now, Guid? updatedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot modify a deleted request.");

        StaffNotes = string.IsNullOrWhiteSpace(staffNotes) ? null : staffNotes.Trim();
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void SoftDelete(DateTimeOffset now, Guid? deletedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Viewing request is already deleted.");

        DeletedAt = now;
        DeletedBy = deletedBy;
        UpdatedAt = now;
        UpdatedBy = deletedBy;
    }
}
