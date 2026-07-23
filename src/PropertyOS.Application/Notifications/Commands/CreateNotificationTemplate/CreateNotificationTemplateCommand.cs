using System;
using MediatR;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Application.Notifications.Commands.CreateNotificationTemplate;

public record CreateNotificationTemplateCommand(
    string TemplateName,
    string Subject,
    string Body,
    NotificationType NotificationType,
    bool IsActive = true
) : IRequest<Guid>;
