using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Marketplace.Commands.DeleteMarketplaceListing;

public class DeleteMarketplaceListingCommandHandler : IRequestHandler<DeleteMarketplaceListingCommand, Unit>
{
    private readonly IMarketplaceListingRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public DeleteMarketplaceListingCommandHandler(
        IMarketplaceListingRepository repository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(DeleteMarketplaceListingCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var listing = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (listing == null)
            throw new KeyNotFoundException($"MarketplaceListing with ID {request.Id} was not found.");

        if (listing.CompanyId != companyId)
            throw new UnauthorizedAccessException("Listing does not belong to the current company context.");

        listing.SoftDelete(DateTimeOffset.UtcNow, _currentUserContext.UserId);

        return Unit.Value;
    }
}
