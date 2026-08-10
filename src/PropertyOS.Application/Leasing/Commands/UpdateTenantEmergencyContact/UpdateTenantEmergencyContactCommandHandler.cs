using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.UpdateTenantEmergencyContact;

public class UpdateTenantEmergencyContactCommandHandler : IRequestHandler<UpdateTenantEmergencyContactCommand, Unit>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateTenantEmergencyContactCommandHandler(
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(UpdateTenantEmergencyContactCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var contact = await _tenantRepository.GetEmergencyContactByIdAsync(request.TenantId, request.ContactId, companyId, cancellationToken);
        if (contact == null)
        {
            throw new NotFoundException($"Emergency contact with ID {request.ContactId} was not found for tenant {request.TenantId}.");
        }

        contact.UpdateDetails(
            name: request.Name,
            relationshipType: request.RelationshipType,
            phone: request.Phone,
            updatedAt: DateTimeOffset.UtcNow,
            updatedBy: _currentUserContext.UserId
        );

        return Unit.Value;
    }
}
