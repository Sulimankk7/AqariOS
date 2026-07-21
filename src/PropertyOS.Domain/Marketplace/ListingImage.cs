using System;
using PropertyOS.Domain.Common;

namespace PropertyOS.Domain.Marketplace;

/// <summary>
/// Represents an ordered photograph of a marketplace listing.
/// Maps to the PostgreSQL table: listing_images (Module 9 §9.2).
/// </summary>
public class ListingImage : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid ListingId { get; private set; }
    public Guid FileId { get; private set; }
    public short DisplayOrder { get; private set; }
    public bool IsCover { get; private set; }
    public Guid? UploadedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    private ListingImage() { }

    public static ListingImage Create(
        Guid companyId,
        Guid listingId,
        Guid fileId,
        short displayOrder,
        bool isCover,
        Guid? uploadedBy,
        DateTimeOffset now,
        Guid? createdBy)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID must be specified.", nameof(companyId));

        if (listingId == Guid.Empty)
            throw new ArgumentException("Listing ID must be specified.", nameof(listingId));

        if (fileId == Guid.Empty)
            throw new ArgumentException("File ID must be specified.", nameof(fileId));

        if (displayOrder < 0 || displayOrder > 2000)
            throw new ArgumentException("Display order is out of bounds. Must be between 0 and 2000 (allowing for +1000 temporary reordering shifts).", nameof(displayOrder));

        return new ListingImage
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            ListingId = listingId,
            FileId = fileId,
            DisplayOrder = displayOrder,
            IsCover = isCover,
            UploadedBy = uploadedBy,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void UpdateDisplayOrder(short newOrder, DateTimeOffset now, Guid? updatedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot modify a deleted image.");

        if (newOrder < 0 || newOrder > 2000)
            throw new ArgumentException("Display order is out of bounds. Must be between 0 and 2000.", nameof(newOrder));

        DisplayOrder = newOrder;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void SetCover(bool isCover, DateTimeOffset now, Guid? updatedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot modify a deleted image.");

        if (IsCover == isCover)
            return;

        IsCover = isCover;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void SoftDelete(DateTimeOffset now, Guid? deletedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Image is already deleted.");

        DeletedAt = now;
        DeletedBy = deletedBy;
        UpdatedAt = now;
        UpdatedBy = deletedBy;
    }
}
