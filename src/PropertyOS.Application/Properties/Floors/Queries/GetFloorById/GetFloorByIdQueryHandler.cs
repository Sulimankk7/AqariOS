using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Floors.Queries.Common;

namespace PropertyOS.Application.Properties.Floors.Queries.GetFloorById;

public class GetFloorByIdQueryHandler : IRequestHandler<GetFloorByIdQuery, FloorDto?>
{
    private readonly IFloorRepository _floorRepository;
    private readonly ITenantContext _tenantContext;

    public GetFloorByIdQueryHandler(IFloorRepository floorRepository, ITenantContext tenantContext)
    {
        _floorRepository = floorRepository;
        _tenantContext = tenantContext;
    }

    public async Task<FloorDto?> Handle(GetFloorByIdQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

        var companyId = _tenantContext.CompanyId.Value;

        var floor = await _floorRepository.GetByIdAsync(request.Id, cancellationToken);
        if (floor == null || floor.CompanyId != companyId || floor.DeletedAt != null)
            throw new NotFoundException($"Floor with ID {request.Id} was not found.");

        return new FloorDto
        {
            Id = floor.Id,
            CompanyId = floor.CompanyId,
            BuildingId = floor.BuildingId,
            FloorNumber = floor.FloorNumber,
            FloorLabel = floor.FloorLabel,
            FloorType = floor.FloorType,
            ApartmentsCount = floor.ApartmentsCount,
            CreatedAt = floor.CreatedAt,
            UpdatedAt = floor.UpdatedAt
        };
    }
}
