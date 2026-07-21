using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PropertyOS.Domain.Audit.Attributes;
using PropertyOS.Domain.Audit.Enums;
using PropertyOS.Domain.Common;
using PropertyOS.Domain.Common.Enums;
using PropertyOS.Domain.Common.ValueObjects;
using PropertyOS.Domain.Marketplace.Enums;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Domain.Marketplace;

/// <summary>
/// Aggregate root representing a marketplace listing for a property unit.
/// Maps to the PostgreSQL table: marketplace_listings (Module 9 §9.1).
/// </summary>
[AuditSeverity(AuditSeverity.Critical)]
public class MarketplaceListing : ISoftDeletable
{
    private const short REORDER_TEMP_OFFSET = 1000;
    private readonly List<ListingImage> _images = new();

    // -------------------------------------------------------------------------
    // Scalar properties
    // -------------------------------------------------------------------------
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid BuildingId { get; private set; }
    public Guid ApartmentId { get; private set; }

    public string ListingTitle { get; private set; } = string.Empty;
    public string ListingDescription { get; private set; } = string.Empty;

    public decimal MonthlyRent { get; private set; }
    public decimal? SecurityDeposit { get; private set; }
    public CurrencyCode Currency { get; private set; }
    public ListingStatus Status { get; private set; }

    public DateOnly? PublishedDate { get; private set; }
    public DateOnly? ExpirationDate { get; private set; }
    public bool IsFeatured { get; private set; }

    public PhoneNumber ContactPhone { get; private set; } = null!;
    public PhoneNumber? ContactWhatsapp { get; private set; }

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

    // -------------------------------------------------------------------------
    // Navigation
    // -------------------------------------------------------------------------
    public IReadOnlyCollection<ListingImage> Images => _images.AsReadOnly();

    private MarketplaceListing() { }

    // -------------------------------------------------------------------------
    // Factory
    // -------------------------------------------------------------------------
    public static MarketplaceListing Create(
        Guid companyId,
        Guid buildingId,
        Guid apartmentId,
        string listingTitle,
        string listingDescription,
        decimal monthlyRent,
        decimal? securityDeposit,
        CurrencyCode currency,
        PhoneNumber contactPhone,
        PhoneNumber? contactWhatsapp,
        DateOnly? expirationDate,
        bool isFeatured,
        DateTimeOffset now,
        Guid? createdBy)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID must be specified.", nameof(companyId));

        if (buildingId == Guid.Empty)
            throw new ArgumentException("Building ID must be specified.", nameof(buildingId));

        if (apartmentId == Guid.Empty)
            throw new ArgumentException("Apartment ID must be specified.", nameof(apartmentId));

        ValidateDetails(listingTitle, listingDescription, monthlyRent, securityDeposit);

