using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Leasing;

namespace PropertyOS.Application.Leasing.Commands.CreateTenantEmergencyContact;

public class CreateTenantEmergencyContactCommandHandler : IRequestHandler<CreateTenantEmergencyContactCommand, Guid>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CreateTenantEmergencyContactCommandHandler(
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Guid> Handle(CreateTenantEmergencyContactCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant == null || tenant.CompanyId != companyId)
        {
            throw new NotFoundException($"Tenant with ID {request.TenantId} was not found.");
        }

        var contact = TenantEmergencyContact.Create(
            companyId: companyId,
            tenantId: request.TenantId,
            name: request.Name,
            relationshipType: request.RelationshipType,
            phone: request.Phone,
            createdAt: DateTimeOffset.UtcNow,
            createdBy: _currentUserContext.UserId
        );

        await _tenantRepository.AddEmergencyContactAsync(contact, cancellationToken);

        return contact.Id;
    }
}
