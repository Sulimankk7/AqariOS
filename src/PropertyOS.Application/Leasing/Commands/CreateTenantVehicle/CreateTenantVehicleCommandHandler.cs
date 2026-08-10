using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Leasing;

namespace PropertyOS.Application.Leasing.Commands.CreateTenantVehicle;

public class CreateTenantVehicleCommandHandler : IRequestHandler<CreateTenantVehicleCommand, Guid>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CreateTenantVehicleCommandHandler(
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Guid> Handle(CreateTenantVehicleCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant == null || tenant.CompanyId != companyId)
        {
            throw new NotFoundException($"Tenant with ID {request.TenantId} was not found.");
        }

        if (await _tenantRepository.ExistsByPlateNumberAsync(companyId, request.PlateNumber, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"A vehicle with plate number '{request.PlateNumber.Trim()}' already exists in this company.");
        }

        var vehicle = TenantVehicle.Create(
            companyId: companyId,
            tenantId: request.TenantId,
            plateNumber: request.PlateNumber,
            makeModel: request.MakeModel,
            color: request.Color,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: _currentUserContext.UserId
        );

        await _tenantRepository.AddVehicleAsync(vehicle, cancellationToken);

        return vehicle.Id;
    }
}
