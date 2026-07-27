using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;

namespace PropertyOS.Application.Marketplace.Commands.PublishMarketplaceListing;

public class PublishMarketplaceListingCommandHandler : IRequestHandler<PublishMarketplaceListingCommand, Unit>
{
    private readonly IMarketplaceListingRepository _repository;
    private readonly IApartmentRepository _apartmentRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public PublishMarketplaceListingCommandHandler(
        IMarketplaceListingRepository repository,
        IApartmentRepository apartmentRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _repository = repository;
        _apartmentRepository = apartmentRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(PublishMarketplaceListingCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var listing = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (listing == null)
            throw new NotFoundException($"MarketplaceListing with ID {request.Id} was not found.");

        if (listing.CompanyId != companyId)
            throw new NotFoundException($"MarketplaceListing with ID {request.Id} was not found.");

        // 1. Uniqueness Guard: One apartment can have only one active published listing at a time
        var activeListing = await _repository.GetActiveListingByApartmentIdAsync(listing.ApartmentId, cancellationToken);
        if (activeListing != null && activeListing.Id != listing.Id)
        {
            throw new ConflictException(
                $"Apartment '{listing.ApartmentId}' already has an active published listing (ID: {activeListing.Id}).");
        }

        // 2. Load apartment to validate active and vacant invariants
        var apartment = await _apartmentRepository.GetByIdAsync(listing.ApartmentId, companyId, cancellationToken);
        if (apartment == null)
            throw new NotFoundException($"Apartment '{listing.ApartmentId}' was not found.");

        // 3. Delegate publish transition to the aggregate root (checks occupancy, images, pricing, phone)
        try
        {
            listing.Publish(apartment, DateTimeOffset.UtcNow, _currentUserContext.UserId);
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }

        return Unit.Value;
    }
}
