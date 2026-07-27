using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Application.Notifications.Commands.UpdateNotificationDelivery;

public record UpdateNotificationDeliveryCommand(
    Guid NotificationId,
    Guid DeliveryId,
    DeliveryStatus NewStatus,
    string? FailureReason
) : ICommand;
