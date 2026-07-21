using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Marketplace;
using PropertyOS.Application.Marketplace.Queries.Common;
using PropertyOS.Domain.Marketplace.Enums;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Marketplace.Repositories;

public class MarketplaceQueries : IMarketplaceQueries
{
    private readonly PropertyOsDbContext _context;

    public MarketplaceQueries(PropertyOsDbContext context)
    {
        _context = context;
    }

    public async Task<List<PublicListingSummaryDto>> GetPublicListingsAsync(
        PublicListingsFilterOptions filter,
        CancellationToken cancellationToken = default)
    {
        // 1. AsNoTracking()
        var query = _context.MarketplaceListings
            .AsNoTracking()
            .Where(l => l.Status == ListingStatus.Published 
                        && l.DeletedAt == null 
                        && (l.ExpirationDate == null || l.ExpirationDate >= DateOnly.FromDateTime(DateTime.UtcNow)));

        // Apply filters
        if (filter.BuildingId.HasValue)
            query = query.Where(l => l.BuildingId == filter.BuildingId.Value);

        if (filter.ApartmentId.HasValue)
            query = query.Where(l => l.ApartmentId == filter.ApartmentId.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var search = filter.SearchText.Trim();
            // Trigram index search via ILIKE
            query = query.Where(l => EF.Functions.ILike(l.ListingTitle, $"%{search}%"));
        }

        // Keyset Pagination: sorted by is_featured DESC, published_date DESC, id ASC
        if (filter.LastSeenIsFeatured.HasValue && filter.LastSeenPublishedDate.HasValue && filter.LastSeenId.HasValue)
        {
            var lastIsFeatured = filter.LastSeenIsFeatured.Value;
            var lastPublishedDate = filter.LastSeenPublishedDate.Value;
            var lastId = filter.LastSeenId.Value;

            // (l.is_featured < lastIsFeatured) OR
            // (l.is_featured = lastIsFeatured AND l.published_date < lastPublishedDate) OR
            // (l.is_featured = lastIsFeatured AND l.published_date = lastPublishedDate AND l.id > lastId)
            // Note: is_featured is boolean. Descending sorting means true comes before false.
            query = query.Where(l =>
                (lastIsFeatured && !l.IsFeatured) ||
                (l.IsFeatured == lastIsFeatured && l.PublishedDate < lastPublishedDate) ||
                (l.IsFeatured == lastIsFeatured && l.PublishedDate == lastPublishedDate && l.Id.CompareTo(lastId) > 0)
            );
        }

        query = query.OrderByDescending(l => l.IsFeatured)
            .ThenByDescending(l => l.PublishedDate)
            .ThenBy(l => l.Id);

        // Enforce maximum page size limit
        var limit = Math.Min(filter.PageSize > 0 ? filter.PageSize : 10, 50);

        // Project directly into DTO to avoid N+1 queries or loading tracking graphs
        return await query.Take(limit)
            .Select(l => new PublicListingSummaryDto(
                l.Id,
                l.ListingTitle,
                l.ListingDescription,
                l.MonthlyRent,
                l.SecurityDeposit,
                l.Currency.ToString(),
                l.PublishedDate,
                l.IsFeatured,
                l.ContactPhone.Value,
                l.ContactWhatsapp != null ? l.ContactWhatsapp.Value : null,
                _context.ListingImages
                    .Where(img => img.ListingId == l.Id && img.IsCover && img.DeletedAt == null)
                    .Select(img => img.FileId)
                    .FirstOrDefault()
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CompanyListingSummaryDto>> GetCompanyListingsAsync(
        CompanyListingsFilterOptions filter,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.MarketplaceListings
            .AsNoTracking()
            .Where(l => l.CompanyId == companyId && l.DeletedAt == null);

        if (filter.Status.HasValue)
            query = query.Where(l => l.Status == filter.Status.Value);

        if (filter.BuildingId.HasValue)
            query = query.Where(l => l.BuildingId == filter.BuildingId.Value);

        if (filter.ApartmentId.HasValue)
            query = query.Where(l => l.ApartmentId == filter.ApartmentId.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var search = filter.SearchText.Trim();
            query = query.Where(l => EF.Functions.ILike(l.ListingTitle, $"%{search}%"));
        }

        // Keyset Pagination: sorted by published_date DESC, id ASC
        if (filter.LastSeenPublishedDate.HasValue && filter.LastSeenId.HasValue)
        {
            var lastDate = filter.LastSeenPublishedDate.Value;
            var lastId = filter.LastSeenId.Value;

            query = query.Where(l =>
                l.PublishedDate < lastDate ||
                (l.PublishedDate == lastDate && l.Id.CompareTo(lastId) > 0));
        }

        query = query.OrderByDescending(l => l.PublishedDate).ThenBy(l => l.Id);

        var limit = Math.Min(filter.PageSize > 0 ? filter.PageSize : 10, 50);

        return await query.Take(limit)
            .Select(l => new CompanyListingSummaryDto(
                l.Id,
                l.ListingTitle,
                _context.Apartments.Where(a => a.Id == l.ApartmentId).Select(a => a.UnitNumber).FirstOrDefault() ?? string.Empty,
                _context.Buildings.Where(b => b.Id == l.BuildingId).Select(b => b.Name).FirstOrDefault() ?? string.Empty,
                l.MonthlyRent,
                l.Currency.ToString(),
                l.Status,
                l.PublishedDate,
                l.ExpirationDate,
                l.IsFeatured,
                _context.ListingImages.Count(img => img.ListingId == l.Id && img.DeletedAt == null),
                _context.ViewingRequests.Count(v => v.ListingId == l.Id && v.DeletedAt == null)
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<ListingDetailDto?> GetListingDetailsAsync(
        Guid id,
        Guid? companyId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.MarketplaceListings
            .AsNoTracking()
            .Where(l => l.Id == id && l.DeletedAt == null);

        // Public vs Staff visibility filter
        if (companyId == null)
        {
            query = query.Where(l => l.Status == ListingStatus.Published 
                                     && (l.ExpirationDate == null || l.ExpirationDate >= DateOnly.FromDateTime(DateTime.UtcNow)));
        }
        else
        {
            query = query.Where(l => l.CompanyId == companyId.Value);
        }

        return await query
            .Select(l => new ListingDetailDto(
                l.Id,
                l.CompanyId,
                l.BuildingId,
                _context.Buildings.Where(b => b.Id == l.BuildingId).Select(b => b.Name).FirstOrDefault() ?? string.Empty,
                l.ApartmentId,
                _context.Apartments.Where(a => a.Id == l.ApartmentId).Select(a => a.UnitNumber).FirstOrDefault() ?? string.Empty,
                l.ListingTitle,
                l.ListingDescription,
                l.MonthlyRent,
                l.SecurityDeposit,
                l.Currency.ToString(),
                l.Status,
                l.PublishedDate,
                l.ExpirationDate,
                l.IsFeatured,
                l.ContactPhone.Value,
                l.ContactWhatsapp != null ? l.ContactWhatsapp.Value : null,
                _context.ListingImages
                    .Where(img => img.ListingId == l.Id && img.DeletedAt == null)
                    .OrderBy(img => img.DisplayOrder)
                    .Select(img => new ListingImageDto(img.Id, img.FileId, img.DisplayOrder, img.IsCover))
                    .ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<ViewingRequestSummaryDto>> GetViewingRequestsAsync(
        ViewingRequestsFilterOptions filter,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ViewingRequests
            .AsNoTracking()
            .Where(v => v.CompanyId == companyId && v.DeletedAt == null);

        if (filter.ListingId.HasValue)
            query = query.Where(v => v.ListingId == filter.ListingId.Value);

        if (filter.Status.HasValue)
            query = query.Where(v => v.RequestStatus == filter.Status.Value);

        // Keyset Pagination: sorted by submitted_at DESC, id ASC
        if (filter.LastSeenSubmittedAt.HasValue && filter.LastSeenId.HasValue)
        {
            var lastSubmitted = filter.LastSeenSubmittedAt.Value;
            var lastId = filter.LastSeenId.Value;

            query = query.Where(v =>
                v.SubmittedAt < lastSubmitted ||
                (v.SubmittedAt == lastSubmitted && v.Id.CompareTo(lastId) > 0));
        }

        query = query.OrderByDescending(v => v.SubmittedAt).ThenBy(v => v.Id);

        var limit = Math.Min(filter.PageSize > 0 ? filter.PageSize : 10, 50);

        return await query.Take(limit)
            .Select(v => new ViewingRequestSummaryDto(
                v.Id,
                v.ListingId,
                _context.MarketplaceListings.Where(l => l.Id == v.ListingId).Select(l => l.ListingTitle).FirstOrDefault() ?? string.Empty,
                v.ApplicantName,
                v.PhoneNumber.Value,
                v.Email,
                v.PreferredViewingDate,
                v.Notes,
                v.RequestStatus,
                v.SubmittedAt
            ))
            .ToListAsync(cancellationToken);
    }
}
