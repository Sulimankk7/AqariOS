using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.DeleteTenant;

public class DeleteTenantCommandHandler : IRequestHandler<DeleteTenantCommand, Unit>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public DeleteTenantCommandHandler(
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        // 1. Fetch; cross-tenant access (and already-deleted tenants) is masked as NotFound.
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant == null || tenant.CompanyId != companyId)
            throw new NotFoundException($"Tenant with ID {request.TenantId} was not found.");

        // 2. Guard: a tenant referenced by any non-terminal (draft/pending_signature/active)
        //    lease contract cannot be deleted.
        if (await _tenantRepository.HasNonTerminalLeaseContractAsync(tenant.Id, cancellationToken))
            throw new BusinessRuleException(
                "Cannot delete a tenant who is referenced by a draft, pending, or active lease contract.",
                "TENANT_HAS_ACTIVE_LEASE");

        // 3. Soft delete via the domain.
        tenant.SoftDelete(DateTimeOffset.UtcNow, _currentUserContext.UserId);

        // Note: SaveChanges is owned by TransactionBehavior
        return Unit.Value;
    }
}
