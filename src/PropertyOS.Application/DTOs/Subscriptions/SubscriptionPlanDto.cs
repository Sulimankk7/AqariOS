using System;

namespace PropertyOS.Application.DTOs.Subscriptions;

/// <summary>
/// Data transfer object representing a subscription plan.
/// </summary>
public class SubscriptionPlanDto
{
    /// <summary>
    /// Unique identifier for the subscription plan.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique plan code (e.g., "BASIC", "PRO", "ENTERPRISE").
    /// </summary>
    public string Code { get; set; } = null!;

    /// <summary>
    /// English display name.
    /// </summary>
    public string NameEn { get; set; } = null!;

    /// <summary>
    /// Arabic display name.
    /// </summary>
    public string NameAr { get; set; } = null!;

    /// <summary>
    /// English description.
    /// </summary>
    public string? DescriptionEn { get; set; }

    /// <summary>
    /// Arabic description.
    /// </summary>
    public string? DescriptionAr { get; set; }

    /// <summary>
    /// Monthly price in specified currency.
    /// </summary>
    public decimal MonthlyPrice { get; set; }

    /// <summary>
    /// Yearly price in specified currency.
    /// </summary>
    public decimal YearlyPrice { get; set; }

    public PropertyOS.Domain.Subscriptions.Enums.SubscriptionPricingModel PricingModel { get; set; }
    public decimal? PaygMonthlyUnitPrice { get; set; }
    public decimal? PaygYearlyMonthlyEquivalentUnitPrice { get; set; }

    /// <summary>
    /// ISO currency code (e.g., "JOD").
    /// </summary>
    public string Currency { get; set; } = "JOD";

    /// <summary>
    /// Maximum allowed buildings quota (null = unlimited).
    /// </summary>
    public int? MaxBuildings { get; set; }

    /// <summary>
    /// Maximum allowed users quota (null = unlimited).
    /// </summary>
    public int? MaxUsers { get; set; }

    /// <summary>
    /// Maximum allowed storage in megabytes (null = unlimited).
    /// </summary>
    public int? MaxStorageMb { get; set; }

    /// <summary>
    /// JSON string of enabled feature flags.
    /// </summary>
    public string FeatureFlags { get; set; } = "{}";

    /// <summary>
    /// Indicates whether the plan supports a trial period.
    /// </summary>
    public bool SupportsTrial { get; set; }

    /// <summary>
    /// Trial duration in days, if supported.
    /// </summary>
    public short? TrialDurationDays { get; set; }

    /// <summary>
    /// Indicates whether the plan is active for subscription.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Sort order index for UI display.
    /// </summary>
    public short SortOrder { get; set; }
}
