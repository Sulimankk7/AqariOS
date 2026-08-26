using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Notifications.Commands.CreateNotification;
using PropertyOS.Application.UtilityBills.Options;
using PropertyOS.Application.UtilityBills.Services;
using PropertyOS.Domain.Notifications.Enums;
using PropertyOS.Domain.UtilityBills;
using PropertyOS.Domain.UtilityBills.Enums;
using PropertyOS.Domain.UtilityBills.Services;

namespace PropertyOS.Application.UtilityBills.Commands.SyncUtilityAccount;

/// <summary>
/// Handles one provider sync cycle for a single utility account.
///
/// CRITICAL DESIGN RULES:
///   1. External HTTP calls (IUtilityBillingProvider.FetchBillsAsync) MUST NOT happen
///      inside a DB transaction. The flow is:
///        a) Short claim transaction (commit and release)
///        b) Provider HTTP call (no transaction open)
///        c) New transaction via SaveChanges (via TransactionBehavior)
///
///   2. This handler IS a command (ICommand) so TransactionBehavior wraps it.
///      We must ensure the provider call happens before TransactionBehavior's
///      BeginTransactionAsync. This is achieved by calling TryClaimForSyncAsync
///      inside a raw ExecuteInTransactionAsync (which TransactionBehavior does not
///      control), then calling the provider, then persisting.
///
///      However, since TransactionBehavior owns the outer transaction, and we need
///      the provider call outside a transaction, this handler is structured as a
///      NON-ICommand (using IRequest directly) so TransactionBehavior does not fire.
///      All DB work uses explicit transactions via IApplicationDbContext.
///
///   3. RecordSyncFailure MUST NEVER modify LastKnownBillDate.
///
///   4. On consecutive_failure_count >= max, the account is suspended.
///
///   5. Null provider (PROVIDER_NOT_CONFIGURED): not treated as a failure.
///      Does NOT increment consecutive_failure_count. Schedules next check.
/// </summary>
public sealed class SyncUtilityAccountCommandHandler : IRequestHandler<SyncUtilityAccountCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IUtilityAccountRepository _accountRepo;
    private readonly IUtilityBillRepository _billRepo;
    private readonly IReadOnlyDictionary<UtilityType, IUtilityBillingProvider> _providers;
    private readonly IOptionsSnapshot<UtilityBillsOptions> _opts;
    private readonly IBusinessClock _clock;
    private readonly ISender _sender;
    private readonly ILogger<SyncUtilityAccountCommandHandler> _logger;

    public SyncUtilityAccountCommandHandler(
        IApplicationDbContext db,
        IUtilityAccountRepository accountRepo,
        IUtilityBillRepository billRepo,
        IEnumerable<IUtilityBillingProvider> providers,
        IOptionsSnapshot<UtilityBillsOptions> opts,
        IBusinessClock clock,
        ISender sender,
        ILogger<SyncUtilityAccountCommandHandler> logger)
    {
        _db          = db;
        _accountRepo = accountRepo;
        _billRepo    = billRepo;
        _providers   = (providers ?? throw new ArgumentNullException(nameof(providers)))
            .ToDictionary(p => p.ProviderType);
        _opts        = opts;
        _clock       = clock;
        _sender      = sender;
        _logger      = logger;
    }

    public async Task<Unit> Handle(SyncUtilityAccountCommand request, CancellationToken cancellationToken)
    {
        var opts    = _opts.Value;
        var now     = _clock.UtcNow;
        var jobRunId = Guid.CreateVersion7().ToString("N");

        var staleThreshold = now.AddMinutes(-opts.StaleClaimThresholdMinutes);

        // ── Step 1: Claim the account (FOR UPDATE SKIP LOCKED) ────────────────
        // Short transaction: claim only. Committed before any provider HTTP call.
        UtilityAccount? account = null;
        await _db.ExecuteInTransactionAsync(async ct =>
        {
            account = await _accountRepo.TryClaimForSyncAsync(
                request.UtilityAccountId, staleThreshold, ct);

            if (account is not null)
            {
                account.BeginClaim(jobRunId, now);
                // SaveChanges is handled by ExecuteInTransactionAsync (explicit)
            }
        }, cancellationToken);

        if (account is null)
        {
            _logger.LogInformation(
                "Utility account {Id} skipped — already claimed by another worker.", request.UtilityAccountId);
            return Unit.Value;
        }

        // ── Step 2: Provider HTTP call (NO transaction open) ─────────────────
        if (!_providers.TryGetValue(account.UtilityType, out var provider))
        {
            _logger.LogError("No provider registered for utility type {UtilityType}.", account.UtilityType);
            await _db.ExecuteInTransactionAsync(async ct =>
            {
                var claimedAccount = await _accountRepo.GetByIdAsync(request.UtilityAccountId, ct)
                    ?? throw new InvalidOperationException(
                        $"Utility account {request.UtilityAccountId} not found while releasing its claim.");
                claimedAccount.RecordProviderNotConfigured(
                    "No utility billing provider is registered.",
                    now,
                    ComputeProviderNotConfiguredNextCheck(claimedAccount, now, opts));
                claimedAccount.ReleaseClaim(now);
            }, cancellationToken);
            return Unit.Value;
        }


        var providerTimeout = account.UtilityType == UtilityType.Electricity
            ? TimeSpan.FromSeconds(opts.Electricity.ProviderTimeoutSeconds)
            : TimeSpan.FromSeconds(opts.Water.ProviderTimeoutSeconds);

        ProviderBillFetchResult result;
        using var timeoutCts    = new CancellationTokenSource(providerTimeout);
        using var linkedCts     = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        try
        {
            // sinceDate = null → full history (bootstrap run)
            // sinceDate = LastKnownBillDate → incremental (normal run)
            var sinceDate = request.IsBootstrapRun ? null : account.LastKnownBillDate;

            result = await provider.FetchBillsAsync(
                account.AccountNumber,
                account.MeterNumber,
                sinceDate,
                linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            result = ProviderBillFetchResult.Failure("TIMEOUT",
                $"Provider call exceeded timeout ({providerTimeout.TotalSeconds}s).");
        }
        catch (Exception ex)
        {
            result = ProviderBillFetchResult.Failure("UNEXPECTED_ERROR", ex.Message);
        }

        // ── Step 3: Persist results (new transaction via IApplicationDbContext) ─
        await _db.ExecuteInTransactionAsync(async ct =>
        {
            // Re-load for the persistence transaction (account may have been modified)
            account = await _accountRepo.GetByIdAsync(request.UtilityAccountId, ct)
                ?? throw new InvalidOperationException(
                    $"Utility account {request.UtilityAccountId} not found during persistence step.");

            // PROVIDER_NOT_CONFIGURED: treat as a no-op — do not increment failure count
            if (result.ErrorCode == "PROVIDER_NOT_CONFIGURED")
            {
                account.RecordProviderNotConfigured(
                    result.ErrorMessage,
                    now,
                    ComputeProviderNotConfiguredNextCheck(account, now, opts));
                account.ReleaseClaim(now);
                _logger.LogInformation(
                    "Utility account {Id} ({Type}): provider not configured. Skipping.", 
                    account.Id, account.UtilityType);
                return;
            }

            if (!result.IsSuccess)
            {
                await HandleFailureAsync(account, result, now, opts, ct);
                return;
            }


            // ── Insert newly discovered bills ─────────────────────────────────
            var sortedBills = result.Bills
                .OrderBy(b => b.BillDate)
                .ToList();

            DateOnly? latestBillDate = account.LastKnownBillDate;
            var newlyInsertedBillDates = new List<DateOnly>();

            foreach (var providerBill in sortedBills)
            {
                var domainBill = UtilityBill.Create(
                    companyId:               account.CompanyId,
                    utilityAccountId:        account.Id,
                    utilityType:             account.UtilityType,
                    providerExternalId:      providerBill.ExternalId,
                    billDate:                providerBill.BillDate,
                    dueDate:                 providerBill.DueDate,
                    amount:                  providerBill.Amount,
                    currency:                providerBill.Currency,
                    isPaid:                  providerBill.IsPaid,
                    paymentStatus:           providerBill.PaymentStatus,
                    providerReference:       providerBill.ProviderReference,
                    isFromHistoricalBackfill: request.IsBootstrapRun,
                    discoveredAt:            now);

                bool inserted = await _billRepo.InsertIfNewAsync(domainBill, ct);

                if (inserted)
                    newlyInsertedBillDates.Add(providerBill.BillDate);

                if (inserted && providerBill.BillDate > (latestBillDate ?? DateOnly.MinValue))
                    latestBillDate = providerBill.BillDate;
            }

            // ── Compute next check at and update interval statistics ───────────
            var ammanTz     = TimeZoneInfo.FindSystemTimeZoneById("Asia/Amman");
            var nextCheckAt = ComputeNextCheckAt(account, result.Bills.Count > 0, now, opts, ammanTz);

            if (request.IsBootstrapRun)
            {
                short? avgDays = null;
                short sampleCount = 0;
                DateOnly? estimatedNextBillDate = null;

                // Interval learning is a water-only concern. Electricity must retain
                // the configured monthly billing-window schedule computed above.
                if (account.UtilityType == UtilityType.Water)
                {
                    var allBillDates = sortedBills.Select(b => b.BillDate).ToList();
                    (avgDays, sampleCount) = WaterBillingIntervalService
                        .ComputeFromHistory(allBillDates);

                    if (latestBillDate.HasValue && avgDays.HasValue)
                    {
                        estimatedNextBillDate = WaterBillingIntervalService
                            .ComputeEstimatedNextBillDate(latestBillDate.Value, avgDays.Value);
                        nextCheckAt = WaterBillingIntervalService
                            .ComputeNextCheckAt(estimatedNextBillDate.Value,
                                (short)opts.Water.SafetyLeadDays, ammanTz);
                    }
                }

                account.MarkBootstrapCompleted(
                    lastKnownBillDate:           latestBillDate,
                    averageBillingIntervalDays:  avgDays,
                    sampleCount:                 sampleCount,
                    estimatedNextBillDate:       estimatedNextBillDate,
                    syncedAt:                    now,
                    nextCheckAt:                 nextCheckAt,
                    totalOutstandingBalance:     result.TotalOutstandingBalance);
            }
            else
            {
                // Incremental sync: update interval statistics for newly discovered water bills
                if (account.UtilityType == UtilityType.Water
                    && account.LastKnownBillDate.HasValue)
                {
                    var previousBillDate = account.LastKnownBillDate.Value;
                    var averageDays = account.AverageBillingIntervalDays ?? 0;
                    var sampleCount = account.BillingIntervalSampleCount;

                    foreach (var newBillDate in newlyInsertedBillDates
                                 .Distinct()
                                 .OrderBy(date => date))
                    {
                        var gapDays = newBillDate.DayNumber - previousBillDate.DayNumber;
                        if (gapDays <= 0)
                            continue;

                        (averageDays, sampleCount) = WaterBillingIntervalService.IncrementalUpdate(
                            oldAvgDays: averageDays,
                            oldCount: sampleCount,
                            newGapDays: gapDays);
                        previousBillDate = newBillDate;
                    }

                    if (previousBillDate != account.LastKnownBillDate.Value)
                    {
                        var estimatedNextBillDate = WaterBillingIntervalService
                            .ComputeEstimatedNextBillDate(previousBillDate, averageDays);
                        nextCheckAt = WaterBillingIntervalService
                            .ComputeNextCheckAt(estimatedNextBillDate,
                                (short)opts.Water.SafetyLeadDays, ammanTz);

                        account.UpdateWaterIntervalStatistics(
                            newAverageBillingIntervalDays: averageDays,
                            newSampleCount:                sampleCount,
                            estimatedNextBillDate:         estimatedNextBillDate,
                            nextCheckAt:                   nextCheckAt,
                            updatedAt:                     now);
                    }
                }

                account.RecordSyncSuccess(
                    latestBillDate,
                    now,
                    nextCheckAt,
                    result.TotalOutstandingBalance);
            }

            account.ReleaseClaim(now);

            // ── Send notifications for newly discovered unpaid bills ───────────
            // Done within the same transaction so notification_sent_at is set atomically.
            var unnotifiedBills = request.IsBootstrapRun
                ? new List<UtilityBill>()
                : await _billRepo.GetUnnotifiedUnpaidBillsAsync(account.Id, ct)
                    ?? new List<UtilityBill>();

            foreach (var billToNotify in unnotifiedBills)
            {
                if (billToNotify.NotificationSentAt.HasValue)
                    continue; // Already notified (crash-safe retry guard)

                var notificationType = account.UtilityType == UtilityType.Electricity
                    ? NotificationType.UtilityBillElectricity
                    : NotificationType.UtilityBillWater;

                var subject = account.UtilityType == UtilityType.Electricity
                    ? "لديك فاتورة كهرباء جديدة"
                    : "لديك فاتورة مياه جديدة";

                var body    = $"فاتورة بقيمة {billToNotify.Amount} {billToNotify.Currency}" +
                              $" بتاريخ {billToNotify.BillDate:dd/MM/yyyy}." +
                              (billToNotify.DueDate.HasValue
                                  ? $" يُرجى التسديد قبل {billToNotify.DueDate:dd/MM/yyyy}."
                                  : string.Empty);

                // Resolve the tenant's user ID for the notification recipient
                var tenantUserId = await _db.Tenants
                    .AsNoTracking()
                    .Where(t => t.Id == account.TenantId)
                    .Select(t => t.UserId)
                    .FirstOrDefaultAsync(ct);

                if (tenantUserId is null)
                {
                    _logger.LogWarning(
                        "Utility account {AccountId}: tenant {TenantId} has no associated user. Skipping notification.",
                        account.Id, account.TenantId);
                    continue;
                }

                await _sender.Send(new CreateNotificationCommand(
                    RecipientUserId:  tenantUserId.Value,
                    TemplateId:       null,
                    Subject:          subject,
                    Body:             body,
                    NotificationType: notificationType,
                    Priority:         NotificationPriority.Normal,
                    Channels:         new List<DeliveryChannel> { DeliveryChannel.InApp }
                ), ct);

                billToNotify.MarkNotificationSent(now);
                // The EF change tracker will detect the update to notification_sent_at
            }

        }, cancellationToken);

        return Unit.Value;
    }


    // ── Failure handling ─────────────────────────────────────────────────────

    private async Task HandleFailureAsync(
        UtilityAccount account,
        ProviderBillFetchResult result,
        DateTimeOffset now,
        UtilityBillsOptions opts,
        CancellationToken ct)
    {
        var maxFailures    = account.UtilityType == UtilityType.Electricity
            ? opts.Electricity.MaxConsecutiveFailuresBeforeSkip
            : opts.Water.MaxConsecutiveFailuresBeforeSkip;

        var backoffBase    = account.UtilityType == UtilityType.Electricity
            ? opts.Electricity.BackoffBaseMinutes
            : opts.Water.BackoffBaseMinutes;

        var maxBackoffHours = account.UtilityType == UtilityType.Electricity
            ? opts.Electricity.MaxBackoffHours
            : opts.Water.MaxBackoffHours;

        var backoffMinutes = Math.Min(
            backoffBase * (int)Math.Pow(2, account.ConsecutiveFailureCount),
            maxBackoffHours * 60);

        var nextCheckAt = now.AddMinutes(backoffMinutes);

        var status = result.ErrorCode switch
        {
            "TIMEOUT"       => UtilitySyncStatus.Timeout,
            "RATE_LIMITED"  => UtilitySyncStatus.RateLimited,
            "INVALID_ACCOUNT" => UtilitySyncStatus.InvalidAccount,
            _               => UtilitySyncStatus.ProviderError
        };

        // INVARIANT: RecordSyncFailure never modifies LastKnownBillDate
        account.RecordSyncFailure(status, result.ErrorMessage, now, nextCheckAt);
        account.ReleaseClaim(now);

        if (account.ConsecutiveFailureCount >= maxFailures)
        {
            account.Suspend(now);
            _logger.LogWarning(
                "Utility account {Id} ({Type}) suspended after {FailureCount} consecutive failures.",
                account.Id, account.UtilityType, account.ConsecutiveFailureCount);
        }
        else
        {
            _logger.LogWarning(
                "Utility account {Id} ({Type}) sync failure #{FailureCount}: {ErrorCode}. Next check at {NextCheckAt}.",
                account.Id, account.UtilityType, account.ConsecutiveFailureCount,
                result.ErrorCode, nextCheckAt);
        }

        await Task.CompletedTask; // EF change tracking handles the update
    }

    private static DateTimeOffset ComputeNextCheckAt(
        UtilityAccount account,
        bool newBillFound,
        DateTimeOffset now,
        UtilityBillsOptions opts,
        TimeZoneInfo tz)
    {
        if (account.UtilityType == UtilityType.Electricity)
        {
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now.UtcDateTime, tz));
            return ElectricityBillingWindowService.ComputeNextCheckAt(
                referenceDate:           today,
                referenceTime:           now,
                newBillFound:            newBillFound,
                retryHoursWithinWindow:  opts.Electricity.RetryHoursWithinWindow,
                billingWindowStartHour:  opts.Electricity.BillingWindowStartHour,
                localTimeZone:           tz);
        }

        // Water: fallback used when no interval statistics are available yet
        return now.AddDays(opts.Water.DefaultCheckIntervalDays);
    }

    private static DateTimeOffset ComputeProviderNotConfiguredNextCheck(
        UtilityAccount account,
        DateTimeOffset now,
        UtilityBillsOptions opts)
    {
        var delayMinutes = account.UtilityType == UtilityType.Electricity
            ? opts.Electricity.BackoffBaseMinutes
            : opts.Water.BackoffBaseMinutes;
        return now.AddMinutes(Math.Max(1, delayMinutes));
    }
}
