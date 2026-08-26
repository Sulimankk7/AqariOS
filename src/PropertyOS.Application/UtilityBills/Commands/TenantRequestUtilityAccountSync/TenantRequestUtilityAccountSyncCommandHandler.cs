using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.UtilityBills.Commands.RequestUtilityAccountSync;
using PropertyOS.Application.UtilityBills.Security;

namespace PropertyOS.Application.UtilityBills.Commands.TenantRequestUtilityAccountSync;

public sealed class TenantRequestUtilityAccountSyncCommandHandler
    : IRequestHandler<TenantRequestUtilityAccountSyncCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ISender _sender;

    public TenantRequestUtilityAccountSyncCommandHandler(
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
        TenantRequestUtilityAccountSyncCommand request,
        CancellationToken cancellationToken)
    {
        var scope = await TenantUtilityAccountAccess.ResolveAsync(
            _db, _tenantContext, _currentUserContext, cancellationToken);
        await TenantUtilityAccountAccess.EnsureOwnCurrentAccountAsync(
            _db, scope, request.UtilityAccountId, cancellationToken);

        await _sender.Send(
            new RequestUtilityAccountSyncCommand(request.UtilityAccountId), cancellationToken);
        return Unit.Value;
    }
}
