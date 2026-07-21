using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Domain.Marketplace;

namespace PropertyOS.Application.Marketplace;

/// <summary>
/// Write-side repository boundary for the ViewingRequest aggregate root.
/// </summary>
public interface IViewingRequestRepository
{
    Task<ViewingRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task AddAsync(ViewingRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a listing exists and is eligible for viewing requests (published, active).
    /// </summary>
    Task<bool> PublishedListingExistsAsync(Guid listingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Safely resolves the owning CompanyId for a listing to ensure we enforce
    /// tenant assignment server-side for public viewing requests.
    /// </summary>
    Task<Guid?> GetListingCompanyIdAsync(Guid listingId, CancellationToken cancellationToken = default);
}
