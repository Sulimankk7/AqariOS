using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Marketplace;
using PropertyOS.Domain.Marketplace;
using PropertyOS.Domain.Marketplace.Enums;
using PropertyOS.Domain.Properties;
using PropertyOS.Infrastructure.Persistence;

namespace PropertyOS.Infrastructure.Marketplace.Repositories;

public class MarketplaceListingRepository : IMarketplaceListingRepository
{
    private readonly PropertyOsDbContext _context;

    public MarketplaceListingRepository(PropertyOsDbContext context)
    {
        _context = context;
    }

    public Task<MarketplaceListing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Load the aggregate root along with its child collection so it can enforce invariants
        return _context.MarketplaceListings
            .Include(l => l.Images)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public Task<MarketplaceListing?> GetActiveListingByApartmentIdAsync(Guid apartmentId, CancellationToken cancellationToken = default)
    {
        return _context.MarketplaceListings
            .FirstOrDefaultAsync(l => l.ApartmentId == apartmentId 
                                      && l.Status == ListingStatus.Published 
                                      && l.DeletedAt == null, cancellationToken);
    }

    public async Task AddAsync(MarketplaceListing listing, CancellationToken cancellationToken = default)
    {
        await _context.MarketplaceListings.AddAsync(listing, cancellationToken);
    }

}
