using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Api.Models.Notifications;

/// <summary>
/// Request model for updating the status of a notification delivery attempt.
/// </summary>
public record UpdateNotificationDeliveryRequest(
    DeliveryStatus NewStatus,
    string? FailureReason = null
);
