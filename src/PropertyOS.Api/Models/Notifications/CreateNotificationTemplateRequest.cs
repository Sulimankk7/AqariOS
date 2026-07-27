using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Api.Models.Notifications;

/// <summary>
/// Request model for creating a new notification template.
/// </summary>
public record CreateNotificationTemplateRequest(
    string TemplateName,
    string Subject,
    string Body,
    NotificationType NotificationType,
    bool IsActive = true
);