        return new MarketplaceListing
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            BuildingId = buildingId,
            ApartmentId = apartmentId,
            ListingTitle = listingTitle.Trim(),
            ListingDescription = listingDescription.Trim(),
            MonthlyRent = monthlyRent,
            SecurityDeposit = securityDeposit,
            Currency = currency,
            Status = ListingStatus.Draft,
            PublishedDate = null,
            ExpirationDate = expirationDate,
            IsFeatured = isFeatured,
            ContactPhone = contactPhone,
            ContactWhatsapp = contactWhatsapp,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    private static void ValidateDetails(string title, string description, decimal rent, decimal? deposit)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Listing title must not be blank.", nameof(title));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Listing description must not be blank.", nameof(description));

        if (rent <= 0)
            throw new ArgumentException("Monthly rent must be positive.", nameof(rent));

        if (deposit.HasValue && deposit.Value < 0)
            throw new ArgumentException("Security deposit cannot be negative.", nameof(deposit));
    }

    // -------------------------------------------------------------------------
    // Business Operations
    // -------------------------------------------------------------------------
    public void UpdateDetails(
        string listingTitle,
        string listingDescription,
        decimal monthlyRent,
        decimal? securityDeposit,
        CurrencyCode currency,
        PhoneNumber contactPhone,
        PhoneNumber? contactWhatsapp,
        DateOnly? expirationDate,
        bool isFeatured,
        DateTimeOffset now,
        Guid? updatedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot update details of a deleted listing.");

        if (Status != ListingStatus.Draft)
            throw new InvalidOperationException("Details can only be modified in Draft status.");

        ValidateDetails(listingTitle, listingDescription, monthlyRent, securityDeposit);

        ListingTitle = listingTitle.Trim();
        ListingDescription = listingDescription.Trim();
        MonthlyRent = monthlyRent;
        SecurityDeposit = securityDeposit;
        Currency = currency;
        ContactPhone = contactPhone;
        ContactWhatsapp = contactWhatsapp;
        ExpirationDate = expirationDate;
        IsFeatured = isFeatured;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void Publish(Apartment apartment, DateTimeOffset now, Guid? updatedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot publish a deleted listing.");

        if (Status != ListingStatus.Draft)
            throw new InvalidOperationException($"Cannot publish a listing in {Status} status. Only Draft listings can be published.");

        // Invariant Validation
        if (apartment == null)
            throw new ArgumentNullException(nameof(apartment));

        if (apartment.Id != ApartmentId)
            throw new InvalidOperationException("Apartment details do not match listing's associated apartment.");

        if (apartment.CompanyId != CompanyId)
            throw new InvalidOperationException("Apartment belongs to a different company.");

        if (!apartment.IsActive)
            throw new InvalidOperationException("Cannot publish a listing for an inactive apartment.");

        if (apartment.OccupancyStatus != OccupancyStatus.Vacant)
            throw new InvalidOperationException("Only vacant apartments can be published.");

        var activeImages = _images.Where(i => i.DeletedAt == null).ToList();
        if (activeImages.Count == 0)
            throw new InvalidOperationException("Listing must contain at least one image before publishing.");

        if (activeImages.Count(i => i.IsCover) != 1)
            throw new InvalidOperationException("Listing must have exactly one cover image designated before publishing.");

        if (ContactPhone == null || string.IsNullOrWhiteSpace(ContactPhone.Value))
            throw new InvalidOperationException("Required contact information is missing.");

        if (MonthlyRent <= 0)
            throw new InvalidOperationException("Required pricing is invalid or missing.");

        var today = DateOnly.FromDateTime(now.UtcDateTime);
        if (ExpirationDate.HasValue && ExpirationDate.Value <= today)
            throw new InvalidOperationException("Expiration date must lie strictly after the publication date.");

        Status = ListingStatus.Published;
        // PublishedDate is set once and remains immutable
        if (PublishedDate == null)
        {
            PublishedDate = today;
        }

        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public bool TryMarkAsRented(DateTimeOffset now, Guid? updatedBy)
    {
        if (DeletedAt != null || Status != ListingStatus.Published)
            return false;

        Status = ListingStatus.Rented;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
        return true;
    }

    public void Expire(DateTimeOffset now, Guid? updatedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot expire a deleted listing.");

        if (Status != ListingStatus.Published)
            throw new InvalidOperationException("Only published listings can be expired.");

        Status = ListingStatus.Expired;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void Archive(DateTimeOffset now, Guid? updatedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot archive a deleted listing.");

        if (Status is not (ListingStatus.Published or ListingStatus.Rented or ListingStatus.Expired))
            throw new InvalidOperationException($"Cannot archive listing from status: {Status}.");

        Status = ListingStatus.Archived;
        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    // -------------------------------------------------------------------------
    // Image management
    // -------------------------------------------------------------------------
    public ListingImage AddImage(Guid fileId, bool isCover, Guid? uploadedBy, DateTimeOffset now, Guid? createdBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot add image to a deleted listing.");

        if (Status != ListingStatus.Draft && Status != ListingStatus.Published)
            throw new InvalidOperationException("Images can only be added to Draft or Published listings.");

        if (_images.Any(i => i.FileId == fileId && i.DeletedAt == null))
            throw new InvalidOperationException("This file is already attached as an image to this listing.");

        if (_images.Count(i => i.DeletedAt == null) >= 10)
            throw new InvalidOperationException("A listing cannot have more than 10 active images.");

        // Calculate contiguous display order
        int nextOrder = _images.Where(i => i.DeletedAt == null).Select(i => (int)i.DisplayOrder).DefaultIfEmpty(-1).Max() + 1;

        var image = ListingImage.Create(
            companyId: CompanyId,
            listingId: Id,
            fileId: fileId,
            displayOrder: (short)nextOrder,
            isCover: isCover,
            uploadedBy: uploadedBy,
            now: now,
            createdBy: createdBy);

        if (isCover)
        {
            // Reset existing cover flag
            foreach (var img in _images.Where(i => i.DeletedAt == null && i.IsCover))
            {
                img.SetCover(false, now, createdBy);
            }
        }

        _images.Add(image);
        UpdatedAt = now;
        UpdatedBy = createdBy;

        return image;
    }

    public void RemoveImage(Guid imageId, DateTimeOffset now, Guid? deletedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot remove image from a deleted listing.");

        var image = _images.FirstOrDefault(i => i.Id == imageId && i.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Image '{imageId}' was not found on this listing.");

        image.SoftDelete(now, deletedBy);

        // Re-align subsequent images to keep display order contiguous
        var activeImages = _images.Where(i => i.DeletedAt == null).OrderBy(i => i.DisplayOrder).ToList();
        for (int i = 0; i < activeImages.Count; i++)
        {
            activeImages[i].UpdateDisplayOrder((short)i, now, deletedBy);
        }

        UpdatedAt = now;
        UpdatedBy = deletedBy;
    }

    public void SetCoverImage(Guid imageId, DateTimeOffset now, Guid? updatedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot set cover image of a deleted listing.");

        var image = _images.FirstOrDefault(i => i.Id == imageId && i.DeletedAt == null)
            ?? throw new KeyNotFoundException($"Image '{imageId}' was not found on this listing.");

        foreach (var img in _images.Where(i => i.DeletedAt == null))
        {
            img.SetCover(img.Id == imageId, now, updatedBy);
        }

        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    public void ReorderImages(List<Guid> imageIdsInNewOrder, DateTimeOffset now, Guid? updatedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot reorder images of a deleted listing.");

        var activeImages = _images.Where(i => i.DeletedAt == null).ToList();
        if (imageIdsInNewOrder.Count != activeImages.Count || imageIdsInNewOrder.Any(id => activeImages.All(i => i.Id != id)))
        {
            throw new ArgumentException("The list of image IDs does not match the active images on this listing.");
        }

        // To prevent DB-level partial unique index conflicts, we perform a 2-step shift update.
        // The unique index allows at most one active image per display_order per listing.
        // If we simply update (0->1, 1->0), the first update will violate the index if order 1 exists.
        // Therefore, we first shift all indices out of the valid range by adding REORDER_TEMP_OFFSET.
        // 1. Shift temporarily by +REORDER_TEMP_OFFSET
        foreach (var img in activeImages)
        {
            img.UpdateDisplayOrder((short)(img.DisplayOrder + REORDER_TEMP_OFFSET), now, updatedBy);
        }

        // 2. Set final contiguous indices (0, 1, 2...)
        for (int i = 0; i < imageIdsInNewOrder.Count; i++)
        {
            var imgId = imageIdsInNewOrder[i];
            var img = activeImages.First(x => x.Id == imgId);
            img.UpdateDisplayOrder((short)i, now, updatedBy);
        }

        UpdatedAt = now;
        UpdatedBy = updatedBy;
    }

    // -------------------------------------------------------------------------
    // Soft Delete
    // -------------------------------------------------------------------------
    public void SoftDelete(DateTimeOffset now, Guid? deletedBy)
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("Listing is already deleted.");

        // Only Draft listings can be soft-deleted directly
        if (Status != ListingStatus.Draft)
            throw new InvalidOperationException("Only Draft listings can be soft-deleted. Real listings must be Archived.");

        DeletedAt = now;
        DeletedBy = deletedBy;
        UpdatedAt = now;
        UpdatedBy = deletedBy;

        // Cascade soft delete to active child images
        foreach (var img in _images.Where(i => i.DeletedAt == null))
        {
            img.SoftDelete(now, deletedBy);
        }
    }
}
