using PropertyOS.Domain.Subscriptions.Enums;

namespace PropertyOS.Api.Subscriptions.Requests;

public sealed class CreatePlanChangeRequestRequest
{
    public Guid RequestedPlanId { get; init; }
    public BillingCycleEnum RequestedBillingCycle { get; init; }
}
