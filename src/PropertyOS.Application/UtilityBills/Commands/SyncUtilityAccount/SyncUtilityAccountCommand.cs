using MediatR;

namespace PropertyOS.Application.UtilityBills.Commands.SyncUtilityAccount;

/// <summary>
/// Performs one provider sync cycle for a single utility account.
///
/// Called by:
///   - CheckElectricityBillsJob (during billing window days 1–3)
///   - CheckWaterBillsJob (daily, NextCheckAt-gated)
///   - BootstrapUtilityAccountJob (once, via Hangfire enqueue after link)
///
/// The command is responsible for:
///   1. Claiming the account (FOR UPDATE SKIP LOCKED)
///   2. Calling the provider (outside any DB transaction)
///   3. Persisting newly discovered bills (ON CONFLICT DO NOTHING)
///   4. Updating account sync state (next_check_at, statistics)
///   5. Triggering notifications for new unpaid bills
///
/// isBootstrapRun = true: treats all fetched bills as historical (no notifications).
/// isBootstrapRun = false: normal incremental sync; notifications enabled.
/// </summary>
public sealed record SyncUtilityAccountCommand(
    Guid UtilityAccountId,
    bool IsBootstrapRun = false
) : IRequest<Unit>;
