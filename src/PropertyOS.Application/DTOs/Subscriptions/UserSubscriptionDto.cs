using System;

namespace PropertyOS.Application.DTOs.Subscriptions;

/// <summary>
/// Data transfer object representing the active or recent subscription for a user/company.
/// </summary>
public class UserSubscriptionDto
{
    /// <summary>
    /// Unique identifier of the subscription record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Associated company/tenant identifier.
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Identifier of the selected plan.
    /// </summary>
    public Guid PlanId { get; set; }

    /// <summary>
    /// Code of the subscribed plan.
    /// </summary>
    public string PlanCode { get; set; } = null!;

    /// <summary>
    /// English name of the subscribed plan.
    /// </summary>
    public string PlanNameEn { get; set; } = null!;

    /// <summary>
    /// Arabic name of the subscribed plan.
    /// </summary>
    public string PlanNameAr { get; set; } = null!;

    /// <summary>
    /// Current status of the subscription (e.g., "Trialing", "Active", "PastDue", "Suspended", "Cancelled", "Expired").
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Subscription start date.
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// Subscription end date.
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// End date of the trial period, if applicable.
    /// </summary>
    public DateOnly? TrialEndDate { get; set; }

    /// <summary>
    /// Price captured at time of subscription creation or plan change.
    /// </summary>
    public decimal PriceAtSubscription { get; set; }

    /// <summary>
    /// Currency code at time of subscription.
    /// </summary>
    public string CurrencyAtSubscription { get; set; } = "JOD";

    /// <summary>
    /// Selected billing cycle ("Monthly" or "Yearly").
    /// </summary>
    public string BillingCycle { get; set; } = null!;

    /// <summary>
    /// Whether auto-renewal is enabled.
    /// </summary>
    public bool AutoRenew { get; set; }

    /// <summary>
    /// Timestamp when subscription was suspended, if applicable.
    /// </summary>
    public DateTimeOffset? SuspendedAt { get; set; }

    /// <summary>
    /// Reason for suspension, if applicable.
    /// </summary>
    public string? SuspensionReason { get; set; }

    /// <summary>
    /// Timestamp when subscription was cancelled, if applicable.
    /// </summary>
    public DateTimeOffset? CancelledAt { get; set; }

    /// <summary>
    /// Reason for cancellation, if applicable.
    /// </summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Timestamp when subscription expired, if applicable.
    /// </summary>
    public DateTimeOffset? ExpiredAt { get; set; }

    /// <summary>
    /// External reference ID from payment gateway.
    /// </summary>
    public string? ExternalBillingRef { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Record update timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
