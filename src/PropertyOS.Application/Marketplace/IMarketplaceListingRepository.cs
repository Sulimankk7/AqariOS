using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Domain.Marketplace;
using PropertyOS.Domain.Properties;

namespace PropertyOS.Application.Marketplace;

/// <summary>
/// Write-side repository boundary for the MarketplaceListing aggregate root.
/// </summary>
public interface IMarketplaceListingRepository
{
    Task<MarketplaceListing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Loads the active published listing for a given apartment.
    /// Used for lease activation reconciliation and duplicate publication checks.
    /// </summary>
    Task<MarketplaceListing?> GetActiveListingByApartmentIdAsync(Guid apartmentId, CancellationToken cancellationToken = default);
    
    Task AddAsync(MarketplaceListing listing, CancellationToken cancellationToken = default);
    
}
