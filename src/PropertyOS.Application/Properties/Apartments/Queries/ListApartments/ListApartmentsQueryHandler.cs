using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Apartments.Queries.Common;
using PropertyOS.Domain.Properties;

namespace PropertyOS.Application.Properties.Apartments.Queries.ListApartments;

public class ListApartmentsQueryHandler : IRequestHandler<ListApartmentsQuery, List<ApartmentDto>>
{
    private readonly IApartmentRepository _apartmentRepository;
    private readonly IFloorRepository _floorRepository;
    private readonly IBuildingRepository _buildingRepository;
    private readonly ITenantContext _tenantContext;

    public ListApartmentsQueryHandler(
        IApartmentRepository apartmentRepository,
        IFloorRepository floorRepository,
        IBuildingRepository buildingRepository,
        ITenantContext tenantContext)
    {
        _apartmentRepository = apartmentRepository;
        _floorRepository = floorRepository;
        _buildingRepository = buildingRepository;
        _tenantContext = tenantContext;
    }

    public async Task<List<ApartmentDto>> Handle(ListApartmentsQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

        var companyId = _tenantContext.CompanyId.Value;

        List<Apartment> apartments;

        if (request.FloorId.HasValue)
        {
            var floor = await _floorRepository.GetByIdAsync(request.FloorId.Value, cancellationToken);
            if (floor == null || floor.CompanyId != companyId || floor.DeletedAt != null)
                throw new NotFoundException($"Floor with ID {request.FloorId.Value} was not found.");

            apartments = await _apartmentRepository.ListByFloorIdAsync(request.FloorId.Value, cancellationToken);
        }
        else if (request.BuildingId.HasValue)
        {
            var building = await _buildingRepository.GetByIdAsync(request.BuildingId.Value, cancellationToken);
            if (building == null || building.CompanyId != companyId || !building.IsActive)
                throw new NotFoundException($"Building with ID {request.BuildingId.Value} was not found.");

            apartments = await _apartmentRepository.ListByBuildingIdAsync(request.BuildingId.Value, cancellationToken);
        }
        else
        {
            apartments = await _apartmentRepository.ListByCompanyIdAsync(companyId, cancellationToken);
        }

        return apartments.Select(apartment => new ApartmentDto
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
        }).ToList();
    }
}
