namespace PropertyOS.Application.Notifications;

/// <summary>
/// Dispatch-pipeline policy for Module 11 (Notifications).
/// </summary>
public static class NotificationDispatchPolicy
{
    /// <summary>
    /// Maximum send attempts per delivery. The domain (NotificationDelivery) tracks
    /// AttemptCount but deliberately defines no maximum, so the cap is an application
    /// policy: a Failed delivery is retried by the dispatch pipeline only while
    /// AttemptCount is below this value; at or beyond it the delivery is terminal and
    /// the eligibility query (INotificationRepository.GetDispatchCandidateIdsAsync)
    /// stops returning the parent notification.
    /// </summary>
    public const int MaxDeliveryAttempts = 3;
}
