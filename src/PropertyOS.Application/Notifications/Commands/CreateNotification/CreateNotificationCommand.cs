using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Application.Notifications.Commands.CreateNotification;

public record CreateNotificationCommand(
    Guid RecipientUserId,
    Guid? TemplateId,
    string Subject,
    string Body,
    NotificationType NotificationType,
    NotificationPriority Priority,
    List<DeliveryChannel> Channels
) : IRequest<Guid>;
