using PropertyOS.Domain.Subscriptions.Enums;

namespace PropertyOS.Application.Subscriptions.DTOs;

public sealed class SubscriptionAdministrationDto
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public Guid PlanId { get; init; }
    public string PlanCode { get; init; } = string.Empty;
    public string PlanNameEn { get; init; } = string.Empty;
    public string PlanNameAr { get; init; } = string.Empty;
    public SubscriptionStatusEnum Status { get; init; }
    public BillingCycleEnum BillingCycle { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public DateOnly? TrialEndDate { get; init; }
    public decimal PriceAtSubscription { get; init; }
    public string CurrencyAtSubscription { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed class SubscriptionPageDto
{
    public IReadOnlyList<SubscriptionAdministrationDto> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}

public sealed class PlanPageDto
{
    public IReadOnlyList<PropertyOS.Application.DTOs.Subscriptions.SubscriptionPlanDto> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}
