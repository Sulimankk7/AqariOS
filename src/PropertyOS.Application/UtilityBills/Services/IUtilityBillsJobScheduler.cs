namespace PropertyOS.Application.UtilityBills.Services;

/// <summary>
/// Durable background boundary for Utility Bills provider work.
/// Application handlers enqueue work but never invoke provider I/O in the web request.
/// </summary>
public interface IUtilityBillsJobScheduler
{
    Task EnqueueBootstrapAsync(Guid utilityAccountId, CancellationToken cancellationToken = default);

    Task EnqueueSyncAsync(Guid utilityAccountId, CancellationToken cancellationToken = default);
}
