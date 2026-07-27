using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using PropertyOS.Api.Hubs;
using PropertyOS.Application.Notifications.Services;

namespace PropertyOS.Api.Services;

/// <summary>
/// In-app channel transport: pushes a notification to the recipient's live SignalR
/// connections. SignalR's default IUserIdProvider keys connections by
/// ClaimTypes.NameIdentifier (the user id emitted by JwtTokenGenerator), so
/// Clients.User(recipientUserId) targets exactly that user's devices. A recipient with
/// no open connections is still a successful send — the notification row itself is the
/// durable inbox; the push is best-effort real-time delivery.
/// </summary>
public sealed class SignalRInAppNotificationPusher : IInAppNotificationPusher
{
    private readonly IHubContext<NotificationsHub> _hubContext;

    public SignalRInAppNotificationPusher(IHubContext<NotificationsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PushAsync(Guid recipientUserId, Guid notificationId, string subject, string body, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.User(recipientUserId.ToString("D"))
            .SendAsync("notification", new { notificationId, subject, body }, cancellationToken);
    }
}
