using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.DeleteTenantEmergencyContact;

public class DeleteTenantEmergencyContactCommandHandler : IRequestHandler<DeleteTenantEmergencyContactCommand, Unit>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public DeleteTenantEmergencyContactCommandHandler(
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(DeleteTenantEmergencyContactCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var contact = await _tenantRepository.GetEmergencyContactByIdAsync(request.TenantId, request.ContactId, companyId, cancellationToken);
        if (contact == null)
        {
            throw new NotFoundException($"Emergency contact with ID {request.ContactId} was not found for tenant {request.TenantId}.");
        }

        contact.SoftDelete(DateTimeOffset.UtcNow, _currentUserContext.UserId);

        return Unit.Value;
    }
}
