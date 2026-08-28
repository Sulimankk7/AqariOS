using PropertyOS.Domain.Subscriptions.Enums;

namespace PropertyOS.Application.Subscriptions.DTOs;

public sealed class PlanChangeRequestDto
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; }
    public string? CompanyName { get; init; }
    public Guid SubscriptionId { get; init; }
    public Guid CurrentPlanId { get; init; }
    public string CurrentPlanNameEn { get; init; } = string.Empty;
    public string CurrentPlanNameAr { get; init; } = string.Empty;
    public Guid RequestedPlanId { get; init; }
    public string RequestedPlanNameEn { get; init; } = string.Empty;
    public string RequestedPlanNameAr { get; init; } = string.Empty;
    public BillingCycleEnum CurrentBillingCycle { get; init; }
    public BillingCycleEnum RequestedBillingCycle { get; init; }
    public Guid RequestedBy { get; init; }
    public string? RequesterName { get; init; }
    public DateTimeOffset RequestedAt { get; init; }
    public PlanChangeRequestStatus Status { get; init; }
    public Guid? ReviewerId { get; init; }
    public string? ReviewerName { get; init; }
    public DateTimeOffset? ReviewedAt { get; init; }
    public string? DecisionNote { get; init; }
    public string? RejectionReason { get; init; }
}

public sealed class PlanChangeRequestPageDto
{
    public IReadOnlyList<PlanChangeRequestDto> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}
