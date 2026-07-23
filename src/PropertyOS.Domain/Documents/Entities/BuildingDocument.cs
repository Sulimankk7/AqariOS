using System;
using PropertyOS.Domain.Common;

namespace PropertyOS.Domain.Documents.Entities;

/// <summary>
/// Represents a categorized, optionally-confidential document filed against a building.
/// Maps to PostgreSQL table: building_documents (Module 10 §10.2).
/// </summary>
public class BuildingDocument : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid BuildingId { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid FileId { get; private set; }
    public string DocumentName { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateOnly? IssueDate { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public bool IsConfidential { get; private set; }
    public Guid? UploadedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    private BuildingDocument() { }

    public static BuildingDocument Create(
        Guid companyId,
        Guid buildingId,
        Guid categoryId,
        Guid fileId,
        string documentName,
        string? description,
        DateOnly? issueDate,
        DateOnly? expiryDate,
        bool isConfidential,
        Guid? uploadedBy,
        DateTimeOffset now,
        Guid? createdBy,
        DateOnly currentDate)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID must be specified.", nameof(companyId));

        if (buildingId == Guid.Empty)
            throw new ArgumentException("Building ID must be specified.", nameof(buildingId));

        if (categoryId == Guid.Empty)
            throw new ArgumentException("Category ID must be specified.", nameof(categoryId));

        if (fileId == Guid.Empty)
            throw new ArgumentException("File ID must be specified.", nameof(fileId));

        if (string.IsNullOrWhiteSpace(documentName))
            throw new ArgumentException("Document name cannot be blank.", nameof(documentName));

        if (documentName.Trim().Length > 255)
            throw new ArgumentException("Document name cannot exceed 255 characters.", nameof(documentName));

        ValidateDates(issueDate, expiryDate, currentDate);

        return new BuildingDocument
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            BuildingId = buildingId,
            CategoryId = categoryId,
            FileId = fileId,
            DocumentName = documentName.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IssueDate = issueDate,
            ExpiryDate = expiryDate,
            IsConfidential = isConfidential,
            UploadedBy = uploadedBy,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void UpdateDetails(
        string newName,
        string? newDescription,
        DateOnly? newIssueDate,
        DateOnly? newExpiryDate,
        bool newIsConfidential,
        DateTimeOffset now,
        Guid? updatedBy,
        DateOnly currentDate)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot modify a deleted document.");

        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Document name cannot be blank.", nameof(newName));

        if (newName.Trim().Length > 255)
            throw new ArgumentException("Document name cannot exceed 255 characters.", nameof(newName));

        ValidateDates(newIssueDate, newExpiryDate, currentDate);

        DocumentName = newName.Trim();
        Description = string.IsNullOrWhiteSpace(newDescription) ? null : newDescription.Trim();
        IssueDate = newIssueDate;
        ExpiryDate = newExpiryDate;
        IsConfidential = newIsConfidential;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void ToggleConfidentiality(bool isConfidential, DateTimeOffset now, Guid? updatedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot modify a deleted document.");

        if (IsConfidential == isConfidential)
            return;

        IsConfidential = isConfidential;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void SoftDelete(DateTimeOffset now, Guid? deletedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Document is already deleted.");

        DeletedAt = now;
        DeletedBy = deletedBy;
        UpdatedAt = now;
        UpdatedBy = deletedBy;
    }

    private static void ValidateDates(DateOnly? issueDate, DateOnly? expiryDate, DateOnly currentDate)
    {
        if (issueDate.HasValue && issueDate.Value > currentDate)
            throw new ArgumentException($"Issue date ({issueDate.Value}) cannot be in the future.", nameof(issueDate));

        if (expiryDate.HasValue && issueDate.HasValue && expiryDate.Value <= issueDate.Value)
            throw new ArgumentException($"Expiry date ({expiryDate.Value}) must strictly succeed issue date ({issueDate.Value}).", nameof(expiryDate));
    }
}
