using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.DeleteTenantFamilyMember;

public class DeleteTenantFamilyMemberCommandHandler : IRequestHandler<DeleteTenantFamilyMemberCommand, Unit>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public DeleteTenantFamilyMemberCommandHandler(
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(DeleteTenantFamilyMemberCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var familyMember = await _tenantRepository.GetFamilyMemberByIdAsync(request.TenantId, request.FamilyMemberId, companyId, cancellationToken);
        if (familyMember == null)
        {
            throw new NotFoundException($"Family member with ID {request.FamilyMemberId} was not found for tenant {request.TenantId}.");
        }

        familyMember.SoftDelete(DateTimeOffset.UtcNow, _currentUserContext.UserId);

        return Unit.Value;
    }
}
