using Hangfire;
using PropertyOS.Application.UtilityBills.Services;

namespace PropertyOS.Infrastructure.UtilityBills.Jobs;

public sealed class HangfireUtilityBillsJobScheduler : IUtilityBillsJobScheduler
{
    private readonly IBackgroundJobClient _backgroundJobs;

    public HangfireUtilityBillsJobScheduler(IBackgroundJobClient backgroundJobs)
    {
        _backgroundJobs = backgroundJobs;
    }

    public Task EnqueueBootstrapAsync(
        Guid utilityAccountId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _backgroundJobs.Enqueue<BootstrapUtilityAccountJob>(
            job => job.ExecuteAsync(utilityAccountId, CancellationToken.None));
        return Task.CompletedTask;
    }

    public Task EnqueueSyncAsync(
        Guid utilityAccountId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _backgroundJobs.Enqueue<SyncUtilityAccountJob>(
            job => job.ExecuteAsync(utilityAccountId, CancellationToken.None));
        return Task.CompletedTask;
    }
}
