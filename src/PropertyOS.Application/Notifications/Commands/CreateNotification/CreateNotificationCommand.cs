using System;
using System.Collections.Generic;
using PropertyOS.Application.Common.Interfaces;
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
) : ICommand<Guid>;
