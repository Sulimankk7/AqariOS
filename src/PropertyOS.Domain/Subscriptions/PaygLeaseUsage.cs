namespace PropertyOS.Domain.Subscriptions;

/// <summary>Per-lease explanation record for a PAYG usage period.</summary>
public sealed class PaygLeaseUsage
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid UsagePeriodId { get; private set; }
    public Guid LeaseContractId { get; private set; }
    public Guid TenantId { get; private set; }
    public DateOnly UsageStart { get; private set; }
    public DateOnly UsageEnd { get; private set; }
    public short BillableDays { get; private set; }
    public decimal CalculatedAmount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public PaygUsagePeriod UsagePeriod { get; private set; } = null!;

    private PaygLeaseUsage() { }

    public static PaygLeaseUsage Create(
        Guid companyId,
        Guid usagePeriodId,
        Guid leaseContractId,
        Guid tenantId,
        DateOnly usageStart,
        DateOnly usageEnd,
        short billableDays,
        decimal calculatedAmount,
        DateTimeOffset createdAt) => new()
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            UsagePeriodId = usagePeriodId,
            LeaseContractId = leaseContractId,
            TenantId = tenantId,
            UsageStart = usageStart,
            UsageEnd = usageEnd,
            BillableDays = billableDays,
            CalculatedAmount = calculatedAmount,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

    public void Recalculate(DateOnly usageStart, DateOnly usageEnd, short billableDays, decimal calculatedAmount, DateTimeOffset updatedAt)
    {
        UsageStart = usageStart;
        UsageEnd = usageEnd;
        BillableDays = billableDays;
        CalculatedAmount = calculatedAmount;
        UpdatedAt = updatedAt;
    }
}
