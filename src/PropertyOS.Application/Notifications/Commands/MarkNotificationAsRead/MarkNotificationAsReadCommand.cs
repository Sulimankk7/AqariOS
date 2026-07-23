using System;
using MediatR;

namespace PropertyOS.Application.Notifications.Commands.MarkNotificationAsRead;

public record MarkNotificationAsReadCommand(Guid NotificationId) : IRequest;
