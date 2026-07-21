using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Common.ValueObjects;
using PropertyOS.Domain.Marketplace;
using PropertyOS.Application.Properties;

namespace PropertyOS.Application.Marketplace.Commands.CreateMarketplaceListing;

public class CreateMarketplaceListingCommandHandler : IRequestHandler<CreateMarketplaceListingCommand, Guid>
{
    private readonly IMarketplaceListingRepository _repository;
    private readonly IApartmentRepository _apartmentRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CreateMarketplaceListingCommandHandler(
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

    public async Task<Guid> Handle(CreateMarketplaceListingCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        // 1. Guard against unknown/foreign apartments (tenant isolation belt-and-suspenders)
        var apartment = await _apartmentRepository.GetByIdAsync(request.ApartmentId, companyId, cancellationToken);
        if (apartment == null)
            throw new KeyNotFoundException($"Apartment '{request.ApartmentId}' was not found.");

        // 2. Validate and normalize phone numbers using E.164 Value Object
        var contactPhone = new PhoneNumber(request.ContactPhone);
        var contactWhatsapp = !string.IsNullOrWhiteSpace(request.ContactWhatsapp)
            ? new PhoneNumber(request.ContactWhatsapp)
            : null;

        var now = DateTimeOffset.UtcNow;
        var listing = MarketplaceListing.Create(
            companyId: companyId,
            buildingId: apartment.BuildingId,
            apartmentId: apartment.Id,
            listingTitle: request.Title,
            listingDescription: request.Description,
            monthlyRent: request.MonthlyRent,
            securityDeposit: request.SecurityDeposit,
            currency: request.Currency,
            contactPhone: contactPhone,
            contactWhatsapp: contactWhatsapp,
            expirationDate: request.ExpirationDate,
            isFeatured: request.IsFeatured,
            now: now,
            createdBy: _currentUserContext.UserId);

        await _repository.AddAsync(listing, cancellationToken);

        return listing.Id;
    }
}
