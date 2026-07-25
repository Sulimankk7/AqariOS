using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;

namespace PropertyOS.Application.Properties.ParkingSpots.Commands.ArchiveParkingSpot;

public class ArchiveParkingSpotCommandHandler : IRequestHandler<ArchiveParkingSpotCommand, Unit>
{
    private readonly IParkingSpotRepository _parkingSpotRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public ArchiveParkingSpotCommandHandler(
        IParkingSpotRepository parkingSpotRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _parkingSpotRepository = parkingSpotRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(ArchiveParkingSpotCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

        var companyId = _tenantContext.CompanyId.Value;

        var spot = await _parkingSpotRepository.GetByIdAsync(request.Id, cancellationToken);
        if (spot == null || spot.CompanyId != companyId)
            throw new NotFoundException($"Parking spot with ID {request.Id} was not found.");

        spot.SoftDelete(DateTimeOffset.UtcNow, _currentUserContext.UserId);

        return Unit.Value;
    }
}
