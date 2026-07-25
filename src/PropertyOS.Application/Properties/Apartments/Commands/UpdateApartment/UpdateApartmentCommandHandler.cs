using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;

namespace PropertyOS.Application.Properties.Apartments.Commands.UpdateApartment;

public class UpdateApartmentCommandHandler : IRequestHandler<UpdateApartmentCommand, Unit>
{
    private readonly IApartmentRepository _apartmentRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateApartmentCommandHandler(
        IApartmentRepository apartmentRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _apartmentRepository = apartmentRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(UpdateApartmentCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

        var companyId = _tenantContext.CompanyId.Value;

        var apartment = await _apartmentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (apartment == null || apartment.CompanyId != companyId || apartment.DeletedAt != null)
            throw new NotFoundException($"Apartment with ID {request.Id} was not found.");

        var now = DateTimeOffset.UtcNow;
        var userId = _currentUserContext.UserId;

        apartment.UpdateBaseRent(
            amount: request.BaseRentAmount,
            currency: request.BaseRentCurrency,
            updatedAt: now,
            updatedBy: userId
        );

        return Unit.Value;
    }
}
