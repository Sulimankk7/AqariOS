using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.DeleteTenantVehicle;

public class DeleteTenantVehicleCommandHandler : IRequestHandler<DeleteTenantVehicleCommand, Unit>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public DeleteTenantVehicleCommandHandler(
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(DeleteTenantVehicleCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var vehicle = await _tenantRepository.GetVehicleByIdAsync(request.TenantId, request.VehicleId, companyId, cancellationToken);
        if (vehicle == null)
        {
            throw new NotFoundException($"Vehicle with ID {request.VehicleId} was not found for tenant {request.TenantId}.");
        }

        vehicle.SoftDelete(DateTimeOffset.UtcNow, _currentUserContext.UserId);

        return Unit.Value;
    }
}
