using System;
using System.Collections.Generic;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Api.Models.Notifications;

/// <summary>
/// Request model for creating and dispatching a notification to a recipient user.
/// </summary>
public record CreateNotificationRequest(
    Guid RecipientUserId,
    string Subject,
    string Body,
    NotificationType NotificationType,
    NotificationPriority Priority,
    List<DeliveryChannel> Channels,
    Guid? TemplateId = null
);
