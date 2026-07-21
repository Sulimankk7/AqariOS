using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Marketplace.Commands.AddListingImage;

public class AddListingImageCommandHandler : IRequestHandler<AddListingImageCommand, Guid>
{
    private readonly IMarketplaceListingRepository _repository;
    private readonly IFileStorageValidator _fileValidator;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public AddListingImageCommandHandler(
        IMarketplaceListingRepository repository,
        IFileStorageValidator fileValidator,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _repository = repository;
        _fileValidator = fileValidator;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Guid> Handle(AddListingImageCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var listing = await _repository.GetByIdAsync(request.ListingId, cancellationToken);
        if (listing == null)
            throw new KeyNotFoundException($"MarketplaceListing with ID {request.ListingId} was not found.");

        if (listing.CompanyId != companyId)
            throw new UnauthorizedAccessException("Listing does not belong to the current company context.");

        // File validation (Module 10 isolation: validated via interface)
        var fileValid = await _fileValidator.ValidateImageFileAsync(request.FileId, companyId, cancellationToken);
        if (!fileValid)
            throw new ArgumentException("Specified file is invalid, is not an image, does not belong to the company, or has been deleted.");

        var now = DateTimeOffset.UtcNow;
        var image = listing.AddImage(
            fileId: request.FileId,
            isCover: request.IsCover,
            uploadedBy: _currentUserContext.UserId,
            now: now,
            createdBy: _currentUserContext.UserId);

        return image.Id;
    }
}
