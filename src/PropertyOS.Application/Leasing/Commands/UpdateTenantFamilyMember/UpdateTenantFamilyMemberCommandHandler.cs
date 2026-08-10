using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.UpdateTenantFamilyMember;

public class UpdateTenantFamilyMemberCommandHandler : IRequestHandler<UpdateTenantFamilyMemberCommand, Unit>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateTenantFamilyMemberCommandHandler(
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(UpdateTenantFamilyMemberCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var familyMember = await _tenantRepository.GetFamilyMemberByIdAsync(request.TenantId, request.FamilyMemberId, companyId, cancellationToken);
        if (familyMember == null)
        {
            throw new NotFoundException($"Family member with ID {request.FamilyMemberId} was not found for tenant {request.TenantId}.");
        }

        familyMember.UpdateDetails(
            name: request.Name,
            relationshipType: request.RelationshipType,
            ageBracket: request.AgeBracket,
            updatedAt: DateTimeOffset.UtcNow,
            updatedBy: _currentUserContext.UserId
        );

        return Unit.Value;
    }
}
