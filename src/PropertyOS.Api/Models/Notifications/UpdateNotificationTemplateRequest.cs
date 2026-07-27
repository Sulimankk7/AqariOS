using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Api.Models.Notifications;

/// <summary>
/// Request model for updating an existing notification template.
/// </summary>
public record UpdateNotificationTemplateRequest(
    string TemplateName,
    string Subject,
    string Body,
    NotificationType NotificationType,
    bool IsActive
);
