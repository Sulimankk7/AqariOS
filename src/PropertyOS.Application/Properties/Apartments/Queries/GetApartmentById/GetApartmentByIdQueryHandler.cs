using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Apartments.Queries.Common;

namespace PropertyOS.Application.Properties.Apartments.Queries.GetApartmentById;

public class GetApartmentByIdQueryHandler : IRequestHandler<GetApartmentByIdQuery, ApartmentDto?>
{
    private readonly IApartmentRepository _apartmentRepository;
    private readonly ITenantContext _tenantContext;

    public GetApartmentByIdQueryHandler(IApartmentRepository apartmentRepository, ITenantContext tenantContext)
    {
        _apartmentRepository = apartmentRepository;
        _tenantContext = tenantContext;
    }

    public async Task<ApartmentDto?> Handle(GetApartmentByIdQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

        var companyId = _tenantContext.CompanyId.Value;

        var apartment = await _apartmentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (apartment == null || apartment.CompanyId != companyId || apartment.DeletedAt != null)
            throw new NotFoundException($"Apartment with ID {request.Id} was not found.");

        return new ApartmentDto
        {
            Id = apartment.Id,
            CompanyId = apartment.CompanyId,
            BuildingId = apartment.BuildingId,
            FloorId = apartment.FloorId,
            UnitNumber = apartment.UnitNumber,
            OwnershipStatus = apartment.OwnershipStatus,
            ExternalOwnerName = apartment.ExternalOwnerName,
            ExternalOwnerPhone = apartment.ExternalOwnerPhone,
            OccupancyStatus = apartment.OccupancyStatus,
            AreaSqm = apartment.AreaSqm,
            Bedrooms = apartment.Bedrooms,
            Bathrooms = apartment.Bathrooms,
            BaseRentAmount = apartment.BaseRentAmount,
            BaseRentCurrency = apartment.BaseRentCurrency,
            IsActive = apartment.IsActive,
            CreatedAt = apartment.CreatedAt,
            UpdatedAt = apartment.UpdatedAt
        };
    }
}
