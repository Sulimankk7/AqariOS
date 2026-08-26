using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.UtilityBills.Commands.LinkUtilityAccount;
using PropertyOS.Application.UtilityBills.Security;

namespace PropertyOS.Application.UtilityBills.Commands.TenantLinkUtilityAccount;

/// <summary>
/// Resolves the tenant self-service authorization boundary, then delegates to the
/// canonical account-link command so domain invariants and post-commit bootstrap
/// behavior remain identical to management linking.
/// </summary>
public sealed class TenantLinkUtilityAccountCommandHandler
    : IRequestHandler<TenantLinkUtilityAccountCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ISender _sender;

    public TenantLinkUtilityAccountCommandHandler(
        IApplicationDbContext db,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        ISender sender)
    {
        _db = db;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _sender = sender;
    }

    public async Task<Guid> Handle(
        TenantLinkUtilityAccountCommand request,
        CancellationToken cancellationToken)
    {
        var scope = await TenantUtilityAccountAccess.ResolveAsync(
            _db, _tenantContext, _currentUserContext, cancellationToken);

        // TransactionBehavior sees the existing tenant-command transaction and does
        // not create a nested transaction. The canonical link handler owns creation,
        // duplicate pre-checks, domain construction, and post-commit bootstrap setup.
        return await _sender.Send(
            new LinkUtilityAccountCommand(
                scope.LeaseContractId,
                request.UtilityType,
                request.AccountNumber,
                request.MeterNumber),
            cancellationToken);
    }
}
