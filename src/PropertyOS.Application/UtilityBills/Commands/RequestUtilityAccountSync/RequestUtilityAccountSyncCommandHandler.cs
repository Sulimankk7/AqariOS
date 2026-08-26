using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.UtilityBills.Services;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Application.UtilityBills.Commands.RequestUtilityAccountSync;

public sealed record RequestUtilityAccountSyncCommand(Guid UtilityAccountId) : ICommand;

public sealed class RequestUtilityAccountSyncCommandHandler
    : IRequestHandler<RequestUtilityAccountSyncCommand, Unit>
{
    private readonly IUtilityAccountRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IBusinessClock _clock;
    private readonly IPostCommitRegistrar _postCommitRegistrar;
    private readonly IUtilityBillsJobScheduler _jobScheduler;

    public RequestUtilityAccountSyncCommandHandler(
        IUtilityAccountRepository repository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IBusinessClock clock,
        IPostCommitRegistrar postCommitRegistrar,
        IUtilityBillsJobScheduler jobScheduler)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _clock = clock;
        _postCommitRegistrar = postCommitRegistrar;
        _jobScheduler = jobScheduler;
    }

    public async Task<Unit> Handle(
        RequestUtilityAccountSyncCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new UnauthorizedAccessException("Tenant context not established.");

        var account = await _repository.GetByIdAsync(
            request.UtilityAccountId, cancellationToken);

        if (account is null || account.CompanyId != companyId
            || (!account.IsActive && account.SyncStatus != UtilitySyncStatus.Suspended))
            throw new NotFoundException(
                $"Utility account with ID '{request.UtilityAccountId}' was not found.");

        if (account.SyncStatus == UtilitySyncStatus.Suspended)
        {
            var actorId = _currentUserContext.UserId
                ?? throw new UnauthorizedAccessException("Current user context not established.");
            account.PrepareManualRetry(_clock.UtcNow, actorId);
        }

        var requiresBootstrap = !account.HistoricalBootstrapCompleted;
        _postCommitRegistrar.RegisterPostCommitAction(async _ =>
        {
            if (requiresBootstrap)
                await _jobScheduler.EnqueueBootstrapAsync(
                    request.UtilityAccountId, CancellationToken.None);
            else
                await _jobScheduler.EnqueueSyncAsync(
                    request.UtilityAccountId, CancellationToken.None);
        });
        return Unit.Value;
    }
}
