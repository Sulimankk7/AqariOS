using System;
using System.Threading;
using System.Threading.Tasks;

namespace PropertyOS.Application.Notifications.Services;

/// <summary>
/// Pushes an in-app notification to a connected recipient in real time.
/// Implemented in the Api layer (SignalR: IHubContext&lt;NotificationsHub&gt;.Clients.User(...))
/// because Application/Infrastructure must not reference ASP.NET Core SignalR hubs.
/// Registered in Program.cs; consumed by InAppChannelProvider (Infrastructure).
/// </summary>
public interface IInAppNotificationPusher
{
    Task PushAsync(
        Guid recipientUserId,
        Guid notificationId,
        string subject,
        string body,
        CancellationToken cancellationToken);
}
