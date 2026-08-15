using MediatR;

namespace PropertyOS.Application.Notifications.Commands.MarkAllNotificationsAsRead;

/// <summary>
/// Command to mark all unread notifications for the currently authenticated user as read.
/// </summary>
public record MarkAllNotificationsAsReadCommand : IRequest<MarkAllNotificationsAsReadResult>;
