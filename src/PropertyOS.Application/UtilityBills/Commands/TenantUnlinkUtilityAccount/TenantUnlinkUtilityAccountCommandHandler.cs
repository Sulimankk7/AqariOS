using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.UtilityBills.Commands.UnlinkUtilityAccount;
using PropertyOS.Application.UtilityBills.Security;

namespace PropertyOS.Application.UtilityBills.Commands.TenantUnlinkUtilityAccount;

public sealed class TenantUnlinkUtilityAccountCommandHandler
    : IRequestHandler<TenantUnlinkUtilityAccountCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ISender _sender;

    public TenantUnlinkUtilityAccountCommandHandler(
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

    public async Task<Unit> Handle(
        TenantUnlinkUtilityAccountCommand request,
        CancellationToken cancellationToken)
    {
        var scope = await TenantUtilityAccountAccess.ResolveAsync(
            _db, _tenantContext, _currentUserContext, cancellationToken);
        await TenantUtilityAccountAccess.EnsureOwnCurrentAccountAsync(
            _db, scope, request.UtilityAccountId, cancellationToken);

        await _sender.Send(
            new UnlinkUtilityAccountCommand(request.UtilityAccountId), cancellationToken);
        return Unit.Value;
    }
}
