using PropertyOS.Domain.Subscriptions.Enums;

namespace PropertyOS.Domain.Subscriptions;

/// <summary>
/// Auditable company-level PAYG usage for one calendar billing period.
/// Periods use the half-open interval [PeriodStart, PeriodEnd).
/// </summary>
public sealed class PaygUsagePeriod
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid CompanySubscriptionId { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public BillingCycleEnum BillingCycle { get; private set; }
    public decimal MonthlyEquivalentUnitPriceSnapshot { get; private set; }
    public string CurrencySnapshot { get; private set; } = "JOD";
    public short PeriodDayCount { get; private set; }
    public int ActiveLeaseCount { get; private set; }
    public int AccumulatedLeaseDays { get; private set; }
    public decimal EstimatedAmount { get; private set; }
    public decimal ProjectedAmount { get; private set; }
    public DateOnly CalculatedThrough { get; private set; }
    public bool IsChargeable { get; private set; }
    public bool IsFinalized { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public CompanySubscription CompanySubscription { get; private set; } = null!;

    private PaygUsagePeriod() { }

    public static PaygUsagePeriod Create(
        Guid companyId,
        Guid companySubscriptionId,
        DateOnly periodStart,
        DateOnly periodEnd,
        BillingCycleEnum billingCycle,
        decimal monthlyEquivalentUnitPriceSnapshot,
        string currencySnapshot,
        bool isChargeable,
        DateTimeOffset createdAt)
    {
        if (periodEnd <= periodStart) throw new ArgumentException("PAYG period end must be after its start.");
        if (monthlyEquivalentUnitPriceSnapshot <= 0) throw new ArgumentOutOfRangeException(nameof(monthlyEquivalentUnitPriceSnapshot));

        return new PaygUsagePeriod
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            CompanySubscriptionId = companySubscriptionId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            BillingCycle = billingCycle,
            MonthlyEquivalentUnitPriceSnapshot = monthlyEquivalentUnitPriceSnapshot,
            CurrencySnapshot = currencySnapshot,
            PeriodDayCount = checked((short)(periodEnd.DayNumber - periodStart.DayNumber)),
            CalculatedThrough = periodStart,
            IsChargeable = isChargeable,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void UpdateTotals(
        int activeLeaseCount,
        int accumulatedLeaseDays,
        decimal estimatedAmount,
        decimal projectedAmount,
        DateOnly calculatedThrough,
        DateTimeOffset updatedAt)
    {
        if (IsFinalized) return;
        if (calculatedThrough < PeriodStart || calculatedThrough > PeriodEnd)
            throw new ArgumentOutOfRangeException(nameof(calculatedThrough));

        ActiveLeaseCount = activeLeaseCount;
        AccumulatedLeaseDays = accumulatedLeaseDays;
        EstimatedAmount = estimatedAmount;
        ProjectedAmount = projectedAmount;
        CalculatedThrough = calculatedThrough;
        UpdatedAt = updatedAt;
    }

    public void Finalize(DateTimeOffset finalizedAt)
    {
        if (IsFinalized) return;
        if (CalculatedThrough != PeriodEnd)
            throw new InvalidOperationException("A PAYG period can only be finalized after the complete half-open interval has been calculated.");

        IsFinalized = true;
        FinalizedAt = finalizedAt;
        UpdatedAt = finalizedAt;
    }
}
