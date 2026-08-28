using System;

namespace PropertyOS.Domain.Subscriptions;

public class SubscriptionPlan
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string NameEn { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? DescriptionEn { get; set; }
    public string? DescriptionAr { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public string Currency { get; set; } = "JOD";
    public int? MaxBuildings { get; set; }
    public int? MaxUsers { get; set; }
    public int? MaxStorageMb { get; set; }
    public string FeatureFlags { get; set; } = "{}";
    public bool SupportsTrial { get; set; }
    public short? TrialDurationDays { get; set; }
    public bool IsActive { get; set; } = true;
    public short SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public void Activate(DateTimeOffset changedAt)
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = changedAt;
    }

    public void Deactivate(DateTimeOffset changedAt)
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = changedAt;
    }
}
