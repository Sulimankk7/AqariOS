using PropertyOS.Domain.Subscriptions.Enums;

namespace PropertyOS.Api.PlatformAdministration.Requests;

public sealed class CreatePlanRequest
{
    public string Code { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public string NameAr { get; init; } = string.Empty;
    public string? DescriptionEn { get; init; }
    public string? DescriptionAr { get; init; }
    public decimal MonthlyPrice { get; init; }
    public decimal YearlyPrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public int? MaxBuildings { get; init; }
    public int? MaxUsers { get; init; }
    public int? MaxStorageMb { get; init; }
    public string FeatureFlags { get; init; } = "{}";
    public bool SupportsTrial { get; init; }
    public short? TrialDurationDays { get; init; }
    public short SortOrder { get; init; }
}

public sealed class CreateCompanySubscriptionRequest
{
    public Guid CompanyId { get; init; }
    public Guid PlanId { get; init; }
    public BillingCycleEnum BillingCycle { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public DateOnly? TrialEndDate { get; init; }
}

public sealed class ApprovePlanChangeRequestRequest
{
    public string? DecisionNote { get; init; }
}

public sealed class RejectPlanChangeRequestRequest
{
    public string RejectionReason { get; init; } = string.Empty;
    public string? DecisionNote { get; init; }
}
