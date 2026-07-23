using System;
using PropertyOS.Domain.Common;

namespace PropertyOS.Domain.Documents.Entities;

/// <summary>
/// Represents a company-managed document category lookup bucket.
/// Maps to PostgreSQL table: document_categories (Module 10 §10.1).
/// </summary>
public class DocumentCategory : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    private DocumentCategory() { }

    public static DocumentCategory Create(
        Guid companyId,
        string name,
        string? description,
        DateTimeOffset now,
        Guid? createdBy)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID must be specified.", nameof(companyId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be blank.", nameof(name));

        if (name.Trim().Length > 100)
            throw new ArgumentException("Category name cannot exceed 100 characters.", nameof(name));

        if (description != null && description.Trim().Length > 255)
            throw new ArgumentException("Category description cannot exceed 255 characters.", nameof(description));

        return new DocumentCategory
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void UpdateDetails(string newName, string? newDescription, DateTimeOffset now, Guid? updatedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot modify a deleted category.");

        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Category name cannot be blank.", nameof(newName));

        if (newName.Trim().Length > 100)
            throw new ArgumentException("Category name cannot exceed 100 characters.", nameof(newName));

        if (newDescription != null && newDescription.Trim().Length > 255)
            throw new ArgumentException("Category description cannot exceed 255 characters.", nameof(newDescription));

        Name = newName.Trim();
        Description = string.IsNullOrWhiteSpace(newDescription) ? null : newDescription.Trim();
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void SoftDelete(DateTimeOffset now, Guid? deletedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Category is already deleted.");

        DeletedAt = now;
        DeletedBy = deletedBy;
        UpdatedAt = now;
        UpdatedBy = deletedBy;
    }
}
