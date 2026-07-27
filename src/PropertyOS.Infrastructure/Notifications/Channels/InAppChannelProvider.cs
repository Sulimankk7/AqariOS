using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Notifications.Services;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Infrastructure.Notifications.Channels;

/// <summary>
/// In-app channel: pushes the notification to the recipient's live SignalR
/// connections via IInAppNotificationPusher (implemented in the Api layer over
/// IHubContext&lt;NotificationsHub&gt;). A completed push counts as a successful send;
/// any push exception is reported as a delivery failure, never rethrown, so one
/// channel's outage cannot poison the surrounding dispatch transaction.
/// </summary>
public sealed class InAppChannelProvider : INotificationChannelProvider
{
    private readonly IInAppNotificationPusher _pusher;

    public InAppChannelProvider(IInAppNotificationPusher pusher)
    {
        _pusher = pusher;
    }

    public DeliveryChannel Channel => DeliveryChannel.InApp;

    public async Task<ChannelSendResult> SendAsync(
        Notification notification,
        NotificationDelivery delivery,
        CancellationToken cancellationToken)
    {
        try
        {
            await _pusher.PushAsync(
                notification.RecipientUserId,
                notification.Id,
                notification.Subject,
                notification.Body,
                cancellationToken);

            return ChannelSendResult.Ok();
        }
        catch (Exception ex)
        {
            return ChannelSendResult.Failed(ex.Message);
        }
    }
}
