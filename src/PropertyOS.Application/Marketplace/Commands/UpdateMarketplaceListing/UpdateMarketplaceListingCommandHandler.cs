using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Common.ValueObjects;

namespace PropertyOS.Application.Marketplace.Commands.UpdateMarketplaceListing;

public class UpdateMarketplaceListingCommandHandler : IRequestHandler<UpdateMarketplaceListingCommand, Unit>
{
    private readonly IMarketplaceListingRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateMarketplaceListingCommandHandler(
        IMarketplaceListingRepository repository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(UpdateMarketplaceListingCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var listing = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (listing == null)
            throw new NotFoundException($"MarketplaceListing with ID {request.Id} was not found.");

        // Tenant boundary check
        if (listing.CompanyId != companyId)
            throw new NotFoundException($"MarketplaceListing with ID {request.Id} was not found.");

        var contactPhone = new PhoneNumber(request.ContactPhone);
        var contactWhatsapp = !string.IsNullOrWhiteSpace(request.ContactWhatsapp)
            ? new PhoneNumber(request.ContactWhatsapp)
            : null;

        try
        {
            listing.UpdateDetails(
                listingTitle: request.Title,
                listingDescription: request.Description,
                monthlyRent: request.MonthlyRent,
                securityDeposit: request.SecurityDeposit,
                currency: request.Currency,
                contactPhone: contactPhone,
                contactWhatsapp: contactWhatsapp,
                expirationDate: request.ExpirationDate,
                isFeatured: request.IsFeatured,
                now: DateTimeOffset.UtcNow,
                updatedBy: _currentUserContext.UserId);
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }

        return Unit.Value;
    }
}
