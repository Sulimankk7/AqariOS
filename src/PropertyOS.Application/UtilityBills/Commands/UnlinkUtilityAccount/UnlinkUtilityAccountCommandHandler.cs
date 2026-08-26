using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.UtilityBills.Commands.UnlinkUtilityAccount;

/// <summary>
/// Soft-deletes a utility account.
///
/// Guard rails:
///   - companyId from JWT only.
///   - Cannot unlink while a sync is actively in progress (claimed_at is fresh).
///   - Preserves all bill history for audit; only marks the account as inactive.
/// </summary>
public sealed class UnlinkUtilityAccountCommandHandler : IRequestHandler<UnlinkUtilityAccountCommand, Unit>
{
    private readonly IUtilityAccountRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IBusinessClock _clock;
    private readonly Microsoft.Extensions.Options.IOptionsSnapshot<Options.UtilityBillsOptions> _opts;

    public UnlinkUtilityAccountCommandHandler(
        IUtilityAccountRepository repository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IBusinessClock clock,
        Microsoft.Extensions.Options.IOptionsSnapshot<Options.UtilityBillsOptions> opts)
    {
        _repository         = repository;
        _tenantContext      = tenantContext;
        _currentUserContext = currentUserContext;
        _clock              = clock;
        _opts               = opts;
    }

    public async Task<Unit> Handle(UnlinkUtilityAccountCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new UnauthorizedAccessException("Tenant context not established.");

        var account = await _repository.GetByIdAsync(request.UtilityAccountId, cancellationToken);

        if (account is null || account.CompanyId != companyId)
            throw new NotFoundException($"Utility account with ID '{request.UtilityAccountId}' was not found.");


        var now = _clock.UtcNow;
        var staleThreshold = now.AddMinutes(-_opts.Value.StaleClaimThresholdMinutes);

        // Prevent unlinking while a sync is actively in progress
        if (account.ClaimedAt.HasValue && account.ClaimedAt.Value > staleThreshold)
            throw new BusinessRuleException(
                "Cannot unlink a utility account while it is being synchronised. " +
                "Please wait for the current sync to complete and try again.");

        var deletedBy = _currentUserContext.UserId
            ?? throw new UnauthorizedAccessException("Current user context not established.");

        account.SoftDelete(now, deletedBy);
        // TransactionBehavior handles SaveChanges + commit.
        return Unit.Value;
    }
}
