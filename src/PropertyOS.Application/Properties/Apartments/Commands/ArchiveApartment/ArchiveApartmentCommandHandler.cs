using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;

namespace PropertyOS.Application.Properties.Apartments.Commands.ArchiveApartment;

public class ArchiveApartmentCommandHandler : IRequestHandler<ArchiveApartmentCommand, Unit>
{
    private readonly IApartmentRepository _apartmentRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public ArchiveApartmentCommandHandler(
        IApartmentRepository apartmentRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _apartmentRepository = apartmentRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(ArchiveApartmentCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

        var companyId = _tenantContext.CompanyId.Value;

        var apartment = await _apartmentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (apartment == null || apartment.CompanyId != companyId)
            throw new NotFoundException($"Apartment with ID {request.Id} was not found.");

        // Active lease archive guard is deferred to Module 5 integration as documented.

        apartment.SoftDelete(DateTimeOffset.UtcNow, _currentUserContext.UserId);

        return Unit.Value;
    }
}
