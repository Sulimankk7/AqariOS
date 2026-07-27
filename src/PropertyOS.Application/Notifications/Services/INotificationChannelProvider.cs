using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Domain.Notifications;
using PropertyOS.Domain.Notifications.Enums;

namespace PropertyOS.Application.Notifications.Services;

/// <summary>
/// A delivery gateway for exactly one <see cref="DeliveryChannel"/>. Implementations
/// live in Infrastructure; the dispatch handler resolves the matching provider per
/// delivery. Implementations must not mutate the domain entities — state transitions
/// (RecordAttempt / MarkAsSent / MarkAsFailed) are owned by the dispatch handler.
/// </summary>
public interface INotificationChannelProvider
{
    DeliveryChannel Channel { get; }

    Task<ChannelSendResult> SendAsync(
        Notification notification,
        NotificationDelivery delivery,
        CancellationToken cancellationToken);
}
