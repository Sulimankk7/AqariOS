using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Marketplace.Commands.SetListingCoverImage;

public class SetListingCoverImageCommandHandler : IRequestHandler<SetListingCoverImageCommand, Unit>
{
    private readonly IMarketplaceListingRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public SetListingCoverImageCommandHandler(
        IMarketplaceListingRepository repository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(SetListingCoverImageCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var listing = await _repository.GetByIdAsync(request.ListingId, cancellationToken);
        if (listing == null)
            throw new NotFoundException($"MarketplaceListing with ID {request.ListingId} was not found.");

        if (listing.CompanyId != companyId)
            throw new NotFoundException($"MarketplaceListing with ID {request.ListingId} was not found.");

        try
        {
            listing.SetCoverImage(request.ImageId, DateTimeOffset.UtcNow, _currentUserContext.UserId);
        }
        catch (KeyNotFoundException ex)
        {
            throw new NotFoundException(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }

        return Unit.Value;
    }
}
