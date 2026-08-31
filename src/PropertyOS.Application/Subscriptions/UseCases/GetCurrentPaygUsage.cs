using MediatR;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Subscriptions.DTOs;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Domain.Subscriptions;
using PropertyOS.Domain.Subscriptions.Enums;

namespace PropertyOS.Application.Subscriptions.UseCases;

public sealed record GetCurrentPaygUsageQuery(DateTimeOffset? AsOf = null) : ITransactionalRequest<PaygUsageSummaryDto>;

public sealed class GetCurrentPaygUsageQueryHandler : IRequestHandler<GetCurrentPaygUsageQuery, PaygUsageSummaryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IBusinessClock _clock;

    public GetCurrentPaygUsageQueryHandler(IApplicationDbContext db, ITenantContext tenant, IBusinessClock clock)
    {
        _db = db;
        _tenant = tenant;
        _clock = clock;
    }

    public async Task<PaygUsageSummaryDto> Handle(GetCurrentPaygUsageQuery request, CancellationToken cancellationToken)
    {
        var companyId = GetCurrentSubscriptionQueryHandler.RequireCompany(_tenant);
        var now = request.AsOf ?? _clock.UtcNow;
        var today = _clock.GetJordanBusinessDate(now);

        var subscription = await _db.CompanySubscriptions
            .Include(x => x.Plan)
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("No subscription was found for the authenticated company.");

        var closingPeriods = await _db.PaygUsagePeriods
            .Where(x => x.CompanySubscriptionId == subscription.Id && !x.IsFinalized && x.PeriodEnd <= today)
            .OrderBy(x => x.PeriodStart)
            .ToListAsync(cancellationToken);
        foreach (var closingPeriod in closingPeriods)
        {
            await RecalculateAsync(subscription, closingPeriod, closingPeriod.PeriodEnd, closingPeriod.PeriodEnd.AddDays(-1), now, cancellationToken);
            closingPeriod.Finalize(now);
        }

        if (subscription.Plan.PricingModel == SubscriptionPricingModel.Fixed)
            return Fixed(subscription);

        var unitPrice = subscription.BillingCycle == BillingCycleEnum.Monthly
            ? subscription.Plan.PaygMonthlyUnitPrice
            : subscription.Plan.PaygYearlyMonthlyEquivalentUnitPrice;
        if (unitPrice is null or <= 0)
            throw new BusinessRuleException("The PAYG plan does not have a valid unit price.", "PAYG_UNIT_PRICE_NOT_CONFIGURED");

        var (periodStart, periodEnd) = PaygUsageCalculator.GetCalendarPeriod(today, subscription.BillingCycle);
        var period = await _db.PaygUsagePeriods
            .SingleOrDefaultAsync(x => x.CompanySubscriptionId == subscription.Id && x.PeriodStart == periodStart && x.PeriodEnd == periodEnd, cancellationToken);

        var accruesUsage = PaygUsageCalculator.AccruesUsage(subscription.Status)
            && today >= subscription.StartDate && today < subscription.EndDate;
        var chargeable = subscription.Status != SubscriptionStatusEnum.Trialing;

        if (period is null)
        {
            period = PaygUsagePeriod.Create(
                companyId, subscription.Id, periodStart, periodEnd, subscription.BillingCycle,
                unitPrice.Value, subscription.Plan.Currency, chargeable, now);
            _db.PaygUsagePeriods.Add(period);
        }

        if (!period.IsFinalized)
        {
            var cutoff = accruesUsage ? Min(today.AddDays(1), periodEnd) : Min(today, periodEnd);
            cutoff = Max(cutoff, periodStart);
            await RecalculateAsync(subscription, period, cutoff, today, now, cancellationToken);
            if (today >= periodEnd && period.CalculatedThrough == periodEnd)
                period.Finalize(now);
        }

        return Map(subscription, period);
    }

    private async Task RecalculateAsync(
        CompanySubscription subscription,
        PaygUsagePeriod period,
        DateOnly cutoff,
        DateOnly today,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var companyId = subscription.CompanyId;
        var leaseRows = await _db.LeaseContracts.IgnoreQueryFilters()
            .Where(x => x.CompanyId == companyId && x.StartDate < cutoff && x.EndDate > period.PeriodStart)
            .Select(x => new LeaseRow(
                x.Id, x.TenantId, x.BuildingId, x.ApartmentId, x.StartDate, x.EndDate, x.Status, x.DeletedAt,
                _db.Tenants.IgnoreQueryFilters().Where(t => t.Id == x.TenantId && t.CompanyId == companyId).Select(t => t.DeletedAt).FirstOrDefault(),
                _db.Buildings.IgnoreQueryFilters().Where(b => b.Id == x.BuildingId && b.CompanyId == companyId).Select(b => b.DeletedAt).FirstOrDefault(),
                _db.Apartments.IgnoreQueryFilters().Where(a => a.Id == x.ApartmentId && a.CompanyId == companyId).Select(a => a.DeletedAt).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var leaseIds = leaseRows.Select(x => x.Id).ToArray();
        var histories = await _db.ContractStatusHistory.AsNoTracking()
            .Where(x => x.CompanyId == companyId && leaseIds.Contains(x.LeaseContractId))
            .OrderBy(x => x.ChangedAt).ToListAsync(cancellationToken);
        var terminations = await _db.ContractTerminations.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.CompanyId == companyId && leaseIds.Contains(x.LeaseContractId))
            .ToDictionaryAsync(x => x.LeaseContractId, x => x.TerminationDate, cancellationToken);
        var existing = await _db.PaygLeaseUsage
            .Where(x => x.UsagePeriodId == period.Id)
            .ToDictionaryAsync(x => x.LeaseContractId, cancellationToken);

        var calculatedLeaseIds = new HashSet<Guid>();
        var totalLeaseDays = 0;
        var totalAmount = 0m;
        var currentActiveCount = 0;
        var periodDays = PaygUsageCalculator.Days(period.PeriodStart, period.PeriodEnd);

        foreach (var lease in leaseRows)
        {
            var leaseHistory = histories.Where(x => x.LeaseContractId == lease.Id).ToArray();
            DateOnly? terminationDate = terminations.TryGetValue(lease.Id, out var value) ? value : null;
            var interval = CalculateInterval(lease, leaseHistory, terminationDate, subscription, period, cutoff);
            if (interval is null) continue;

            var days = PaygUsageCalculator.Days(interval.Value.Start, interval.Value.End);
            if (days <= 0) continue;
            var amount = PaygUsageCalculator.CalculateAmount(
                days, periodDays, period.MonthlyEquivalentUnitPriceSnapshot, period.BillingCycle, period.IsChargeable);
            calculatedLeaseIds.Add(lease.Id);
            totalLeaseDays += days;
            totalAmount += amount;

            if (PaygUsageCalculator.AccruesUsage(subscription.Status) && IsActiveOn(lease, interval.Value, today)) currentActiveCount++;

            if (existing.TryGetValue(lease.Id, out var usage))
                usage.Recalculate(interval.Value.Start, interval.Value.End, checked((short)days), amount, now);
            else
                _db.PaygLeaseUsage.Add(PaygLeaseUsage.Create(
                    companyId, period.Id, lease.Id, lease.TenantId,
                    interval.Value.Start, interval.Value.End, checked((short)days), amount, now));
        }

        foreach (var obsolete in existing.Values.Where(x => !calculatedLeaseIds.Contains(x.LeaseContractId)))
            _db.PaygLeaseUsage.Remove(obsolete);

        var remainingDays = Math.Max(0, period.PeriodEnd.DayNumber - cutoff.DayNumber);
        var projectedIncrement = PaygUsageCalculator.CalculateAmount(
            currentActiveCount * remainingDays, periodDays,
            period.MonthlyEquivalentUnitPriceSnapshot, period.BillingCycle, period.IsChargeable);
        period.UpdateTotals(
            currentActiveCount, totalLeaseDays, Math.Round(totalAmount, 3),
            Math.Round(totalAmount + projectedIncrement, 3), cutoff, now);
    }

    private (DateOnly Start, DateOnly End)? CalculateInterval(
        LeaseRow lease,
        IReadOnlyList<PropertyOS.Domain.Leasing.ContractStatusHistory> history,
        DateOnly? terminationDate,
        CompanySubscription subscription,
        PaygUsagePeriod period,
        DateOnly cutoff)
    {
        var activeEvent = history.FirstOrDefault(x => x.NewStatus == ContractStatus.Active);
        if (activeEvent is null && lease.Status is not (ContractStatus.Active or ContractStatus.Expired or ContractStatus.Terminated or ContractStatus.Superseded or ContractStatus.Renewed))
            return null;

        var activationDate = activeEvent is null ? lease.StartDate : _clock.GetJordanBusinessDate(activeEvent.ChangedAt);
        var subscriptionStoppedAt = subscription.Status switch
        {
            SubscriptionStatusEnum.Suspended when subscription.SuspendedAt.HasValue => _clock.GetJordanBusinessDate(subscription.SuspendedAt.Value),
            SubscriptionStatusEnum.Cancelled when subscription.CancelledAt.HasValue => _clock.GetJordanBusinessDate(subscription.CancelledAt.Value),
            SubscriptionStatusEnum.Expired when subscription.ExpiredAt.HasValue => _clock.GetJordanBusinessDate(subscription.ExpiredAt.Value),
            _ => subscription.EndDate
        };

        var exitEvent = activeEvent is null
            ? history.FirstOrDefault(x => x.NewStatus != ContractStatus.Active)
            : history.FirstOrDefault(x => x.ChangedAt > activeEvent.ChangedAt && x.NewStatus != ContractStatus.Active);
        DateOnly? exitDate = null;
        if (exitEvent is not null)
            exitDate = exitEvent.NewStatus == ContractStatus.Terminated && terminationDate.HasValue
                ? terminationDate.Value
                : _clock.GetJordanBusinessDate(exitEvent.ChangedAt);
        if (terminationDate.HasValue && (!exitDate.HasValue || terminationDate.Value < exitDate.Value))
            exitDate = terminationDate;

        return PaygUsageCalculator.GetBillableInterval(
            period.PeriodStart, period.PeriodEnd, cutoff,
            subscription.StartDate, subscriptionStoppedAt,
            lease.StartDate, lease.EndDate, activationDate, exitDate,
            ToBusinessDate(lease.DeletedAt), ToBusinessDate(lease.TenantDeletedAt),
            ToBusinessDate(lease.BuildingDeletedAt), ToBusinessDate(lease.ApartmentDeletedAt));
    }

    private bool IsActiveOn(LeaseRow lease, (DateOnly Start, DateOnly End) interval, DateOnly day) =>
        lease.Status == ContractStatus.Active && interval.Start <= day && interval.End > day
        && lease.DeletedAt is null && lease.TenantDeletedAt is null
        && lease.BuildingDeletedAt is null && lease.ApartmentDeletedAt is null;

    private DateOnly? ToBusinessDate(DateTimeOffset? value) =>
        value.HasValue ? _clock.GetJordanBusinessDate(value.Value) : null;

    private static PaygUsageSummaryDto Fixed(CompanySubscription subscription) => new()
    {
        SubscriptionId = subscription.Id, PlanId = subscription.PlanId,
        PlanNameEn = subscription.Plan.NameEn, PlanNameAr = subscription.Plan.NameAr,
        PricingModel = SubscriptionPricingModel.Fixed, SubscriptionStatus = subscription.Status,
        BillingCycle = subscription.BillingCycle, Currency = subscription.CurrencyAtSubscription
    };

    private static PaygUsageSummaryDto Map(CompanySubscription subscription, PaygUsagePeriod period)
    {
        var elapsed = PaygUsageCalculator.Days(period.PeriodStart, period.CalculatedThrough);
        return new PaygUsageSummaryDto
        {
            SubscriptionId = subscription.Id, PlanId = subscription.PlanId,
            PlanNameEn = subscription.Plan.NameEn, PlanNameAr = subscription.Plan.NameAr,
            PricingModel = SubscriptionPricingModel.PayAsYouGo,
            SubscriptionStatus = subscription.Status, BillingCycle = period.BillingCycle,
            IsEstimated = !period.IsFinalized, IsFinalized = period.IsFinalized,
            IsChargeable = period.IsChargeable, CurrentActiveLeaseCount = period.ActiveLeaseCount,
            AccumulatedLeaseDays = period.AccumulatedLeaseDays,
            MonthlyEquivalentUnitPrice = period.MonthlyEquivalentUnitPriceSnapshot,
            Currency = period.CurrencySnapshot, EstimatedAmount = period.EstimatedAmount,
            ProjectedPeriodAmount = period.ProjectedAmount,
            BillingPeriodStart = period.PeriodStart, BillingPeriodEnd = period.PeriodEnd,
            CalculatedThrough = period.CalculatedThrough, PeriodDays = period.PeriodDayCount,
            DaysElapsed = elapsed, DaysRemaining = Math.Max(0, period.PeriodDayCount - elapsed)
        };
    }

    private static DateOnly Min(params DateOnly[] values) => values.Min();
    private static DateOnly Max(params DateOnly[] values) => values.Max();

    private sealed record LeaseRow(
        Guid Id, Guid TenantId, Guid BuildingId, Guid ApartmentId,
        DateOnly StartDate, DateOnly EndDate, ContractStatus Status,
        DateTimeOffset? DeletedAt, DateTimeOffset? TenantDeletedAt,
        DateTimeOffset? BuildingDeletedAt, DateTimeOffset? ApartmentDeletedAt);
}
