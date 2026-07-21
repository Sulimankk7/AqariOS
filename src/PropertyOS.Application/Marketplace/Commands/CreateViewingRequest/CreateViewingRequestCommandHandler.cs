using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Common.ValueObjects;
using PropertyOS.Domain.Marketplace;

namespace PropertyOS.Application.Marketplace.Commands.CreateViewingRequest;

public class CreateViewingRequestCommandHandler : IRequestHandler<CreateViewingRequestCommand, Guid>
{
    private readonly IViewingRequestRepository _repository;
    private readonly ICurrentUserContext _currentUserContext;

    public CreateViewingRequestCommandHandler(
        IViewingRequestRepository repository,
        ICurrentUserContext currentUserContext)
    {
        _repository = repository;
        _currentUserContext = currentUserContext;
    }

    public async Task<Guid> Handle(CreateViewingRequestCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify listing existence and resolve CompanyId server-side (do not trust client)
        var companyId = await _repository.GetListingCompanyIdAsync(request.ListingId, cancellationToken);
        if (companyId == null)
            throw new KeyNotFoundException($"MarketplaceListing with ID {request.ListingId} was not found.");

        // 2. Validate and normalize phone number to E.164
        var phoneNumber = new PhoneNumber(request.PhoneNumber);

        var now = DateTimeOffset.UtcNow;
        var viewingRequest = ViewingRequest.Create(
            companyId: companyId.Value,
            listingId: request.ListingId,
            applicantName: request.ApplicantName,
            phoneNumber: phoneNumber,
            email: request.Email,
            preferredViewingDate: request.PreferredViewingDate,
            notes: request.Notes,
            now: now,
            createdBy: _currentUserContext.UserId);

        await _repository.AddAsync(viewingRequest, cancellationToken);

        return viewingRequest.Id;
    }
}
