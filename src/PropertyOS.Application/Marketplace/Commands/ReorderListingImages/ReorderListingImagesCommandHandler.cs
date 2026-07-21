using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Marketplace.Commands.ReorderListingImages;

public class ReorderListingImagesCommandHandler : IRequestHandler<ReorderListingImagesCommand, Unit>
{
    private readonly IMarketplaceListingRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public ReorderListingImagesCommandHandler(
        IMarketplaceListingRepository repository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(ReorderListingImagesCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var listing = await _repository.GetByIdAsync(request.ListingId, cancellationToken);
        if (listing == null)
            throw new KeyNotFoundException($"MarketplaceListing with ID {request.ListingId} was not found.");

        if (listing.CompanyId != companyId)
            throw new UnauthorizedAccessException("Listing does not belong to the current company context.");

        // Calls reorder on aggregate root, which runs step 1 (temp shift) and step 2 (final align) within the transaction
        listing.ReorderImages(request.ImageIds, DateTimeOffset.UtcNow, _currentUserContext.UserId);

        return Unit.Value;
    }
}
