using System;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Application.Notifications.Queries.Common;

public record NotificationTemplateDto(
    Guid Id,
    string TemplateName,
    string Subject,
    string Body,
    NotificationType NotificationType,
    bool IsActive
);

public record NotificationDto(
    Guid Id,
    Guid RecipientUserId,
    string Subject,
    string Body,
    NotificationType NotificationType,
    NotificationPriority Priority,
    NotificationStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt
);

public record NotificationDeliveryDto(
    Guid Id,
    Guid NotificationId,
    DeliveryChannel DeliveryChannel,
    DeliveryStatus DeliveryStatus,
    int AttemptCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt,
    DateTimeOffset? DeliveredAt,
    string? FailureReason
);
