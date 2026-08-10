using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.UpdateTenantVehicle;

public class UpdateTenantVehicleCommandHandler : IRequestHandler<UpdateTenantVehicleCommand, Unit>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateTenantVehicleCommandHandler(
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(UpdateTenantVehicleCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var vehicle = await _tenantRepository.GetVehicleByIdAsync(request.TenantId, request.VehicleId, companyId, cancellationToken);
        if (vehicle == null)
        {
            throw new NotFoundException($"Vehicle with ID {request.VehicleId} was not found for tenant {request.TenantId}.");
        }

        if (await _tenantRepository.ExistsByPlateNumberAsync(companyId, request.PlateNumber, request.VehicleId, cancellationToken))
        {
            throw new ConflictException($"A vehicle with plate number '{request.PlateNumber.Trim()}' already exists in this company.");
        }

        vehicle.UpdateDetails(
            plateNumber: request.PlateNumber,
            makeModel: request.MakeModel,
            color: request.Color,
            updatedAt: DateTimeOffset.UtcNow,
            updatedBy: _currentUserContext.UserId
        );

        return Unit.Value;
    }
}
