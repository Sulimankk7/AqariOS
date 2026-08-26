using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.UtilityBills.Services;
using PropertyOS.Domain.UtilityBills;
using PropertyOS.Domain.UtilityBills.Services;

namespace PropertyOS.Application.UtilityBills.Commands.LinkUtilityAccount;

/// <summary>
/// Links a utility account to an existing lease contract.
///
/// Guard rails:
///   1. companyId comes from ITenantContext (JWT); never from request body.
///   2. LeaseContract existence is verified within the company scope — cross-tenant
///      access returns NotFoundException (same as "not found").
///   3. Duplicate (lease, type) combination throws ConflictException.
///   4. Bootstrap Hangfire job is enqueued via IPostCommitRegistrar — it fires
///      ONLY if the transaction commits. A rollback will not enqueue the job.
///
/// Transaction: owned by TransactionBehavior (ICommand marker).
/// External provider calls: deferred entirely to BootstrapUtilityAccountJob.
/// </summary>
public sealed class LinkUtilityAccountCommandHandler : IRequestHandler<LinkUtilityAccountCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly IUtilityAccountRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IBusinessClock _clock;
    private readonly IPostCommitRegistrar _postCommitRegistrar;
    private readonly IUtilityBillsJobScheduler _jobScheduler;

    public LinkUtilityAccountCommandHandler(
        IApplicationDbContext db,
        IUtilityAccountRepository repository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IBusinessClock clock,
        IPostCommitRegistrar postCommitRegistrar,
        IUtilityBillsJobScheduler jobScheduler)
    {
        _db                  = db;
        _repository          = repository;
        _tenantContext       = tenantContext;
        _currentUserContext  = currentUserContext;
        _clock               = clock;
        _postCommitRegistrar = postCommitRegistrar;
        _jobScheduler        = jobScheduler;
    }

    public async Task<Guid> Handle(
        LinkUtilityAccountCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new UnauthorizedAccessException("Tenant context not established.");

        var now = _clock.UtcNow;

        // ── 1. Verify lease exists and belongs to this company ────────────────
        var lease = await _db.LeaseContracts
            .AsNoTracking()
            .Where(l => l.Id == request.LeaseContractId && l.CompanyId == companyId)
            .Select(l => new { l.Id, l.TenantId, l.ApartmentId, l.CompanyId })
            .FirstOrDefaultAsync(cancellationToken);

        if (lease is null)
            throw new NotFoundException($"Lease contract with ID '{request.LeaseContractId}' was not found.");

        // ── 2. Prevent duplicate (lease, utility type) ────────────────────────
        var alreadyLinked = await _repository.ExistsByLeaseAndTypeAsync(
            request.LeaseContractId, request.UtilityType, cancellationToken);

        if (alreadyLinked)
            throw new UtilityAccountAlreadyLinkedException(
                $"A {request.UtilityType} utility account is already linked to this lease contract.");

        // ── 3. Create domain entity ───────────────────────────────────────────
        var actorId = _currentUserContext.UserId
            ?? throw new UnauthorizedAccessException("Current user context not established.");

        var account = await _repository.GetDeletedExactMatchAsync(
            companyId,
            request.LeaseContractId,
            request.UtilityType,
            request.AccountNumber,
            cancellationToken);

        if (account is not null)
        {
            account.Reactivate(request.AccountNumber, request.MeterNumber, now, actorId);
        }
        else
        {
            account = UtilityAccount.Create(
                companyId:        companyId,
                leaseContractId:  request.LeaseContractId,
                tenantId:         lease.TenantId,
                apartmentId:      lease.ApartmentId,
                utilityType:      request.UtilityType,
                accountNumber:    request.AccountNumber,
                meterNumber:      request.MeterNumber,
                createdAt:        now,
                createdBy:        actorId);

            await _repository.AddAsync(account, cancellationToken);
        }
        // TransactionBehavior calls SaveChanges + CommitAsync after this handler returns.

        // ── 4. Enqueue bootstrap sync AFTER commit (post-commit action) ───────
        // Using IPostCommitRegistrar prevents the action from firing if the
        // transaction rolls back. The bootstrap run performs the one-time historical
        // import and sets historical_bootstrap_completed = true.
        var accountId = account.Id;
        var requiresBootstrap = !account.HistoricalBootstrapCompleted;
        _postCommitRegistrar.RegisterPostCommitAction(async (ct) =>
        {
            if (requiresBootstrap)
                await _jobScheduler.EnqueueBootstrapAsync(accountId, CancellationToken.None);
            else
                await _jobScheduler.EnqueueSyncAsync(accountId, CancellationToken.None);
        });

        return account.Id;
    }
}
