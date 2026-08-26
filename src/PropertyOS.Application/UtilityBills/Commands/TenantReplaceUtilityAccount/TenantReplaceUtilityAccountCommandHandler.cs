using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.UtilityBills.Commands.ReplaceUtilityAccount;
using PropertyOS.Application.UtilityBills.Security;

namespace PropertyOS.Application.UtilityBills.Commands.TenantReplaceUtilityAccount;

public sealed class TenantReplaceUtilityAccountCommandHandler
    : IRequestHandler<TenantReplaceUtilityAccountCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ISender _sender;

    public TenantReplaceUtilityAccountCommandHandler(
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
        TenantReplaceUtilityAccountCommand request,
        CancellationToken cancellationToken)
    {
        var scope = await TenantUtilityAccountAccess.ResolveAsync(
            _db, _tenantContext, _currentUserContext, cancellationToken);
        await TenantUtilityAccountAccess.EnsureOwnCurrentAccountAsync(
            _db, scope, request.UtilityAccountId, cancellationToken);

        return await _sender.Send(new ReplaceUtilityAccountCommand(
            request.UtilityAccountId,
            request.AccountNumber,
            request.MeterNumber), cancellationToken);
    }
}
