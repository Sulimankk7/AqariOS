using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Notifications.Commands.DispatchNotification;

/// <summary>
/// Attempts delivery of one notification across all of its sendable channels.
/// Sent by DispatchNotificationsJob per candidate; idempotent — dispatching a
/// notification that has nothing left to send is a silent no-op.
/// </summary>
public record DispatchNotificationCommand(Guid NotificationId) : ICommand;
