using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Marketplace;
using PropertyOS.Domain.Marketplace;
using PropertyOS.Domain.Marketplace.Enums;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Marketplace.Repositories;

public class ViewingRequestRepository : IViewingRequestRepository
{
    private readonly PropertyOsDbContext _context;

    public ViewingRequestRepository(PropertyOsDbContext context)
    {
        _context = context;
    }

    public Task<ViewingRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.ViewingRequests
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task AddAsync(ViewingRequest request, CancellationToken cancellationToken = default)
    {
        await _context.ViewingRequests.AddAsync(request, cancellationToken);
    }

    public Task<bool> PublishedListingExistsAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        // A listing exists and is eligible if it is Published, not deleted, and not expired.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return _context.MarketplaceListings
            .AnyAsync(l => l.Id == listingId 
                           && l.Status == ListingStatus.Published 
                           && l.DeletedAt == null 
                           && (l.ExpirationDate == null || l.ExpirationDate >= today), cancellationToken);
    }

    public async Task<Guid?> GetListingCompanyIdAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Ignore global query filter here because the public unauthenticated submitter
        // does not have a resolved tenant context, which would otherwise filter out the listing.
        // We still explicitly filter out deleted listings manually below.
        var listing = await _context.MarketplaceListings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.Id == listingId 
                                      && l.Status == ListingStatus.Published 
                                      && l.DeletedAt == null 
                                      && (l.ExpirationDate == null || l.ExpirationDate >= today), cancellationToken);

        return listing?.CompanyId;
    }
}
