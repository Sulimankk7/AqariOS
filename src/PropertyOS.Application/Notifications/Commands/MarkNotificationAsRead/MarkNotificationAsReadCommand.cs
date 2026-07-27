using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Notifications.Commands.MarkNotificationAsRead;

public record MarkNotificationAsReadCommand(Guid NotificationId) : ICommand;
