namespace PropertyOS.Application.DTOs.Subscriptions;

/// <summary>
/// Request payload for cancelling an active subscription.
/// </summary>
public class CancelSubscriptionRequestDto
{
    /// <summary>
    /// Optional reason for cancellation.
    /// </summary>
    public string? Reason { get; set; }
}
