using PropertyOS.Application.Subscriptions.DTOs;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Subscriptions;
using PropertyOS.Domain.Subscriptions.Enums;

namespace PropertyOS.Application.Subscriptions;

/// <summary>
/// Persistence port for subscription operations that require provider-specific SQL semantics.
/// Authorization and business-rule decisions remain in Application handlers.
/// </summary>
public interface ISubscriptionPersistence
{
    Task<PlanPageDto> GetPlatformPlansAsync(
        int page, int pageSize, bool? isActive, string? search, CancellationToken cancellationToken = default);

    Task<SubscriptionPageDto> GetPlatformSubscriptionsAsync(
        int page, int pageSize, string? search, SubscriptionStatusEnum? status,
        BillingCycleEnum? billingCycle, Guid? planId, string sortBy, bool descending,
        CancellationToken cancellationToken = default);

    Task<PlanChangeRequestPageDto> GetPlatformPlanChangeRequestsAsync(
        int page, int pageSize, PlanChangeRequestStatus? status, Guid? companyId, string? search,
        CancellationToken cancellationToken = default);

    Task<Company?> GetCompanyForUpdateAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<SubscriptionPlan?> GetPlanForUpdateAsync(Guid planId, CancellationToken cancellationToken = default);
    Task<CompanySubscription?> GetCurrentSubscriptionForUpdateAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<CompanySubscription?> GetSubscriptionForUpdateAsync(Guid subscriptionId, Guid companyId, CancellationToken cancellationToken = default);
    Task<PlanChangeRequest?> GetPlanChangeRequestForUpdateAsync(Guid requestId, Guid? companyId, CancellationToken cancellationToken = default);
}
