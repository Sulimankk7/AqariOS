using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Application.Notifications.Commands.UpdateNotificationTemplate;

public record UpdateNotificationTemplateCommand(
    Guid Id,
    string TemplateName,
    string Subject,
    string Body,
    NotificationType NotificationType,
    bool IsActive
) : ICommand;
