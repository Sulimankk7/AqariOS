using MediatR;
using Microsoft.Extensions.Options;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.UtilityBills.Options;
using PropertyOS.Application.UtilityBills.Services;
using PropertyOS.Domain.UtilityBills;

namespace PropertyOS.Application.UtilityBills.Commands.ReplaceUtilityAccount;

public sealed class ReplaceUtilityAccountCommandHandler
    : IRequestHandler<ReplaceUtilityAccountCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IUtilityAccountRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IBusinessClock _clock;
    private readonly UtilityBillsOptions _options;
    private readonly IPostCommitRegistrar _postCommitRegistrar;
    private readonly IUtilityBillsJobScheduler _jobScheduler;

    public ReplaceUtilityAccountCommandHandler(
        IApplicationDbContext db,
        IUtilityAccountRepository repository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IBusinessClock clock,
        IOptionsSnapshot<UtilityBillsOptions> options,
        IPostCommitRegistrar postCommitRegistrar,
        IUtilityBillsJobScheduler jobScheduler)
    {
        _db = db;
        _repository = repository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _clock = clock;
        _options = options.Value;
        _postCommitRegistrar = postCommitRegistrar;
        _jobScheduler = jobScheduler;
    }

    public async Task<Guid> Handle(
        ReplaceUtilityAccountCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new UnauthorizedAccessException("Tenant context not established.");
        var actorId = _currentUserContext.UserId
            ?? throw new UnauthorizedAccessException("Current user context not established.");

        var currentAccount = await _repository.GetByIdAsync(
            request.UtilityAccountId, cancellationToken);
        if (currentAccount is null || currentAccount.CompanyId != companyId)
            throw new NotFoundException(
                $"Utility account with ID '{request.UtilityAccountId}' was not found.");

        if (!UtilityAccountNumberRules.IsValid(
                currentAccount.UtilityType, request.AccountNumber))
        {
            throw new BusinessRuleException(
                "The utility account number does not match the provider contract.",
                "UTILITY_ACCOUNT_NUMBER_INVALID");
        }

        var now = _clock.UtcNow;
        var staleThreshold = now.AddMinutes(-_options.StaleClaimThresholdMinutes);
        if (currentAccount.ClaimedAt.HasValue && currentAccount.ClaimedAt > staleThreshold)
        {
            throw new BusinessRuleException(
                "Cannot replace a utility account while it is being synchronised. " +
                "Please wait for the current sync to complete and try again.");
        }

        var normalizedAccountNumber = request.AccountNumber.Trim();
        UtilityAccount resultingAccount;

        if (string.Equals(
                currentAccount.AccountNumber,
                normalizedAccountNumber,
                StringComparison.Ordinal))
        {
            currentAccount.RefreshLinkDetails(
                normalizedAccountNumber, request.MeterNumber, now, actorId);
            resultingAccount = currentAccount;
        }
        else
        {
            currentAccount.SoftDelete(now, actorId);

            // Flush the superseded account inside the existing transaction before
            // activating/adding its replacement. This avoids a transient violation of
            // the filtered one-active-account-per-lease/type unique index.
            await _db.SaveChangesAsync(cancellationToken);

            resultingAccount = await _repository.GetDeletedExactMatchAsync(
                companyId,
                currentAccount.LeaseContractId,
                currentAccount.UtilityType,
                normalizedAccountNumber,
                cancellationToken)
                ?? UtilityAccount.Create(
                    companyId,
                    currentAccount.LeaseContractId,
                    currentAccount.TenantId,
                    currentAccount.ApartmentId,
                    currentAccount.UtilityType,
                    normalizedAccountNumber,
                    request.MeterNumber,
                    now,
                    actorId);

            if (resultingAccount.DeletedAt.HasValue)
            {
                resultingAccount.Reactivate(
                    normalizedAccountNumber, request.MeterNumber, now, actorId);
            }
            else
            {
                await _repository.AddAsync(resultingAccount, cancellationToken);
            }
        }

        var accountId = resultingAccount.Id;
        var requiresBootstrap = !resultingAccount.HistoricalBootstrapCompleted;
        _postCommitRegistrar.RegisterPostCommitAction(async _ =>
        {
            if (requiresBootstrap)
                await _jobScheduler.EnqueueBootstrapAsync(accountId, CancellationToken.None);
            else
                await _jobScheduler.EnqueueSyncAsync(accountId, CancellationToken.None);
        });

        return accountId;
    }
}
